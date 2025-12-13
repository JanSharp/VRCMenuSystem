using JanSharp.Internal;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    public class MenuConfigurationInternals : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public MenuManager menuManager;
        public RectTransform pageTogglesContainer;
        public RectTransform pageRootsContainer;
        public ToggleGroup pageTogglesGroup;
        public GameObject pageTogglePrefab;
        public GameObject pageRootPrefab;
    }
}
