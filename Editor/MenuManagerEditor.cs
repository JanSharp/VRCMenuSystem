using UdonSharpEditor;
using UnityEditor;

namespace JanSharp.Internal
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MenuManager))]
    public class MenuManagerEditor : Editor
    {
        private static bool internalFoldedOut = false;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets))
                return;

            if (internalFoldedOut = EditorGUILayout.Foldout(internalFoldedOut, "Internal", toggleOnLabelClick: true))
            {
                serializedObject.Update();
                DrawPropertiesExcluding(serializedObject, "m_Script");
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
