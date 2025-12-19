using JanSharp.Internal;
using UdonSharpEditor;
using UnityEditor;

namespace JanSharp
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MenuManager))]
    public class MenuManagerEditor : Editor
    {
        private bool foldedOut = false;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets))
                return;

            if (foldedOut = EditorGUILayout.Foldout(foldedOut, "Internal", toggleOnLabelClick: true))
            {
                serializedObject.Update();
                DrawPropertiesExcluding(serializedObject, "m_Script");
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
