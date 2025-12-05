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

    public enum MenuPositionType
    {
        InFront,
        LeftHand,
        RightHand,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [SingletonScript("d9be4a8a9d454bfb7ba93f4988cbe45a")] // Runtime/Prefabs/Internal/MenuDummy - the system you are using likely provides its own prefab.prefab
    public class MenuInputHandler : UdonSharpBehaviour
    {
        [SerializeField][HideInInspector][SingletonReference] private UpdateManager updateManager;
        [HideInInspector][SerializeField][SingletonReference] private BoneAttachmentManager boneAttachment;
        /// <summary>For the <see cref="UpdateManager"/>.</summary>
        [System.NonSerialized] public int customUpdateInternalIndex;

        public MenuManager menuManager;
        public MenuOpenCloseKeyBind keyBind;
        [SerializeField] private MenuPositionType menuPosition;
        public float pixelsFromDesktopScreenEdge = 50f;
        public Transform makeDesktopCanvasWorkWhileHoldingTab;

        public MenuPositionType MenuPosition
        {
            get => menuPosition;
            set
            {
                if (menuPosition == value)
                    return;
                menuPosition = value;
                if (!isMenuOpen)
                    return;
                boneAttachment.DetachFromLocalTrackingData(menuAttachedTrackingType, vrPositioningRoot);
                UpdateMenuAttachedTrackingType();
                boneAttachment.AttachToLocalTrackingData(menuAttachedTrackingType, vrPositioningRoot);
                UpdateMenuLocalPosition();
            }
        }

        private VRCPlayerApi localPlayer;
        private VRCPlayerApi.TrackingDataType menuAttachedTrackingType;

        private Quaternion handRotationNormalization = Quaternion.AngleAxis(90f, Vector3.forward) * Quaternion.AngleAxis(45f, Vector3.right);
        public Vector3 leftHandOffsetPosition;
        public Vector3 leftHandOffsetRotation;
        public Vector3 rightHandOffsetPosition;
        public Vector3 rightHandOffsetRotation;
        public Vector3 headOffsetPosition;
        public Vector3 headOffsetRotation;

        public float upThreshold = 0.3f;
        public float downThreshold = -0.3f;

        public float holdDownTimer = 1.5f;
        public float doubleInputTimeout = 0.75f;

        // Down as in "input key down" equivalent
        // Down as in "look down"
        private float previousDownDownTime;
        private float currentDownDownTime;

        private bool isHoldingDown;
        private bool holdDownActuated;

        private bool isInVR;

        private bool isMenuOpen;

        private CanvasGroup vrCanvasGroup;
        private Transform vrPositioningRoot;
        private CanvasGroup desktopCanvasGroup;
        private Collider mainCanvasCollider;
        private Collider sideCanvasCollider;
        private RectTransform desktopCanvas;
        private Vector2 lastDesktopCanvasSize = -Vector2.one;

        public void Start()
        {
            localPlayer = Networking.LocalPlayer;
            isInVR = localPlayer.IsUserInVR();

            UpdateMenuAttachedTrackingType();
            vrCanvasGroup = menuManager.vrCanvasGroup;
            vrPositioningRoot = menuManager.vrPositioningRoot;
            desktopCanvasGroup = menuManager.desktopCanvasGroup;
            mainCanvasCollider = menuManager.mainCanvasCollider;
            sideCanvasCollider = menuManager.sideCanvasCollider;
            desktopCanvas = menuManager.desktopCanvas;

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
                if (value <= downThreshold)
                {
                    if (holdDownActuated || Time.time > (currentDownDownTime + holdDownTimer))
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

            if (value <= downThreshold)
            {
                isHoldingDown = true;
                currentDownDownTime = Time.time;
                if (keyBind == MenuOpenCloseKeyBind.DownDown && currentDownDownTime < previousDownDownTime + doubleInputTimeout)
                {
                    currentDownDownTime = 0f; // Consume this down input, to prevent 3 downs being treated as 2 down downs.
                    OpenCloseInVR();
                }
                return;
            }

            if (value >= upThreshold && keyBind == MenuOpenCloseKeyBind.DownUp && Time.time < previousDownDownTime + doubleInputTimeout)
            {
                previousDownDownTime = 0f; // Consume this down up input.
                OpenCloseInVR();
            }
        }

        private void OpenCloseInVR()
        {
            isMenuOpen = !isMenuOpen;
            vrCanvasGroup.alpha = isMenuOpen ? 1f : 0f;
            vrCanvasGroup.blocksRaycasts = isMenuOpen; // For good measure.
            mainCanvasCollider.enabled = isMenuOpen;
            sideCanvasCollider.enabled = isMenuOpen;

            if (isMenuOpen)
            {
                boneAttachment.AttachToLocalTrackingData(menuAttachedTrackingType, vrPositioningRoot);
                UpdateMenuLocalPosition();
            }
            else
                boneAttachment.DetachFromLocalTrackingData(menuAttachedTrackingType, vrPositioningRoot);
        }

        private void UpdateMenuAttachedTrackingType()
        {
            switch (menuPosition)
            {
                case MenuPositionType.InFront:
                    menuAttachedTrackingType = VRCPlayerApi.TrackingDataType.Origin;
                    break;
                case MenuPositionType.LeftHand:
                    menuAttachedTrackingType = VRCPlayerApi.TrackingDataType.LeftHand;
                    break;
                case MenuPositionType.RightHand:
                    menuAttachedTrackingType = VRCPlayerApi.TrackingDataType.RightHand;
                    break;
            }
        }

        private void UpdateMenuLocalPosition()
        {
            switch (menuPosition)
            {
                case MenuPositionType.InFront:
                    var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                    vrPositioningRoot.position = head.position + headOffsetPosition;
                    vrPositioningRoot.rotation = head.rotation * Quaternion.Euler(headOffsetRotation);
                    break;
                case MenuPositionType.LeftHand:
                    vrPositioningRoot.localPosition = handRotationNormalization * leftHandOffsetPosition;
                    vrPositioningRoot.localRotation = handRotationNormalization * Quaternion.Euler(leftHandOffsetRotation);
                    break;
                case MenuPositionType.RightHand:
                    vrPositioningRoot.localPosition = handRotationNormalization * rightHandOffsetPosition;
                    vrPositioningRoot.localRotation = handRotationNormalization * Quaternion.Euler(rightHandOffsetRotation);
                    break;
            }
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
