using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace JanSharp
{
    [InitializeOnLoad]
    public static class ShowPageByPermissionOnBuild
    {
        static ShowPageByPermissionOnBuild()
        {
            OnBuildUtil.RegisterTypeCumulative<ShowPageByPermission>(OnBuildCumulative);
        }

        private static bool OnBuildCumulative(IEnumerable<ShowPageByPermission> showPageByPermissions)
        {
            bool result = true;
            foreach (var showPageByPermission in showPageByPermissions)
            {
                SerializedObject so = new(showPageByPermission);
                so.FindProperty("menuPageRoot").objectReferenceValue = showPageByPermission.GetComponentInParent<MenuPageRoot>(includeInactive: true);
                so.FindProperty("isIgnored").boolValue = showPageByPermission.GetComponentInParent<IgnoreShowPageByPermissionInChildren>(includeInactive: true) != null;
                so.ApplyModifiedProperties();

                if (!PermissionSystemEditorUtil.OnPermissionConditionsListBuild(
                    showPageByPermission,
                    showPageByPermission.AssetGuids,
                    permissionDefsFieldName: "permissionDefs",
                    conditionsHeaderName: "Conditions"))
                {
                    result = false;
                }
            }
            return result;
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(ShowPageByPermission))]
    public class ShowPageByPermissionEditor : Editor
    {
        private SerializedProperty whenConditionsAreMetProp;
        private PermissionConditionsList conditionsList;

        public void OnEnable()
        {
            whenConditionsAreMetProp = serializedObject.FindProperty("whenConditionsAreMet");

            conditionsList = new PermissionConditionsList(
                targets: targets,
                header: new GUIContent("Conditions", "Empty Conditions are treated as a match"),
                logicalAndsFieldName: "logicalAnds",
                invertsFieldName: "inverts",
                assetGuidsFieldName: "assetGuids",
                getLogicalAnds: t => ((ShowPageByPermission)t).logicalAnds,
                getInverts: t => ((ShowPageByPermission)t).inverts,
                getAssetGuids: t => ((ShowPageByPermission)t).AssetGuids);
        }

        public void OnDisable()
        {
            conditionsList.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets))
                return;

            serializedObject.Update();
            EditorGUILayout.PropertyField(whenConditionsAreMetProp);
            serializedObject.ApplyModifiedProperties();

            conditionsList.Draw();
        }
    }
}
