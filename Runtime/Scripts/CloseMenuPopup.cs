using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CloseMenuPopup : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;
        public RectTransform popup;

        public void OnClick()
        {
            menuManager.ClosePopup(popup, doCallback: true);
        }
    }
}
