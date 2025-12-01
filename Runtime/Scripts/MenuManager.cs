using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [SingletonScript("d9be4a8a9d454bfb7ba93f4988cbe45a")]
    public class MenuManager : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        private Internal.Lockstep lockstepHiddenAPI;

        public string[] pageInternalNames;
        public MenuPageRoot[] pageRoots;
        public Toggle[] pageToggles;
        public ToggleGroup pageTogglesToggleGroup;
        public Image collapseButtonImage;
        public Sprite collapseIcon;
        public Sprite expandIcon;
        public RectTransform sideCanvas;
        public TextMeshProUGUI infoTextOverlay;
        public bool isCollapsed;
        public float collapsedPosition;
        public float collapsedSize;
        public float expandedPosition;
        public float expandedSize;

        public CanvasGroup loadingPageRoot;
        public TextMeshProUGUI loadingTitle;
        public Slider loadingProgress;
        public Image loadingProgressFill;
        public TextMeshProUGUI loadingInfo;
        public Color minLoadingProgressFillColor;
        public Color maxLoadingProgressFillColor;
        public float loadingProgressFillPulseDuration = 1f;
        private uint firstCatchUpTick;
        private bool loadingPageIsShown;

        private int pageCount = 0;
        private int shownPageCount = 0;
        private int activePageIndex = IndexForUninitializedActivePage;

        private const int IndexForUninitializedActivePage = -2;
        private const int IndexForNoShownPages = -1;

        public void Start()
        {
            lockstepHiddenAPI = (Internal.Lockstep)lockstep;
            pageCount = pageRoots.Length;
            foreach (MenuPageRoot pageRoot in pageRoots)
                pageRoot.Initialize();
            UpdateWhichPagesAreShown();
            ShowLoadingPage();
        }

        public void UpdateWhichPagesAreShown()
        {
            pageTogglesToggleGroup.allowSwitchOff = true;
            shownPageCount = 0;
            int firstShownPageIndex = -1;
            for (int i = 0; i < pageCount; i++)
            {
                MenuPageRoot pageRoot = pageRoots[i];
                Toggle pageToggle = pageToggles[i];
                pageToggle.SetIsOnWithoutNotify(false);
                bool shouldBeShown = pageRoot.ShouldBeShown;
                pageToggle.gameObject.SetActive(shouldBeShown);
                if (!shouldBeShown)
                    continue;
                shownPageCount++;
                if (firstShownPageIndex == -1)
                    firstShownPageIndex = i;
            }
            if (shownPageCount == 0)
            {
                SetActivePageIndex(IndexForNoShownPages);
                return;
            }
            if (activePageIndex < 0 || !pageRoots[activePageIndex].ShouldBeShown)
                SetActivePageIndex(firstShownPageIndex);
            pageToggles[activePageIndex].SetIsOnWithoutNotify(true);
            pageTogglesToggleGroup.allowSwitchOff = false;
        }

        public void OnPageToggleValueChanged()
        {
            for (int i = 0; i < pageCount; i++)
                if (pageToggles[i].isOn)
                {
                    SetActivePageIndex(i);
                    break;
                }
        }

        private void SetActivePageIndex(int activePageIndex)
        {
            if (this.activePageIndex == activePageIndex)
                return;
            HideActivePage();
            this.activePageIndex = activePageIndex;
            ShowActivePage();
            UpdateInfoTextOverlay();
        }

        private void HideActivePage()
        {
            if (activePageIndex < 0)
                return;
            CanvasGroup pageRoot = pageRoots[activePageIndex].CanvasGroup;
            pageRoot.blocksRaycasts = false;
            pageRoot.alpha = 0f;
        }

        private void ShowActivePage()
        {
            if (activePageIndex < 0)
                return;
            CanvasGroup pageRoot = pageRoots[activePageIndex].CanvasGroup;
            pageRoot.blocksRaycasts = true;
            pageRoot.alpha = 1f;
        }

        private void UpdateInfoTextOverlay()
        {
            if (loadingPageIsShown || activePageIndex != IndexForNoShownPages)
            {
                infoTextOverlay.gameObject.SetActive(false);
                return;
            }
            infoTextOverlay.text = pageCount == 0
                ? "No pages configured for this menu,\n"
                    + "or the menu has not been built,\n"
                    + "or this is the wrong menu prefab."
                : "Missing permissions to view any pages.";
            infoTextOverlay.gameObject.SetActive(true);
        }

        public void OnCollapseClick()
        {
            isCollapsed = !isCollapsed;
            collapseButtonImage.sprite = isCollapsed ? expandIcon : collapseIcon;
            sideCanvas.localPosition = new Vector3(isCollapsed ? collapsedPosition : expandedPosition, 0f, 0f);
            Vector2 size = sideCanvas.sizeDelta;
            size.x = isCollapsed ? collapsedSize : expandedSize;
            sideCanvas.sizeDelta = size;
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            firstCatchUpTick = lockstep.CurrentTick;
        }

        private void ShowLoadingPage()
        {
            if (loadingPageIsShown)
                return;
            loadingPageIsShown = true;
            loadingPageRoot.alpha = 1f;
            UpdateInfoTextOverlay();
            HideActivePage();
            LoadingPageUpdateLoop();
        }

        private void HideLoadingPage()
        {
            if (!loadingPageIsShown)
                return;
            loadingPageIsShown = false;
            loadingPageRoot.alpha = 0f;
            UpdateInfoTextOverlay();
            ShowActivePage();
        }

        public void LoadingPageUpdateLoop()
        {
            if (lockstep.IsInitialized && !lockstep.IsCatchingUp)
            {
                HideLoadingPage();
                return;
            }
            if (lockstepHiddenAPI.IsProcessingLJGameStates)
            {
                loadingProgressFill.color = Color.white;
                int processingGSIndex = lockstepHiddenAPI.NextLJGameStateToProcess;
                loadingProgress.value = lockstep.AllGameStatesCount / (processingGSIndex + 1f);
                loadingInfo.text = $"Processing {lockstep.GetGameState(processingGSIndex).GameStateDisplayName} "
                    + $"[{processingGSIndex + 1}/{lockstep.AllGameStatesCount}]";
                SendCustomEventDelayedFrames(nameof(LoadingPageUpdateLoop), 1);
                return;
            }
            if (lockstep.IsCatchingUp)
            {
                loadingProgressFill.color = Color.white;
                uint goal = lockstepHiddenAPI.LastRunnableTick - firstCatchUpTick;
                uint current = lockstep.CurrentTick - firstCatchUpTick;
                loadingProgress.value = goal / (float)current;
                loadingInfo.text = "Catching Up";
                SendCustomEventDelayedFrames(nameof(LoadingPageUpdateLoop), 1);
                return;
            }
            loadingProgress.value = 1f;
            loadingProgressFill.color = Color.Lerp(
                minLoadingProgressFillColor,
                maxLoadingProgressFillColor,
                (Mathf.Sin((Time.time % loadingProgressFillPulseDuration) * Mathf.PI * 2f / loadingProgressFillPulseDuration)
                    + 1f) / 2f);
            loadingInfo.text = lockstepHiddenAPI.IsWaitingForLateJoinerSync
                ? "Waiting For Data"
                : "Idly Waiting";
            SendCustomEventDelayedFrames(nameof(LoadingPageUpdateLoop), 1);
        }
    }
}
