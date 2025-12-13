using System.Collections.Generic;
using System.Linq;
using JanSharp.Internal;
using TMPro;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [CustomEditor(typeof(MenuConfiguration))]
    public class MenuConfigurationEditor : Editor
    {
        private new MenuConfiguration target;
        private MenuConfigurationInternals Internals => target.internals;

        SerializedProperty internalsProp;
        SerializedProperty pagesProp;

        public void OnEnable()
        {
            target = (MenuConfiguration)base.target;
            internalsProp = serializedObject.FindProperty("internals");
            pagesProp = serializedObject.FindProperty("pages");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(internalsProp);
            EditorGUILayout.PropertyField(pagesProp);
            serializedObject.ApplyModifiedProperties();

            bool valid = true;

            if (Internals == null)
            {
                valid = false;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    GUILayout.Label("Missing reference to Menu Configuration Internals.", EditorStyles.wordWrappedLabel);
            }

            if (target.pages.Any(p => p == null))
            {
                valid = false;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    GUILayout.Label("There must be no null or missing page definitions.", EditorStyles.wordWrappedLabel);
            }

            var duplicateInternalNames = target.pages
                .Where(p => p != null)
                .Distinct()
                .GroupBy(p => p.internalName)
                .Where(g => g.Skip(1).Any());
            if (duplicateInternalNames.Any())
            {
                valid = false;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    GUILayout.Label("Each page must have a unique Internal Name. Duplicates:\n"
                        + string.Join('\n', duplicateInternalNames
                            .Select(g => $"{g.Key}:\n  {string.Join("\n  ", g.Select(p => p.name))}")),
                        EditorStyles.wordWrappedLabel);
            }

            if (target.pages.Where(p => p != null).Distinct().Count() != target.pages.Where(p => p != null).Count())
            {
                valid = false;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    GUILayout.Label("Each page can only be used once.", EditorStyles.wordWrappedLabel);
            }

            if (valid && GUILayout.Button("Build Menu"))
                BuildMenu();
        }

        private void BuildMenu()
        {
            List<GameObject> pageToggleGos = GetPageToggles(target.pages.Length);
            for (int i = 0; i < target.pages.Length; i++)
                ConfigurePageToggle(pageToggleGos[i], target.pages[i]);

            ClearPageRoots();
            GameObject[] pageRootGos = GeneratePages(target.pages);

            SerializedObject managerSo = new(Internals.menuManager);
            EditorUtil.SetArrayProperty(
                managerSo.FindProperty("pageInternalNames"),
                target.pages,
                (p, v) => p.stringValue = v.internalName);
            EditorUtil.SetArrayProperty(
                managerSo.FindProperty("pageRoots"),
                pageRootGos,
                (p, v) => p.objectReferenceValue = v.GetComponent<MenuPageRoot>());
            EditorUtil.SetArrayProperty(
                managerSo.FindProperty("pageToggles"),
                pageToggleGos,
                (p, v) => p.objectReferenceValue = v.GetComponent<Toggle>());
            EditorUtil.SetArrayProperty(
                managerSo.FindProperty("pageToggleLabels"),
                pageToggleGos,
                (p, v) => p.objectReferenceValue = v.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true).gameObject);
            managerSo.ApplyModifiedProperties();
        }

        private List<GameObject> GetPageToggles(int count)
        {
            List<GameObject> pageToggleGos = Internals.pageTogglesContainer
                .Cast<Transform>()
                .Select(t => t.gameObject)
                .Where(go => PrefabUtility.IsAnyPrefabInstanceRoot(go))
                .ToList();
            for (int i = pageToggleGos.Count - 1; i >= count; i--)
            {
                Undo.DestroyObjectImmediate(pageToggleGos[i]);
                pageToggleGos.RemoveAt(i);
            }
            foreach (GameObject go in pageToggleGos)
            {
                // I actually don't know how you would do Undo support correctly for this. Not that it matters here.
                // Only idea I'd have is recording that object and its entire hierarchy, which seems silly.
                PrefabUtility.RevertPrefabInstance(go, InteractionMode.AutomatedAction);
            }
            while (pageToggleGos.Count < count)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(Internals.pageTogglePrefab);
                go.name = Internals.pageTogglePrefab.name;
                go.transform.SetParent(Internals.pageTogglesContainer, worldPositionStays: false);
                go.transform.SetSiblingIndex(pageToggleGos.Count);
                pageToggleGos.Add(go);
                Undo.RegisterCreatedObjectUndo(go, "Build Menu");
            }
            return pageToggleGos;
        }

        private void ConfigurePageToggle(GameObject toggleGo, MenuPageDefinition pageDef)
        {
            Toggle toggle = toggleGo.GetComponent<Toggle>();
            SerializedObject so = new(toggle);
            so.FindProperty("m_Group").objectReferenceValue = Internals.pageTogglesGroup;
            EditorUtil.EnsureHasPersistentSendCustomEventListener(
                so.FindProperty("onValueChanged"),
                UdonSharpEditorUtility.GetBackingUdonBehaviour(Internals.menuManager),
                nameof(MenuManager.OnPageToggleValueChanged));
            so.FindProperty("m_IsOn").boolValue = false;
            so.ApplyModifiedProperties();

            so = new(toggleGo.GetComponentsInChildren<Image>(includeInactive: true)
                .First(i => i != toggle.targetGraphic && i != toggle.graphic));
            so.FindProperty("m_Sprite").objectReferenceValue = pageDef.icon;
            so.ApplyModifiedProperties();

            so = new(toggleGo.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true));
            so.FindProperty("m_text").stringValue = pageDef.displayName;
            so.ApplyModifiedProperties();
        }

        private void ClearPageRoots()
        {
            RectTransform container = Internals.pageRootsContainer;
            while (container.childCount != 0)
                Undo.DestroyObjectImmediate(container.GetChild(container.childCount - 1).gameObject);
        }

        private GameObject[] GeneratePages(MenuPageDefinition[] pageDefs)
        {
            GameObject[] pageRootGos = new GameObject[pageDefs.Length];
            for (int i = 0; i < pageDefs.Length; i++)
                pageRootGos[i] = GeneratePage(pageDefs[i], i);
            return pageRootGos;
        }

        private GameObject GeneratePage(MenuPageDefinition pageDef, int pageIndex)
        {
            GameObject pageRootGo = (GameObject)PrefabUtility.InstantiatePrefab(Internals.pageRootPrefab);
            pageRootGo.name = Internals.pageRootPrefab.name;
            pageRootGo.transform.SetParent(Internals.pageRootsContainer, worldPositionStays: false);
            Undo.RegisterCreatedObjectUndo(pageRootGo, "Build Menu");

            MenuPageRoot pageRoot = pageRootGo.GetComponent<MenuPageRoot>();
            SerializedObject so = new(pageRoot);
            so.FindProperty("pageInternalName").stringValue = pageDef.internalName;
            so.FindProperty("pageDisplayName").stringValue = pageDef.displayName;
            so.FindProperty("pageIndex").intValue = pageIndex;
            so.ApplyModifiedProperties();

            GameObject pageGo = (GameObject)PrefabUtility.InstantiatePrefab(pageDef.pagePrefab);
            pageGo.name = pageDef.pagePrefab.name;
            pageGo.transform.SetParent(pageRootGo.transform, worldPositionStays: false);
            Undo.RegisterCreatedObjectUndo(pageGo, "Build Menu");

            return pageRootGo;
        }
    }
}
