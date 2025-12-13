using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TestCenteredPopup : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private MenuManagerAPI menuManager;
        public RectTransform popup;
        private Transform popupParent;

        public void OnClick()
        {
            popupParent = popup.parent;
            menuManager.ShowPopupAtItsAnchor(popup, this, nameof(OnPopupClosed));
        }

        public void OnPopupClosed()
        {
            popup.SetParent(popupParent);
        }
    }
}
