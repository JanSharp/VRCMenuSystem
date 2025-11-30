// using UnityEditor;

// namespace JanSharp
// {
//     [InitializeOnLoad]
//     public static class MenuPageRootOnBuild
//     {
//         static MenuPageRootOnBuild()
//         {
//             OnBuildUtil.RegisterType<MenuPageRoot>(OnBuild);
//         }

//         private static bool OnBuild(MenuPageRoot pageRoot)
//         {
//             SerializedObject so = new(pageRoot.GetComponentsInChildren<ShowPageByPermission>(includeInactive: true));
//             so.FindProperty("menuPageRoot").objectReferenceValue = pageRoot;
//             so.ApplyModifiedProperties();
//             return true;
//         }
//     }
// }
