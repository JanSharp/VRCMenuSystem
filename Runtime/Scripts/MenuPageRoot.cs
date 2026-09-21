using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuPageRoot : MenuPointerDetection
    {
        [HideInInspector][SerializeField] private string pageInternalName;
        [HideInInspector][SerializeField] private string pageDisplayName;
        [HideInInspector][SerializeField] private int pageIndex;
        public string PageInternalName => pageInternalName;
        public string PageDisplayName => pageDisplayName;
        public int PageIndex => pageIndex;

        [HideInInspector][SerializeField] private bool hasAnyShowPageByScriptsInChildren;
        private bool IsShownByDefault => !hasAnyShowPageByScriptsInChildren;
        private uint shouldBeShownCounter;
        public bool ShouldBeShown => shouldBeShownCounter != 0u;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public const string PageInternalNamePropName = nameof(pageInternalName);
        public const string PageDisplayNamePropName = nameof(pageDisplayName);
        public const string PageIndexPropName = nameof(pageIndex);
        public const string HasAnyShowPageByScriptsInChildrenPropName = nameof(hasAnyShowPageByScriptsInChildren);
#endif

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
