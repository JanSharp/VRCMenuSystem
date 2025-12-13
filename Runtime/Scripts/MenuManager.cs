using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    // Both the API and this type have the SingletonScript so that internal scripts can directly reference the internal type.
    [SingletonScript("d9be4a8a9d454bfb7ba93f4988cbe45a")] // Runtime/Prefabs/Internal/MenuDummy - the system you are using likely provides its own prefab.prefab
    public class MenuManager : MenuManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        private Lockstep lockstepHiddenAPI;

        public string[] pageInternalNames;
        public MenuPageRoot[] pageRoots;
        public Toggle[] pageToggles;
        public GameObject[] pageToggleLabels;
        public ToggleGroup pageTogglesToggleGroup;
        public Image collapseButtonImage;
        public Sprite collapseIcon;
        public Sprite expandIcon;
        public RectTransform mainCanvas;
        public RectTransform sideCanvas;
        public Transform vrPositioningRoot;
        public CanvasGroup vrCanvasGroup;
        public CanvasGroup desktopCanvasGroup;
        public Collider mainCanvasCollider;
        public Collider sideCanvasCollider;
        public RectTransform mainRoot;
        public RectTransform sideRoot;
        public RectTransform desktopCanvas;
        public RectTransform desktopScalingRoot;
        public RectTransform desktopMainPanel;
        public RectTransform desktopSidePanel;
        public TextMeshProUGUI infoTextOverlay;
        public Image mainOpaqueImage;
        public Image sideOpaqueImage;
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
        /// <summary>
        /// <para>Prevent import loading screens from causing a seizure when game states are small.</para>
        /// </summary>
        private const float ShowLoadingPageForAtLeast = 0.8f;
        /// <summary>
        /// <para>Short confirmation of importing being done.</para>
        /// </summary>
        private const float ShowLoadingPageOnceDoneForAtLeast = 0.3f;
        private float keepLoadingPageOpenUntil;
        private bool importIsWaitingForData;

        private int pageCount = 0;
        private int shownPageCount = 0;
        private int activePageIndex = IndexForUninitializedActivePage;
        private const int IndexForUninitializedActivePage = -2;
        private const int IndexForNoShownPages = -1;

        #region Popups

        public RectTransform popupContainer;
        public Image[] popupBackgroundImages;
        public Button[] popupBackgroundButtons;
        public RectTransform primaryPopupBackground;

        private RectTransform[] popups = new RectTransform[ArrList.MinCapacity];
        private UdonSharpBehaviour[] popupCallbackInsts = new UdonSharpBehaviour[ArrList.MinCapacity];
        private string[] popupCallbackNames = new string[ArrList.MinCapacity];
        private int popupsCount = 0;
        private int popupCallbackInstsCount = 0;
        private int popupCallbackNamesCount = 0;

        // 99% of system will easily be able to know which popup got closed, and have a reference to it,
        // making this useful only for edge cases. But still, might as well have it.
        private RectTransform popupToClose;
        public override RectTransform PopupToClose => popupToClose;

        #endregion

        public override string ActivePageInternalName => activePageIndex < 0 ? null : pageInternalNames[activePageIndex];

        public bool isMenuOpen = true;
        public override bool IsMenuOpen
        {
            get => isMenuOpen;
            set
            {
                if (isMenuOpen == value)
                    return;
                isMenuOpen = value;
                RaiseOnMenuOpenStateChanged();
            }
        }

        public void Start()
        {
            lockstepHiddenAPI = (Lockstep)lockstep;
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
            RaiseOnDefaultMusicChanged();
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
            foreach (GameObject label in pageToggleLabels)
                label.SetActive(!isCollapsed);
        }

        #region Loading Page

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            firstCatchUpTick = lockstep.CurrentTick;
        }

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart()
        {
            importIsWaitingForData = true;
            keepLoadingPageOpenUntil = Time.time + ShowLoadingPageForAtLeast;
            ShowLoadingPage();
        }

        [LockstepEvent(LockstepEventType.OnImportOptionsDeserialized)]
        public void OnImportOptionsDeserialized()
        {
            importIsWaitingForData = false;
        }

        [LockstepEvent(LockstepEventType.OnImportFinished)]
        public void OnImportFinished()
        {
            keepLoadingPageOpenUntil = Mathf.Max(keepLoadingPageOpenUntil, Time.time + ShowLoadingPageOnceDoneForAtLeast);
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
            if (lockstep.IsImporting)
            {
                loadingTitle.text = "Importing";
                if (importIsWaitingForData)
                {
                    ThrobLoadingProgressFill();
                    loadingInfo.text = "Waiting For Data";
                    SendCustomEventDelayedFrames(nameof(LoadingPageUpdateLoop), 1);
                    return;
                }
                loadingProgressFill.color = Color.white;
                int importingGSIndex = lockstepHiddenAPI.GameStatesBeingImportedFinishedCount;
                loadingProgress.value = lockstep.GameStatesBeingImportedCount / (importingGSIndex + 1f);
                loadingInfo.text = $"Processing {lockstep.GetGameStateBeingImported(importingGSIndex).GameStateDisplayName} "
                    + $"[{importingGSIndex + 1}/{lockstep.GameStatesBeingImportedCount}]";
                SendCustomEventDelayedFrames(nameof(LoadingPageUpdateLoop), 1);
                return;
            }
            if (Time.time < keepLoadingPageOpenUntil)
            {
                loadingProgressFill.color = Color.white;
                loadingProgress.value = 1f;
                loadingInfo.text = "Done!";
                // Continuing to loop like this is inefficient, but another import could technically start
                // effectively nearly instantly after finishing one, at which point this is the easiest approach.
                // And nobody cares about that miniscule performance impact for less than a second.
                SendCustomEventDelayedFrames(nameof(LoadingPageUpdateLoop), 1);
                return;
            }

            if (lockstep.IsInitialized && !lockstep.IsCatchingUp)
            {
                HideLoadingPage();
                return;
            }
            loadingTitle.text = "Loading";
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
            ThrobLoadingProgressFill();
            loadingInfo.text = lockstepHiddenAPI.IsWaitingForLateJoinerSync
                ? "Waiting For Data"
                : "Idly Waiting";
            SendCustomEventDelayedFrames(nameof(LoadingPageUpdateLoop), 1);
        }

        private void ThrobLoadingProgressFill()
        {
            loadingProgress.value = 1f;
            loadingProgressFill.color = Color.Lerp(
                minLoadingProgressFillColor,
                maxLoadingProgressFillColor,
                (Mathf.Sin((Time.time % loadingProgressFillPulseDuration) * Mathf.PI * 2f / loadingProgressFillPulseDuration)
                    + 1f) / 2f);
        }

        #endregion

        #region Popups

        public override void ShowPopupAtItsAnchor(
            RectTransform popup,
            UdonSharpBehaviour callbackInst,
            string callbackEventName)
        {
            AddPopup(popup, callbackInst, callbackEventName);
            popup.anchoredPosition = Vector2.zero;
        }

        public override void ShowPopupAtCurrentPosition(
            RectTransform popup,
            UdonSharpBehaviour callbackInst,
            string callbackEventName,
            float minDistanceFromPageEdge = 20f)
        {
            AddPopup(popup, callbackInst, callbackEventName);
            PushOntoMainCanvas(popup, minDistanceFromPageEdge);
        }

        private void AddPopup(
            RectTransform popup,
            UdonSharpBehaviour callbackInst,
            string callbackEventName)
        {
            if (ArrList.Contains(ref popups, ref popupsCount, popup))
            {
                Debug.LogError($"[MenuSystem] Attempt to show popup '{popup.name}' when it was already shown.");
                return;
            }
            primaryPopupBackground.SetSiblingIndex(popupsCount);
            ArrList.Add(ref popups, ref popupsCount, popup);
            ArrList.Add(ref popupCallbackInsts, ref popupCallbackInstsCount, callbackInst);
            ArrList.Add(ref popupCallbackNames, ref popupCallbackNamesCount, callbackEventName);
            popup.SetParent(popupContainer);
            popup.gameObject.SetActive(true);
            if (popupCallbackInstsCount != 1)
                return;
            foreach (Image img in popupBackgroundImages)
                img.raycastTarget = true;
            foreach (Button btn in popupBackgroundButtons)
                btn.interactable = true;
        }

        private void PushOntoMainCanvas(RectTransform toPush, float minDistanceFromPageEdge)
        {
            Vector2 normalizedAnchor = toPush.anchorMin;
            if (toPush.anchorMax != normalizedAnchor) // Stretching is not supported.
                return;
            // TODO: Try using the `rect` property rather than doing the math manually.
            Vector2 canvasSize = mainCanvas.sizeDelta;
            Vector2 anchoredPosition = toPush.anchoredPosition;
            Vector2 anchor = canvasSize * normalizedAnchor + anchoredPosition;
            Vector2 size = toPush.sizeDelta;
            Vector2 bottomLeft = anchor - size * toPush.pivot;
            Vector2 topRight = bottomLeft + size;

            float distanceFromEdge = bottomLeft.x - minDistanceFromPageEdge;
            if (distanceFromEdge < 0f)
                anchoredPosition.x -= distanceFromEdge;

            distanceFromEdge = bottomLeft.y - minDistanceFromPageEdge;
            if (distanceFromEdge < 0f)
                anchoredPosition.y -= distanceFromEdge;

            distanceFromEdge = (canvasSize.x - minDistanceFromPageEdge) - topRight.x;
            if (distanceFromEdge < 0f)
                anchoredPosition.x += distanceFromEdge;

            distanceFromEdge = (canvasSize.y - minDistanceFromPageEdge) - topRight.y;
            if (distanceFromEdge < 0f)
                anchoredPosition.y += distanceFromEdge;

            toPush.anchoredPosition = anchoredPosition;
        }

        public void OnDarkPopupBackgroundClick()
        {
            ClosePopupAt(popupsCount - 1, doCallback: true);
        }

        /// <summary>
        /// <para>Can be called recursively.</para>
        /// </summary>
        /// <param name="popup"></param>
        /// <param name="doCallback"></param>
        public override void ClosePopup(RectTransform popup, bool doCallback)
        {
            int index = ArrList.IndexOf(ref popups, ref popupsCount, popup);
            if (index < 0)
            {
                Debug.LogError($"[MenuSystem] Attempt to close popup '{popup.name}' when it was not shown.");
                return;
            }
            ClosePopupAt(index, doCallback);
        }

        private void ClosePopupAt(int index, bool doCallback)
        {
            RectTransform popup = ArrList.RemoveAt(ref popups, ref popupsCount, index);
            UdonSharpBehaviour inst = ArrList.RemoveAt(ref popupCallbackInsts, ref popupCallbackInstsCount, index);
            string eventName = ArrList.RemoveAt(ref popupCallbackNames, ref popupCallbackNamesCount, index);
            primaryPopupBackground.SetSiblingIndex(popupCallbackInstsCount);
            if (popupCallbackInstsCount == 0)
            {
                foreach (Image img in popupBackgroundImages)
                    img.raycastTarget = false;
                foreach (Button btn in popupBackgroundButtons)
                    btn.interactable = false;
            }
            popup.gameObject.SetActive(false);
            if (!doCallback)
                return;
            popupToClose = popup;
            inst.SendCustomEvent(eventName);
            // TODO: I'm pretty sure since none of the local variables are used after the SendCustomEvent call
            // recursion should work just fine even without the recursive method attribute. Requires testing.
            popupToClose = null;
        }

        #endregion

        #region Events

        private UdonSharpBehaviour[] onMenuActivePageChangedListeners = new UdonSharpBehaviour[ArrList.MinCapacity];
        private int onMenuActivePageChangedListenersCount = 0;
        private UdonSharpBehaviour[] onMenuOpenStateChangedListeners = new UdonSharpBehaviour[ArrList.MinCapacity];
        private int onMenuOpenStateChangedListenersCount = 0;

        public override void RegisterOnMenuActivePageChanged(UdonSharpBehaviour listener)
        {
            ArrList.Add(ref onMenuActivePageChangedListeners, ref onMenuActivePageChangedListenersCount, listener);
        }

        private void RaiseOnDefaultMusicChanged()
        {
            for (int i = 0; i < onMenuActivePageChangedListenersCount; i++)
            {
                UdonSharpBehaviour listener = onMenuActivePageChangedListeners[i];
                if (listener != null) // Listeners should not get destroyed, but there is no way do deregister so I guess.
                    listener.SendCustomEvent(OnMenuActivePageChangedEventName);
            }
        }

        public override void RegisterOnMenuOpenStateChanged(UdonSharpBehaviour listener)
        {
            ArrList.Add(ref onMenuOpenStateChangedListeners, ref onMenuOpenStateChangedListenersCount, listener);
        }

        private void RaiseOnMenuOpenStateChanged()
        {
            for (int i = 0; i < onMenuOpenStateChangedListenersCount; i++)
            {
                UdonSharpBehaviour listener = onMenuOpenStateChangedListeners[i];
                if (listener != null) // Listeners should not get destroyed, but there is no way do deregister so I guess.
                    listener.SendCustomEvent(OnMenuOpenStateChangedEventName);
            }
        }

        #endregion
    }
}
