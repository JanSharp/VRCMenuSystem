using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    public enum MenuManagerEventType
    {
        /// <summary>
        /// <para>Raised inside of regular Unity <c>Start</c>, after the <see cref="MenuManagerAPI"/> has been
        /// initialized, though notably before <see cref="LockstepAPI.IsInitialized"/>. Which is normal for
        /// <c>Start</c> anyway to be clear.</para>
        /// <para>Useful in order to initialize page specific scripts without having to worry about timing
        /// issues due to pages being disabled until they get shown, which would make <c>Start</c> run likely
        /// after the page script had already received other events.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnMenuManagerStart,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class MenuManagerEventAttribute : CustomRaisedEventBaseAttribute
    {
        /// <summary>
        /// <para>The method this attribute gets applied to must be public.</para>
        /// <para>The name of the function this attribute is applied to must have the exact same name as the
        /// name of the <paramref name="eventType"/>.</para>
        /// <para>Event registration is performed at OnBuild, which is to say that scripts with these kinds of
        /// event handlers must exist in the scene at build time, any runtime instantiated objects with these
        /// scripts on them will not receive these events.</para>
        /// <para>Disabled scripts still receive events.</para>
        /// </summary>
        /// <param name="eventType">The event to register this function as a listener to.</param>
        public MenuManagerEventAttribute(MenuManagerEventType eventType)
            : base((int)eventType)
        { }
    }

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
        /// <para>Can be <see langword="null"/> in which case no pages are visible at all.</para>
        /// <para>Whenever this value has been changed
        /// <see cref="RegisterOnMenuActivePageChanged(UdonSharpBehaviour)"/> gets raised.</para>
        /// </summary>
        public abstract string ActivePageInternalName { get; }
        /// <summary>
        /// <para>Is the menu as a whole currently open/visible?</para>
        /// <para>The menu manager does not actually manage the menu being open/visible, writing to this
        /// property does nothing but change its value and raising
        /// <see cref="RegisterOnMenuOpenStateChanged(UdonSharpBehaviour)"/>.</para>
        /// <para>This property exists for systems which do manage the menu's open state with other systems
        /// that wish to check the open state.</para>
        /// </summary>
        public abstract bool IsMenuOpen { get; set; }

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

        public const string OnMenuActivePageChangedEventName = "OnMenuActivePageChanged";
        /// <summary>
        /// <para>Register a behaviour for the <c>OnMenuActivePageChanged</c> event.</para>
        /// <para><c>OnMenuActivePageChanged</c> is raised whenever the <see cref="ActivePageInternalName"/>
        /// has changed.</para>
        /// <para>It is raised instantly, do not change the active page within the raised event as that would
        /// cause recursion and break.</para>
        /// </summary>
        public abstract void RegisterOnMenuActivePageChanged(UdonSharpBehaviour listener);

        public const string OnMenuOpenStateChangedEventName = "OnMenuOpenStateChanged";
        /// <summary>
        /// <para>Register a behaviour for the <c>OnMenuOpenStateChanged</c> event.</para>
        /// <para><c>OnMenuOpenStateChanged</c> is raised whenever the <see cref="IsMenuOpen"/> has
        /// changed.</para>
        /// <para>It is raised instantly, do not change the menu open state within the raised event as that
        /// would cause recursion and break.</para>
        /// </summary>
        public abstract void RegisterOnMenuOpenStateChanged(UdonSharpBehaviour listener);
    }
}
