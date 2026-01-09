using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(MenuManagerEventAttribute), typeof(MenuManagerEventType), FindListenersInChildrenOnly = true)]
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
        public RectTransform vrRootCanvas;
        public Transform vrPositioningRoot;
        public RectTransform desktopCanvas;
        public RectTransform desktopScalingRoot;
        public RectTransform desktopElementsRoot;
        public TextMeshProUGUI infoTextOverlay;
        public Image mainOpaqueImage;
        public Image sideOpaqueImage;
        public bool isCollapsed;
        public float canvasHeight;
        public float mainPanelWidth;
        public float collapsedSideSize;
        public float expandedSideSize;

        public GameObject loadingPageRoot;
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
        private bool importGotCancelled;

        /// <summary>
        /// <para>Gets set to <see langword="true"/> the first time the loading page disappears.</para>
        /// </summary>
        private bool pageTogglesShouldBeShown = false;

        private int pageCount = 0;
        private int shownPageCount = 0;
        private int activePageIndex = IndexForUninitializedActivePage;
        private const int IndexForUninitializedActivePage = -2;
        private const int IndexForNoShownPages = -1;

        #region Popups

        public RectTransform popupContainer;
        public GameObject popupContainerGo;
        public RectTransform popupBackground;
        public Image popupBackgroundImage;
        public Button popupBackgroundButton;

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

        [System.NonSerialized] public bool isMenuOpen = true;
        public override bool IsMenuOpen
        {
            get => isMenuOpen;
            set
            {
#if PERMISSION_SYSTEM_DEBUG
                Debug.Log($"[MenuSystemDebug] Manager {this.name}  IsMenuOpen.set - isMenuOpen: {isMenuOpen}, value: {value}");
#endif
                if (isMenuOpen == value)
                    return;
                isMenuOpen = value;
                RaiseOnMenuOpenStateChanged();
            }
        }

        public void Start()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  Start");
#endif
            lockstepHiddenAPI = (Lockstep)lockstep;
            pageCount = pageRoots.Length;
            foreach (MenuPageRoot pageRoot in pageRoots)
                pageRoot.Initialize();
            InitPopupBlockingBackground();
            UpdateWhichPagesAreShown();
            ShowLoadingPage();
            RaiseOnMenuManagerStart();
        }

        public void UpdateWhichPagesAreShown()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  UpdateWhichPagesAreShown");
#endif
            if (!pageTogglesShouldBeShown)
            {
                HideAllPageToggles();
                return;
            }

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

        private void HideAllPageToggles()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  HideAllPageToggles");
#endif
            pageTogglesToggleGroup.allowSwitchOff = true;
            for (int i = 0; i < pageCount; i++)
            {
                Toggle pageToggle = pageToggles[i];
                pageToggle.SetIsOnWithoutNotify(false);
                pageToggle.gameObject.SetActive(false);
            }
            SetActivePageIndex(IndexForNoShownPages);
        }

        public void OnPageToggleValueChanged()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  OnPageToggleValueChanged");
#endif
            for (int i = 0; i < pageCount; i++)
                if (pageToggles[i].isOn)
                {
                    SetActivePageIndex(i);
                    break;
                }
        }

        private void SetActivePageIndex(int activePageIndex)
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  SetActivePageIndex");
#endif
            if (this.activePageIndex == activePageIndex)
                return;
            HideActivePage();
            this.activePageIndex = activePageIndex;
            if (!loadingPageIsShown)
                ShowActivePage();
            UpdateInfoTextOverlay();
            RaiseOnMenuActivePageChanged();
        }

        private void HideActivePage()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  HideActivePage");
#endif
            if (activePageIndex < 0)
                return;
            pageRoots[activePageIndex].gameObject.SetActive(false);
        }

        private void ShowActivePage()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  ShowActivePage");
#endif
            if (activePageIndex < 0)
                return;
            pageRoots[activePageIndex].gameObject.SetActive(true);
        }

        private void UpdateInfoTextOverlay()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  UpdateInfoTextOverlay");
#endif
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
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  OnCollapseClick");
#endif
            isCollapsed = !isCollapsed;
            collapseButtonImage.sprite = isCollapsed ? expandIcon : collapseIcon;
            float sideSize = isCollapsed ? collapsedSideSize : expandedSideSize;
            Vector2 negatedHalfSideSizeWide = new Vector2(sideSize / -2f, 0f);
            vrRootCanvas.anchoredPosition = negatedHalfSideSizeWide;
            vrRootCanvas.sizeDelta = new Vector2(mainPanelWidth + sideSize, canvasHeight);
            popupBackground.anchoredPosition = negatedHalfSideSizeWide;
            popupBackground.sizeDelta = new Vector2(sideSize, 0f);
            foreach (GameObject label in pageToggleLabels)
                label.SetActive(!isCollapsed);
        }

        #region Loading Page

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  OnClientBeginCatchUp");
#endif
            firstCatchUpTick = lockstep.CurrentTick;
        }

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  OnImportStart");
#endif
            importIsWaitingForData = true;
            keepLoadingPageOpenUntil = Time.time + ShowLoadingPageForAtLeast;
            ShowLoadingPage();
        }

        [LockstepEvent(LockstepEventType.OnImportOptionsDeserialized)]
        public void OnImportOptionsDeserialized()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  OnImportOptionsDeserialized");
#endif
            importIsWaitingForData = false;
        }

        [LockstepEvent(LockstepEventType.OnImportFinished)]
        public void OnImportFinished()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  OnImportFinished");
#endif
            if (lockstep.GameStatesBeingImportedFinishedCount == 0) // Got cancelled due to the importing player leaving.
            {
                importIsWaitingForData = false;
                importGotCancelled = true;
            }
            keepLoadingPageOpenUntil = Mathf.Max(keepLoadingPageOpenUntil, Time.time + ShowLoadingPageOnceDoneForAtLeast);
        }

        private void ShowLoadingPage()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  ShowLoadingPage");
#endif
            importGotCancelled = false; // Reset unconditionally.
            if (loadingPageIsShown)
                return;
            loadingPageIsShown = true;
            UpdateInfoTextOverlay();
            HideActivePage();
            LoadingPageUpdateLoop();
            loadingPageRoot.SetActive(true);
        }

        private void HideLoadingPage()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  HideLoadingPage");
#endif
            if (!loadingPageIsShown)
                return;
            loadingPageIsShown = false;
            loadingPageRoot.SetActive(false);
            UpdateInfoTextOverlay();
            if (pageTogglesShouldBeShown)
                ShowActivePage();
            else
            {
                pageTogglesShouldBeShown = true;
                UpdateWhichPagesAreShown();
            }
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
                int currentStep = lockstep.GameStatesBeingImportedFinishedCount + 1;
                int totalStepsCount = lockstep.GameStatesBeingImportedCount + 1;
                loadingProgress.value = currentStep / (float)totalStepsCount;
                loadingInfo.text = currentStep == totalStepsCount
                    ? $"Finishing Up [{currentStep}/{totalStepsCount}]"
                    : $"Processing {lockstep.GetGameStateBeingImported(currentStep - 1).GameStateDisplayName} "
                        + $"[{currentStep}/{totalStepsCount}]";
                SendCustomEventDelayedFrames(nameof(LoadingPageUpdateLoop), 1);
                return;
            }
            if (Time.time < keepLoadingPageOpenUntil)
            {
                loadingProgressFill.color = Color.white;
                loadingProgress.value = 1f;
                loadingInfo.text = importGotCancelled ? "Import Cancelled" : "Done!";
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
                loadingProgress.value = (processingGSIndex + 1f) / lockstep.AllGameStatesCount;
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
                loadingProgress.value = current / (float)goal;
                loadingInfo.text = "Catching Up";
                SendCustomEventDelayedFrames(nameof(LoadingPageUpdateLoop), 1);
                return;
            }
            ThrobLoadingProgressFill();
            loadingInfo.text = lockstepHiddenAPI.IsWaitingForLateJoinerSync
                ? "Waiting For Data"
                : lockstep.LockstepIsInitialized // But IsInitialized is still false.
                ? "Running Initial Setup"
                : "Waiting";
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
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  ShowPopupAtItsAnchor");
#endif
            AddPopup(popup, callbackInst, callbackEventName);
            popup.anchoredPosition = Vector2.zero;
        }

        public override void ShowPopupAtCurrentPosition(
            RectTransform popup,
            UdonSharpBehaviour callbackInst,
            string callbackEventName,
            float minDistanceFromPageEdge = 20f)
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  ShowPopupAtCurrentPosition");
#endif
            AddPopup(popup, callbackInst, callbackEventName);
            PushOntoMainCanvas(popup, minDistanceFromPageEdge);
        }

        private void AddPopup(
            RectTransform popup,
            UdonSharpBehaviour callbackInst,
            string callbackEventName)
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  AddPopup");
#endif
            if (ArrList.Contains(ref popups, ref popupsCount, popup))
            {
                Debug.LogError($"[MenuSystemDebug] Attempt to show popup '{popup.name}' when it was already shown.", popup);
                return;
            }
            popupBackground.SetSiblingIndex(popupsCount);
            ArrList.Add(ref popups, ref popupsCount, popup);
            ArrList.Add(ref popupCallbackInsts, ref popupCallbackInstsCount, callbackInst);
            ArrList.Add(ref popupCallbackNames, ref popupCallbackNamesCount, callbackEventName);
            popup.SetParent(popupContainer);
            popup.gameObject.SetActive(true);
            if (popupsCount == 1)
                EnablePopupBlockingBackground();
        }

        private void PushOntoMainCanvas(RectTransform toPush, float minDistanceFromPageEdge)
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  PushOntoMainCanvas");
#endif
            Vector2 normalizedAnchor = toPush.anchorMin;
            if (toPush.anchorMax != normalizedAnchor) // Stretching is not supported.
                return;
            // TODO: Try using the `rect` property rather than doing the math manually.
            Vector2 canvasSize = new Vector2(mainPanelWidth, canvasHeight);
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
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  OnDarkPopupBackgroundClick");
#endif
            ClosePopupAt(popupsCount - 1, doCallback: true);
        }

        /// <summary>
        /// <para>Can be called recursively.</para>
        /// </summary>
        /// <param name="popup"></param>
        /// <param name="doCallback"></param>
        public override void ClosePopup(RectTransform popup, bool doCallback)
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  ClosePopup");
#endif
            int index = ArrList.IndexOf(ref popups, ref popupsCount, popup);
            if (index < 0)
            {
                Debug.LogError($"[MenuSystemDebug] Attempt to close popup '{popup.name}' when it was not shown.", popup);
                return;
            }
            ClosePopupAt(index, doCallback);
        }

        private void ClosePopupAt(int index, bool doCallback)
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  ClosePopupAt");
#endif
            RectTransform popup = ArrList.RemoveAt(ref popups, ref popupsCount, index);
            UdonSharpBehaviour inst = ArrList.RemoveAt(ref popupCallbackInsts, ref popupCallbackInstsCount, index);
            string eventName = ArrList.RemoveAt(ref popupCallbackNames, ref popupCallbackNamesCount, index);
            popupBackground.SetSiblingIndex(popupCallbackInstsCount);
            if (popupsCount == 0)
                DisablePopupBlockingBackground();
            popup.gameObject.SetActive(false);
            if (!doCallback)
                return;
            popupToClose = popup;
            inst.SendCustomEvent(eventName);
            // TODO: I'm pretty sure since none of the local variables are used after the SendCustomEvent call
            // recursion should work just fine even without the recursive method attribute. Requires testing.
            popupToClose = null;
        }

        private void InitPopupBlockingBackground()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  InitPopupBlockingBackground");
#endif
            popupBackgroundButton.interactable = false;
        }

        private void EnablePopupBlockingBackground()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  EnablePopupBlockingBackground");
#endif
            popupContainerGo.SetActive(true);
            popupBackgroundImage.raycastTarget = true;
            popupBackgroundButton.interactable = true;
        }

        private void DisablePopupBlockingBackground()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  DisablePopupBlockingBackground");
#endif
            popupBackgroundImage.raycastTarget = false;
            popupBackgroundButton.interactable = false;
            SendCustomEventDelayedSeconds(nameof(DisablePopupBlockingBackgroundDelayed), 0.11f);
        }

        public void DisablePopupBlockingBackgroundDelayed()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  DisablePopupBlockingBackgroundDelayed");
#endif
            if (popupsCount != 0)
                return;
            // Disable entirely to remove overdraw from the now invisible Image and disabling the Graphics Raycaster.
            popupContainerGo.SetActive(false);
        }

        #endregion

        #region Events

        private UdonSharpBehaviour[] onMenuActivePageChangedListeners = new UdonSharpBehaviour[ArrList.MinCapacity];
        private int onMenuActivePageChangedListenersCount = 0;
        private UdonSharpBehaviour[] onMenuOpenStateChangedListeners = new UdonSharpBehaviour[ArrList.MinCapacity];
        private int onMenuOpenStateChangedListenersCount = 0;

        public override void RegisterOnMenuActivePageChanged(UdonSharpBehaviour listener)
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  RegisterOnMenuActivePageChanged");
#endif
            ArrList.Add(ref onMenuActivePageChangedListeners, ref onMenuActivePageChangedListenersCount, listener);
        }

        private void RaiseOnMenuActivePageChanged()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  RaiseOnMenuActivePageChanged");
#endif
            for (int i = 0; i < onMenuActivePageChangedListenersCount; i++)
            {
                UdonSharpBehaviour listener = onMenuActivePageChangedListeners[i];
                if (listener != null) // Listeners should not get destroyed, but there is no way do deregister so I guess.
                    listener.SendCustomEvent(OnMenuActivePageChangedEventName);
            }
        }

        public override void RegisterOnMenuOpenStateChanged(UdonSharpBehaviour listener)
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  RegisterOnMenuOpenStateChanged");
#endif
            ArrList.Add(ref onMenuOpenStateChangedListeners, ref onMenuOpenStateChangedListenersCount, listener);
        }

        private void RaiseOnMenuOpenStateChanged()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  RaiseOnMenuOpenStateChanged");
#endif
            for (int i = 0; i < onMenuOpenStateChangedListenersCount; i++)
            {
                UdonSharpBehaviour listener = onMenuOpenStateChangedListeners[i];
                if (listener != null) // Listeners should not get destroyed, but there is no way do deregister so I guess.
                    listener.SendCustomEvent(OnMenuOpenStateChangedEventName);
            }
        }

        #endregion

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onMenuManagerStartListeners;

        private void RaiseOnMenuManagerStart()
        {
#if PERMISSION_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] Manager {this.name}  RaiseOnMenuManagerStart");
#endif
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onMenuManagerStartListeners, nameof(MenuManagerEventType.OnMenuManagerStart));
        }

        #endregion
    }
}
