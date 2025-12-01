using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuPageRoot : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private MenuManager menuManager;

        [SerializeField] private CanvasGroup canvasGroup;
        [HideInInspector][SerializeField] private string pageInternalName;
        [HideInInspector][SerializeField] private string pageDisplayName;
        [HideInInspector][SerializeField] private int pageIndex;
        public CanvasGroup CanvasGroup => canvasGroup;
        public string PageInternalName => pageInternalName;
        public string PageDisplayName => pageDisplayName;
        public int PageIndex => pageIndex;

        [HideInInspector][SerializeField] private bool hasAnyShowPageByPermissionsInChildren;
        private uint shouldBeShownCounter;
        public bool ShouldBeShown => shouldBeShownCounter != 0u;

        public void Initialize()
        {
            if (!hasAnyShowPageByPermissionsInChildren)
                shouldBeShownCounter++; // Do not call UpdateWhichPagesAreShown, the manager itself will run that later.
        }

        public void IncrementShouldBeShown()
        {
            shouldBeShownCounter++;
            if (shouldBeShownCounter != 1u) // Was 0.
                return;
            menuManager.UpdateWhichPagesAreShown();
        }

        public void DecrementShouldBeShown()
        {
            if (shouldBeShownCounter == 0u)
            {
                Debug.LogError($"[MenuSystem] Attempt to {nameof(DecrementShouldBeShown)} more often than "
                    + $"{nameof(IncrementShouldBeShown)} on a {nameof(MenuPageRoot)} script.");
                return;
            }
            shouldBeShownCounter--;
            if (shouldBeShownCounter != 0u)
                return;
            menuManager.UpdateWhichPagesAreShown();
        }
    }
}
