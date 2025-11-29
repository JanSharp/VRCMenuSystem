using UnityEngine;

namespace JanSharp
{
    public class MenuConfiguration : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public MenuPageDefinition[] pages;

        public MenuManager menuManager;
    }
}
