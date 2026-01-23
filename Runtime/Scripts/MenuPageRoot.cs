using JanSharp.Internal;
using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuPageRoot : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][FindInParent] private MenuManager menuManager;

        [HideInInspector][SerializeField] private string pageInternalName;
        [HideInInspector][SerializeField] private string pageDisplayName;
        [HideInInspector][SerializeField] private int pageIndex;
        public string PageInternalName => pageInternalName;
        public string PageDisplayName => pageDisplayName;
        public int PageIndex => pageIndex;

        [HideInInspector][SerializeField] private bool hasAnyShowPageByPermissionsInChildren;
        private bool IsShownByDefault => !hasAnyShowPageByPermissionsInChildren;
        private uint shouldBeShownCounter;
        public bool ShouldBeShown => shouldBeShownCounter != 0u;

        private bool isInitialized;
        private bool isForcedHidden;

        public void Initialize()
        {
            if (IsShownByDefault && !isForcedHidden)
                shouldBeShownCounter++; // Do not call UpdateWhichPagesAreShown, the manager itself will run that later.
            isInitialized = true;
        }

        public void HideByDefault()
        {
            if (isForcedHidden)
                return;
            isForcedHidden = true;
            if (isInitialized && IsShownByDefault)
                DecrementShouldBeShown();
        }

        public void IncrementShouldBeShown()
        {
            shouldBeShownCounter++;
            if (shouldBeShownCounter != 1u) // Was 0.
                return;
            if (isInitialized)
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
            if (isInitialized)
                menuManager.UpdateWhichPagesAreShown();
        }
    }
}
