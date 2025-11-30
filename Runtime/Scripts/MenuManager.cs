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
            UpdateWhichPagesAreShown();
        }

        public void UpdateWhichPagesAreShown()
        {
            if (pageCount == 0)
                return;
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
            } // TODO: else hide the special page for "no pages are shown".
            this.activePageIndex = activePageIndex;
            if (this.activePageIndex >= 0)
            {
                CanvasGroup pageRoot = pageRoots[this.activePageIndex].CanvasGroup;
                pageRoot.blocksRaycasts = true;
                pageRoot.alpha = 1f;
            } // TODO: else show the special page for "no pages are shown".
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
