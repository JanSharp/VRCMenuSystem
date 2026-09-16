using JanSharp.Internal;
using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuPointerDetection : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][FindInParent] protected MenuManager menuManager;

        private bool pointerIsInDetectionArea;
        public bool PointerIsInDetectionArea => pointerIsInDetectionArea;

        public void OnPointerEnter()
        {
            pointerIsInDetectionArea = true;
            menuManager.UpdatePointerDetection();
        }

        public void OnPointerExit()
        {
            pointerIsInDetectionArea = false;
            menuManager.UpdatePointerDetection();
        }

        protected virtual void OnDisable() => OnPointerExit(); // Just to make sure.
    }
}
