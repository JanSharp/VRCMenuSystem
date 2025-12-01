using System.Linq;
using UnityEditor;

namespace JanSharp
{
    [InitializeOnLoad]
    public static class MenuPageRootOnBuild
    {
        static MenuPageRootOnBuild()
        {
            OnBuildUtil.RegisterType<MenuPageRoot>(OnBuild);
        }

        private static bool OnBuild(MenuPageRoot pageRoot)
        {
            SerializedObject so = new(pageRoot);
            using (new EditorUtil.BatchedEditorOnlyChecksScope())
                so.FindProperty("hasAnyShowPageByPermissionsInChildren").boolValue
                    = pageRoot.GetComponentsInChildren<ShowPageByPermission>(includeInactive: true)
                        .Any(s => !EditorUtil.IsEditorOnly(s));
            so.ApplyModifiedProperties();
            return true;
        }
    }
}
