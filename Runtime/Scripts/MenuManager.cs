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

        private int pageCount = 0;
        private int shownPageCount = 0;
        private int activePageIndex = IndexForUninitializedActivePage;

        private const int IndexForUninitializedActivePage = -2;
        private const int IndexForNoShownPages = -1;

        public void Start()
        {
            pageCount = pageRoots.Length;
            foreach (MenuPageRoot pageRoot in pageRoots)
                pageRoot.Initialize();
            UpdateWhichPagesAreShown();
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
            if (this.activePageIndex >= 0)
            {
                CanvasGroup pageRoot = pageRoots[this.activePageIndex].CanvasGroup;
                pageRoot.blocksRaycasts = false;
                pageRoot.alpha = 0f;
            }
            this.activePageIndex = activePageIndex;
            if (this.activePageIndex >= 0)
            {
                CanvasGroup pageRoot = pageRoots[this.activePageIndex].CanvasGroup;
                pageRoot.blocksRaycasts = true;
                pageRoot.alpha = 1f;
            }
            UpdateInfoTextOverlay();
        }

        private void UpdateInfoTextOverlay()
        {
            if (activePageIndex != IndexForNoShownPages)
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
    }
}
