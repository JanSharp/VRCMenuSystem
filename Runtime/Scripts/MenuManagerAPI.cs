using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [SingletonScript("d9be4a8a9d454bfb7ba93f4988cbe45a")] // Runtime/Prefabs/Internal/MenuDummy - the system you are using likely provides its own prefab.prefab
    public abstract class MenuManagerAPI : UdonSharpBehaviour
    {
        public abstract void ShowPopupAtItsAnchor(
            RectTransform popup,
            UdonSharpBehaviour callbackInst,
            string callbackEventName);
        public abstract void ShowPopupAtCurrentPosition(
            RectTransform popup,
            UdonSharpBehaviour callbackInst,
            string callbackEventName,
            float minDistanceFromPageEdge = 20f);
        /// <summary>
        /// <para>Can be called recursively.</para>
        /// </summary>
        /// <param name="popup"></param>
        /// <param name="doCallback"></param>
        public abstract void ClosePopup(RectTransform popup, bool doCallback);

        /// <summary>
        /// <para>Use inside of popup callbacks to get the popup which is being closed.</para>
        /// </summary>
        public abstract RectTransform PopupToClose { get; }
    }
}
