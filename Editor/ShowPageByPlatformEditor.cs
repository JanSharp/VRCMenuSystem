using UnityEditor;

namespace JanSharp
{
    public static class ShowPageByPlatformOnBuild
    {
        [OrderedInitializeOnLoad]
        private static void OnAssemblyLoad()
        {
            OnBuildUtil.RegisterType<ShowPageByPlatform>(OnBuild);
        }

        private static bool OnBuild(ShowPageByPlatform showPageByPlatform)
        {
            SerializedObject so = new(showPageByPlatform);
            so.FindProperty(ShowPageByPlatform.MenuPageRootPropName).objectReferenceValue = showPageByPlatform.GetComponentInParent<MenuPageRoot>(includeInactive: true);
            so.FindProperty(ShowPageByPlatform.IsIgnoredPropName).boolValue = showPageByPlatform.GetComponentInParent<IgnoreShowPageByPlatformInChildren>(includeInactive: true) != null;
            so.ApplyModifiedProperties();
            return true;
        }
    }
}
