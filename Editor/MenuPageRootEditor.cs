using System.Linq;
using UnityEditor;

namespace JanSharp
{
    public static class MenuPageRootOnBuild
    {
        [OrderedInitializeOnLoad]
        private static void OnAssemblyLoad()
        {
            OnBuildUtil.RegisterType<MenuPageRoot>(OnBuild);
        }

        private static bool OnBuild(MenuPageRoot pageRoot)
        {
            SerializedObject so = new(pageRoot);
            using (new EditorUtil.BatchedEditorOnlyChecksScope())
                so.FindProperty("hasAnyShowPageByScriptsInChildren").boolValue
                    = pageRoot.GetComponentsInChildren<ShowPageByPermission>(includeInactive: true)
                        .Any(s => !EditorUtil.IsEditorOnly(s)
                            && s.GetComponentInParent<IgnoreShowPageByPermissionInChildren>(includeInactive: true) == null)
                    || pageRoot.GetComponentsInChildren<ShowPageByPlatform>(includeInactive: true)
                        .Any(s => !EditorUtil.IsEditorOnly(s)
                            && s.GetComponentInParent<IgnoreShowPageByPlatformInChildren>(includeInactive: true) == null);
            so.ApplyModifiedProperties();
            return true;
        }
    }
}
