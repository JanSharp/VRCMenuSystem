using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TestCustomPositionedPopup : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;
        public RectTransform popup;
        private Transform popupParent;
        private Vector2 localPosition;

        public void OnClick()
        {
            popupParent = popup.parent;
            localPosition = popup.anchoredPosition;
            menuManager.ShowPopupAtCurrentPosition(popup, this, nameof(OnPopupClosed));
        }

        public void OnPopupClosed()
        {
            popup.SetParent(popupParent);
            popup.anchoredPosition = localPosition;
        }
    }
}
