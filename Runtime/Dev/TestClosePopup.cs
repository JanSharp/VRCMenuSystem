using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TestClosePopup : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private MenuManager menuManager;
        public RectTransform popup;

        public void OnClick()
        {
            menuManager.ClosePopup(popup, doCallback: true);
        }
    }
}
