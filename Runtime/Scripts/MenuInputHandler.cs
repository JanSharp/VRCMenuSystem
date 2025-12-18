using JanSharp.Internal;
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
        [HideInInspector][SerializeField][SingletonReference] private BoneAttachmentManager boneAttachment;

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
                if (isMenuOpen)
                    boneAttachment.DetachFromLocalTrackingData(menuAttachedTrackingType, vrPositioningRoot);
                UpdateMenuAttachedTrackingType();
                if (!isMenuOpen)
                    return;
                boneAttachment.AttachToLocalTrackingData(menuAttachedTrackingType, vrPositioningRoot);
                UpdateMenuLocalPosition();
            }
        }

        private VRCPlayerApi localPlayer;
        private VRCPlayerApi.TrackingDataType menuAttachedTrackingType;

        private Quaternion handRotationNormalization = Quaternion.AngleAxis(90f, Vector3.forward) * Quaternion.AngleAxis(45f, Vector3.right);
        [System.NonSerialized] public Vector3 headOffsetPosition = new Vector3(0f, 0f, 0.9f);
        [System.NonSerialized] public Vector3 headOffsetRotation = new Vector3(0f, 0f, 0f);
        [System.NonSerialized] public Vector3 leftHandOffsetPosition = new Vector3(0.1f, 0.55f, 0.15f);
        [System.NonSerialized] public Vector3 leftHandOffsetRotation = new Vector3(-35f, -15f, 0f);
        [System.NonSerialized] public Vector3 rightHandOffsetPosition = new Vector3(-0.125f, 0.18f, 0.24f);
        [System.NonSerialized] public Vector3 rightHandOffsetRotation = new Vector3(55f, 5f, -10f);
        [System.NonSerialized] public float headAttachedScale = 0.001f;
        [System.NonSerialized] public float leftHandAttachedScale = 0.00075f;
        [System.NonSerialized] public float rightHandAttachedScale = 0.0005f;

        [System.NonSerialized] public float upThreshold = 0.1f;
        [System.NonSerialized] public float downThreshold = -0.1f;

        [System.NonSerialized] public float holdDownTimer = 1.5f;
        [System.NonSerialized] public float doubleInputTimeout = 0.75f;

        private float lookVerticalValue;
        // Press as in "input key down" equivalent
        // Down as in "vertical look input down/negative"
        private float previousPressDownTime;
        private float currentPressDownTime;

        private bool isHoldingDown;
        private bool holdDownActuated;
        private int ignoreJoystickInputCounter;

        private bool isInVR;

        private bool isMenuOpen;

        private Transform vrPositioningRoot;
        private GameObject vrRootGo;
        private RectTransform desktopCanvas;
        private GameObject desktopCanvasGo;
        private Vector2 lastDesktopCanvasSize = -Vector2.one;

        public void Start()
        {
            localPlayer = Networking.LocalPlayer;
            isInVR = localPlayer.IsUserInVR();

            UpdateMenuAttachedTrackingType();
            vrPositioningRoot = menuManager.vrPositioningRoot;
            vrRootGo = vrPositioningRoot.gameObject;
            desktopCanvas = menuManager.desktopCanvas;
            desktopCanvasGo = desktopCanvas.gameObject;

            if (isInVR)
            {
                Destroy(desktopCanvasGo);
                isMenuOpen = true;
                OpenCloseInVR(); // Close.
                return;
            }
            MoveMenuIntoScreenCanvas();
        }

        public void IgnoreJoystickInput()
        {
            ignoreJoystickInputCounter++;
            isHoldingDown = false;
            holdDownActuated = false;
            previousPressDownTime = 0f;
            currentPressDownTime = 0f;
        }

        public void UnignoreJoystickInput()
        {
            if (ignoreJoystickInputCounter == 0)
            {
                Debug.LogError("[MenuSystem] Attempt to UnignoreJoystickInput more often than IgnoreJoystickInput.");
                return;
            }
            ignoreJoystickInputCounter--;
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            // Only gets raised if the value changed it would seem.
            lookVerticalValue = value;
        }

        private void UpdateVRInput()
        {
            if (ignoreJoystickInputCounter != 0)
                return;

            if (isHoldingDown)
            {
                if (lookVerticalValue <= downThreshold)
                {
                    if (holdDownActuated || Time.time < currentPressDownTime + holdDownTimer)
                        return;
                    holdDownActuated = true;
                    if (keyBind == MenuOpenCloseKeyBind.HoldDown)
                        OpenCloseInVR();
                    return;
                }
                isHoldingDown = false;
                holdDownActuated = false;
                previousPressDownTime = currentPressDownTime;
            }

            else if (lookVerticalValue <= downThreshold) // && was not not holding down.
            {
                isHoldingDown = true;
                currentPressDownTime = Time.time;
                if (keyBind == MenuOpenCloseKeyBind.DownDown && currentPressDownTime < previousPressDownTime + doubleInputTimeout)
                {
                    currentPressDownTime = 0f; // Consume this down input, to prevent 3 downs being treated as 2 down downs.
                    OpenCloseInVR();
                }
                return;
            }

            if (lookVerticalValue >= upThreshold && keyBind == MenuOpenCloseKeyBind.DownUp && Time.time < previousPressDownTime + doubleInputTimeout)
            {
                previousPressDownTime = 0f; // Consume this down up input.
                OpenCloseInVR();
            }
        }

        private void OpenCloseInVR()
        {
            isMenuOpen = !isMenuOpen;
            menuManager.IsMenuOpen = isMenuOpen;

            if (isMenuOpen)
            {
                boneAttachment.AttachToLocalTrackingData(menuAttachedTrackingType, vrPositioningRoot);
                UpdateMenuLocalPosition();
                vrRootGo.SetActive(true);
            }
            else
            {
                vrRootGo.SetActive(false);
                boneAttachment.DetachFromLocalTrackingData(menuAttachedTrackingType, vrPositioningRoot);
            }
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
                    // TODO: What if this calculates the deviation from head rotation and projected rotation,
                    // and if it deviates too much it just uses the head rotation. So it's effectively snapping
                    // to the projected rotation, but supports unusual head rotations.
                    var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                    Quaternion headRotation = head.rotation;
                    Vector3 forward = headRotation * Vector3.forward;
                    Quaternion projected = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) >= 0.95f
                        ? headRotation
                        : Quaternion.LookRotation(forward, (headRotation * Vector3.up).y >= 0f ? Vector3.up : Vector3.down);
                    vrPositioningRoot.position = head.position + projected * headOffsetPosition;
                    vrPositioningRoot.rotation = projected * Quaternion.Euler(headOffsetRotation);
                    vrPositioningRoot.localScale = Vector3.one * headAttachedScale;
                    break;
                case MenuPositionType.LeftHand:
                    vrPositioningRoot.localPosition = handRotationNormalization * leftHandOffsetPosition;
                    vrPositioningRoot.localRotation = handRotationNormalization * Quaternion.Euler(leftHandOffsetRotation);
                    vrPositioningRoot.localScale = Vector3.one * leftHandAttachedScale;
                    break;
                case MenuPositionType.RightHand:
                    vrPositioningRoot.localPosition = handRotationNormalization * rightHandOffsetPosition;
                    vrPositioningRoot.localRotation = handRotationNormalization * Quaternion.Euler(rightHandOffsetRotation);
                    vrPositioningRoot.localScale = Vector3.one * rightHandAttachedScale;
                    break;
            }
        }

        private void MoveMenuIntoScreenCanvas()
        {
            vrRootGo.SetActive(false); // Prevent needless UI layout.
            desktopCanvasGo.SetActive(false); // Ensure it's off. Also intentional order of operation.
            Transform source = menuManager.vrRootCanvas;
            Transform destination = menuManager.desktopElementsRoot;
            while (source.childCount != 0)
                source.GetChild(0).SetParent(destination, worldPositionStays: false);
            menuManager.vrRootCanvas = menuManager.desktopElementsRoot;
            Destroy(vrPositioningRoot.gameObject);
            UpdateDesktopMenuScale();
            isMenuOpen = false;
            menuManager.IsMenuOpen = isMenuOpen;
        }

        private void UpdateDesktopMenuScale()
        {
            Vector2 size = desktopCanvas.sizeDelta;
            if (size == lastDesktopCanvasSize)
                return;
            lastDesktopCanvasSize = size;
            Vector2 availableSize = size - Vector2.one * pixelsFromDesktopScreenEdge * 2f;
            float contentWidth = menuManager.mainPanelWidth + menuManager.expandedSideSize * 2f;
            float contentHeight = menuManager.canvasHeight;
            float scale = Mathf.Max(0.1f, Mathf.Min(availableSize.x / contentWidth, availableSize.y / contentHeight));
            menuManager.desktopScalingRoot.localScale = scale * Vector3.one;
        }

        public void Update()
        {
            if (isInVR)
            {
                UpdateVRInput();
                return;
            }

            UpdateDesktopMenuScale();
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                boneAttachment.AttachToLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, makeDesktopCanvasWorkWhileHoldingTab);
                makeDesktopCanvasWorkWhileHoldingTab.localPosition = Vector3.zero;
                makeDesktopCanvasWorkWhileHoldingTab.localRotation = Quaternion.identity;
                makeDesktopCanvasWorkWhileHoldingTab.gameObject.SetActive(true);
                isMenuOpen = true;
                menuManager.IsMenuOpen = isMenuOpen; // Go still inactive to prevent needless layout any custom scripts would trigger.
                desktopCanvasGo.SetActive(true); // Intentional order of operation.
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                desktopCanvasGo.SetActive(false); // Intentional order of operation.
                makeDesktopCanvasWorkWhileHoldingTab.gameObject.SetActive(false);
                boneAttachment.DetachFromLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, makeDesktopCanvasWorkWhileHoldingTab);
                isMenuOpen = false;
                menuManager.IsMenuOpen = isMenuOpen;
            }
        }
    }
}
