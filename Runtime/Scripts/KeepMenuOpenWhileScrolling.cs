using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class KeepMenuOpenWhileScrolling : UdonSharpBehaviour
    {
        [SerializeField][HideInInspector][SingletonReference] private MenuInputHandler menuInputHandler;

        private bool isIgnoring;
        private int forceResetSendCount;
        /// <summary>
        /// <para>After some time, forcibly unignore joystick input. It is very unlikely for somebody to be
        /// pointing at the same scroll view for a very long time, therefore if we don't get a pointer exit
        /// event for some time, we can assume that something unexpected happened which caused unity to not
        /// raise the exit event.</para>
        /// </summary>
        private const float ForceResetAfter = 120f;

        public void OnPointerEnter()
        {
            if (isIgnoring)
                return;
            isIgnoring = true;
            menuInputHandler.IgnoreJoystickInput();
            forceResetSendCount++;
            SendCustomEventDelayedSeconds(nameof(ForceReset), ForceResetAfter);
        }

        public void ForceReset()
        {
            if ((--forceResetSendCount) != 0)
                return;
            OnPointerExit();
        }

        public void OnPointerExit()
        {
            if (!isIgnoring)
                return;
            isIgnoring = false;
            menuInputHandler.UnignoreJoystickInput();
        }
    }
}
