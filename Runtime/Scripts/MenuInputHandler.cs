using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace JanSharp
{
    public enum MenuOpenCloseKeyBind
    {
        DownUp,
        DownDown,
        HoldDown,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuInputHandler : UdonSharpBehaviour
    {
        [SerializeField][HideInInspector][SingletonReference] private UpdateManager updateManager;
        [HideInInspector][SerializeField][SingletonReference] private BoneAttachmentManager boneAttachment;
        /// <summary>For the <see cref="UpdateManager"/>.</summary>
        [System.NonSerialized] public int customUpdateInternalIndex;

        public MenuManager menuManager;
        public MenuOpenCloseKeyBind keyBind;
        public float pixelsFromDesktopScreenEdge = 50f;
        public Transform makeDesktopCanvasWorkWhileHoldingTab;

        private const float UpThreshold = 0.3f;
        private const float DownThreshold = -0.3f;

        private const float HoldDownTimer = 1.5f;
        private const float DoubleInputTimeout = 0.75f;

        // Down as in "input key down" equivalent
        // Down as in "look down"
        private float previousDownDownTime;
        private float currentDownDownTime;

        private bool isHoldingDown;
        private bool holdDownActuated;

        private bool isInVR;

        private bool isMenuOpen;

        private CanvasGroup vrCanvasGroup;
        private CanvasGroup desktopCanvasGroup;
        private Collider mainCanvasCollider;
        private Collider sideCanvasCollider;
        private RectTransform desktopCanvas;
        private Vector2 lastDesktopCanvasSize = -Vector2.one;

        public void Start()
        {
            vrCanvasGroup = menuManager.vrCanvasGroup;
            desktopCanvasGroup = menuManager.desktopCanvasGroup;
            mainCanvasCollider = menuManager.mainCanvasCollider;
            sideCanvasCollider = menuManager.sideCanvasCollider;
            desktopCanvas = menuManager.desktopCanvas;

            isInVR = Networking.LocalPlayer.IsUserInVR();
            if (isInVR)
            {
                Destroy(menuManager.desktopCanvas.gameObject);
                isMenuOpen = true;
                OpenCloseInVR(); // Close.
                return;
            }
            updateManager.Register(this);
            MoveMenuIntoScreenCanvas();
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            if (!isInVR)
                return;

            if (isHoldingDown)
            {
                if (value <= DownThreshold)
                {
                    if (holdDownActuated || Time.time > (currentDownDownTime + HoldDownTimer))
                        return;
                    holdDownActuated = true;
                    if (keyBind == MenuOpenCloseKeyBind.HoldDown)
                        OpenCloseInVR();
                    return;
                }
                isHoldingDown = false;
                holdDownActuated = false;
                previousDownDownTime = currentDownDownTime;
                return;
            }

            // Not holding down.

            if (value <= DownThreshold)
            {
                isHoldingDown = true;
                currentDownDownTime = Time.time;
                if (keyBind == MenuOpenCloseKeyBind.DownDown && currentDownDownTime < previousDownDownTime + DoubleInputTimeout)
                    OpenCloseInVR();
                return;
            }
            if (value >= UpThreshold && keyBind == MenuOpenCloseKeyBind.DownUp && Time.time < previousDownDownTime + DoubleInputTimeout)
                OpenCloseInVR();
        }

        private void OpenCloseInVR()
        {
            isMenuOpen = !isMenuOpen;
            vrCanvasGroup.alpha = isMenuOpen ? 1f : 0f;
            vrCanvasGroup.blocksRaycasts = isMenuOpen; // For good measure.
            mainCanvasCollider.enabled = isMenuOpen;
            sideCanvasCollider.enabled = isMenuOpen;
        }

        private void MoveMenuIntoScreenCanvas()
        {
            desktopCanvas.gameObject.SetActive(true);
            menuManager.desktopMainPanel.sizeDelta = menuManager.mainCanvas.sizeDelta;
            menuManager.desktopMainPanel.anchoredPosition = menuManager.mainCanvas.anchoredPosition;
            menuManager.desktopSidePanel.sizeDelta = menuManager.sideCanvas.sizeDelta;
            menuManager.desktopSidePanel.anchoredPosition = menuManager.sideCanvas.anchoredPosition;
            menuManager.mainRoot.SetParent(menuManager.desktopMainPanel, worldPositionStays: false);
            menuManager.sideRoot.SetParent(menuManager.desktopSidePanel, worldPositionStays: false);
            GameObject vrRootToDestroy = menuManager.vrCanvasGroup.gameObject;
            menuManager.vrCanvasGroup = menuManager.desktopCanvasGroup;
            menuManager.mainCanvas = menuManager.desktopMainPanel;
            menuManager.sideCanvas = menuManager.desktopSidePanel;
            Destroy(vrRootToDestroy);
            UpdateDesktopMenuScale();
            isMenuOpen = false;
            desktopCanvasGroup.alpha = 0f;
            desktopCanvasGroup.blocksRaycasts = false;
        }

        private void UpdateDesktopMenuScale()
        {
            Vector2 size = desktopCanvas.sizeDelta;
            if (size == lastDesktopCanvasSize)
                return;
            lastDesktopCanvasSize = size;
            Vector2 availableSize = size - Vector2.one * pixelsFromDesktopScreenEdge * 2f;
            float contentWidth = menuManager.mainCanvas.sizeDelta.x + menuManager.expandedSize * 2f;
            float contentHeight = menuManager.mainCanvas.sizeDelta.y;
            float scale = Mathf.Max(0.1f, Mathf.Min(availableSize.x / contentWidth, availableSize.y / contentHeight));
            menuManager.desktopScalingRoot.localScale = scale * Vector3.one;
        }

        public void CustomUpdate()
        {
            UpdateDesktopMenuScale();
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                isMenuOpen = true;
                desktopCanvasGroup.alpha = 1f;
                desktopCanvasGroup.blocksRaycasts = true;
                boneAttachment.AttachToLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, makeDesktopCanvasWorkWhileHoldingTab);
                makeDesktopCanvasWorkWhileHoldingTab.localPosition = Vector3.zero;
                makeDesktopCanvasWorkWhileHoldingTab.localRotation = Quaternion.identity;
                makeDesktopCanvasWorkWhileHoldingTab.gameObject.SetActive(true);
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                isMenuOpen = false;
                desktopCanvasGroup.alpha = 0f;
                desktopCanvasGroup.blocksRaycasts = false;
                makeDesktopCanvasWorkWhileHoldingTab.gameObject.SetActive(false);
                boneAttachment.DetachFromLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, makeDesktopCanvasWorkWhileHoldingTab);
            }
        }
    }
}
