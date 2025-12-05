using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TestVRKeyBindsAndPositioning : UdonSharpBehaviour
    {
        [SerializeField][HideInInspector][SingletonReference] private MenuInputHandler menuInputHandler;
        [SerializeField][HideInInspector][SingletonReference] private WidgetManager widgetManager;

        public GenericValueEditor valueEditor;

        private ToggleFieldWidgetData downUpToggle;
        private ToggleFieldWidgetData downDownToggle;
        private ToggleFieldWidgetData holdDownToggle;

        private ToggleFieldWidgetData inFrontToggle;
        private ToggleFieldWidgetData leftHeldToggle;
        private ToggleFieldWidgetData rightHeldToggle;

        public void Start()
        {
            WidgetData[] widgets = new WidgetData[ArrList.MinCapacity];
            int widgetsCount = 0;

            string[] vectors = new string[]
            {
                nameof(MenuInputHandler.headOffsetPosition),
                nameof(MenuInputHandler.headOffsetRotation),
                nameof(MenuInputHandler.leftHandOffsetPosition),
                nameof(MenuInputHandler.leftHandOffsetRotation),
                nameof(MenuInputHandler.rightHandOffsetPosition),
                nameof(MenuInputHandler.rightHandOffsetRotation),
            };
            foreach (string vector in vectors)
                ArrList.Add(ref widgets, ref widgetsCount, widgetManager
                    .NewVector3Field(vector, (Vector3)menuInputHandler.GetProgramVariable(vector))
                    .SetCustomData(nameof(vectorFieldName), vector)
                    .SetListener(this, nameof(OnVectorValueChanged)));

            string[] floats = new string[]
            {
                nameof(MenuInputHandler.upThreshold),
                nameof(MenuInputHandler.downThreshold),
                nameof(MenuInputHandler.holdDownTimer),
                nameof(MenuInputHandler.doubleInputTimeout),
                nameof(MenuInputHandler.headAttachedScale),
                nameof(MenuInputHandler.leftHandAttachedScale),
                nameof(MenuInputHandler.rightHandAttachedScale),
            };
            foreach (string floatField in floats)
                ArrList.Add(ref widgets, ref widgetsCount, widgetManager
                    .NewFloatField(floatField, (float)menuInputHandler.GetProgramVariable(floatField))
                    .SetCustomData(nameof(floatFieldName), floatField)
                    .SetListener(this, nameof(OnFloatValueChanged)));

            ArrList.Add(ref widgets, ref widgetsCount, widgetManager.NewSpace());

            ArrList.Add(ref widgets, ref widgetsCount, downUpToggle = (ToggleFieldWidgetData)widgetManager
                .NewToggleField("Down Up", menuInputHandler.keyBind == MenuOpenCloseKeyBind.DownUp)
                .SetListener(this, nameof(SetToDownUp)));
            ArrList.Add(ref widgets, ref widgetsCount, downDownToggle = (ToggleFieldWidgetData)widgetManager
                .NewToggleField("Down Down", menuInputHandler.keyBind == MenuOpenCloseKeyBind.DownDown)
                .SetListener(this, nameof(SetToDownDown)));
            ArrList.Add(ref widgets, ref widgetsCount, holdDownToggle = (ToggleFieldWidgetData)widgetManager
                .NewToggleField("Hold Down", menuInputHandler.keyBind == MenuOpenCloseKeyBind.HoldDown)
                .SetListener(this, nameof(SetToHoldDown)));

            ArrList.Add(ref widgets, ref widgetsCount, widgetManager.NewSpace());

            ArrList.Add(ref widgets, ref widgetsCount, inFrontToggle = (ToggleFieldWidgetData)widgetManager
                .NewToggleField("In Front", menuInputHandler.MenuPosition == MenuPositionType.InFront)
                .SetListener(this, nameof(SetToInFront)));
            ArrList.Add(ref widgets, ref widgetsCount, leftHeldToggle = (ToggleFieldWidgetData)widgetManager
                .NewToggleField("Left Hand", menuInputHandler.MenuPosition == MenuPositionType.LeftHand)
                .SetListener(this, nameof(SetToLeftHand)));
            ArrList.Add(ref widgets, ref widgetsCount, rightHeldToggle = (ToggleFieldWidgetData)widgetManager
                .NewToggleField("Right Hand", menuInputHandler.MenuPosition == MenuPositionType.RightHand)
                .SetListener(this, nameof(SetToRightHand)));

            valueEditor.Draw(widgets, widgetsCount);
        }

        [System.NonSerialized] public string vectorFieldName;
        public void OnVectorValueChanged()
        {
            menuInputHandler.SetProgramVariable(vectorFieldName, valueEditor.GetSendingVector3Field().Value);
        }

        [System.NonSerialized] public string floatFieldName;
        public void OnFloatValueChanged()
        {
            menuInputHandler.SetProgramVariable(floatFieldName, valueEditor.GetSendingDecimalField().FloatValue);
        }

        public void SetToDownUp()
        {
            downUpToggle.SetValueWithoutNotify(true);
            downDownToggle.SetValueWithoutNotify(false);
            holdDownToggle.SetValueWithoutNotify(false);
            menuInputHandler.keyBind = MenuOpenCloseKeyBind.DownUp;
        }

        public void SetToDownDown()
        {
            downUpToggle.SetValueWithoutNotify(false);
            downDownToggle.SetValueWithoutNotify(true);
            holdDownToggle.SetValueWithoutNotify(false);
            menuInputHandler.keyBind = MenuOpenCloseKeyBind.DownDown;
        }

        public void SetToHoldDown()
        {
            downUpToggle.SetValueWithoutNotify(false);
            downDownToggle.SetValueWithoutNotify(false);
            holdDownToggle.SetValueWithoutNotify(true);
            menuInputHandler.keyBind = MenuOpenCloseKeyBind.HoldDown;
        }

        public void SetToInFront()
        {
            inFrontToggle.SetValueWithoutNotify(true);
            leftHeldToggle.SetValueWithoutNotify(false);
            rightHeldToggle.SetValueWithoutNotify(false);
            menuInputHandler.MenuPosition = MenuPositionType.InFront;
        }

        public void SetToLeftHand()
        {
            inFrontToggle.SetValueWithoutNotify(false);
            leftHeldToggle.SetValueWithoutNotify(true);
            rightHeldToggle.SetValueWithoutNotify(false);
            menuInputHandler.MenuPosition = MenuPositionType.LeftHand;
        }

        public void SetToRightHand()
        {
            inFrontToggle.SetValueWithoutNotify(false);
            leftHeldToggle.SetValueWithoutNotify(false);
            rightHeldToggle.SetValueWithoutNotify(true);
            menuInputHandler.MenuPosition = MenuPositionType.RightHand;
        }
    }
}
