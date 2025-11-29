using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuManager : UdonSharpBehaviour
    {
        public string[] pageInternalNames;
        public CanvasGroup[] pageRoots;
        public Toggle[] pageToggles;
        public Image collapseButtonImage;
        public Sprite collapseIcon;
        public Sprite expandIcon;
        public RectTransform sideCanvas;
        public bool isCollapsed;
        public float collapsedPosition;
        public float collapsedSize;
        public float expandedPosition;
        public float expandedSize;

        private int pageCount;
        private int activePageIndex = -1;

        public void Start()
        {
            pageCount = pageRoots.Length;
            OnPageToggleValueChanged();
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
            CanvasGroup pageRoot;
            if (this.activePageIndex >= 0)
            {
                pageRoot = pageRoots[this.activePageIndex];
                pageRoot.blocksRaycasts = false;
                pageRoot.alpha = 0f;
            }
            this.activePageIndex = activePageIndex;
            pageRoot = pageRoots[activePageIndex];
            pageRoot.blocksRaycasts = true;
            pageRoot.alpha = 1f;
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
