using UnityEngine;

namespace JanSharp
{
    [CreateAssetMenu(fileName = "Page", menuName = "Menu Page Definition", order = 999)]
    public class MenuPageDefinition : ScriptableObject
    {
        public string internalName;
        public string displayName;
        public Sprite icon;
        public GameObject pagePrefab;
    }
}
