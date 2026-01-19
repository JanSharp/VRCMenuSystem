using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    public enum MenuManagerEventType
    {
        /// <summary>
        /// <para>Raised inside of regular Unity <c>Start</c>, after the <see cref="MenuManagerAPI"/> has been
        /// initialized, though notably before <see cref="LockstepAPI.IsInitialized"/> becomes
        /// <see langword="true"/>. Which is normal for <c>Start</c> anyway to be clear.</para>
        /// <para>Useful in order to initialize page specific scripts without having to worry about timing
        /// issues due to pages being disabled until they get shown, which would make <c>Start</c> run likely
        /// after the page script had already received other events.</para>
        /// <para>Only children of a <see cref="MenuManagerAPI"/> receive events of that associated
        /// manager.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnMenuManagerStart,
        /// <summary>
        /// <para>Raised whenever the <see cref="MenuManagerAPI.ActivePage"/> has changed.</para>
        /// <para>It is raised instantly, do not change the active page within the raised event as that would
        /// cause recursion and break.</para>
        /// <para>Only children of a <see cref="MenuManagerAPI"/> receive events of that associated
        /// manager.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnMenuActivePageChanged,
        /// <summary>
        /// <para>Raised whenever the <see cref="MenuManagerAPI.IsMenuOpen"/> state has changed.</para>
        /// <para>It is raised instantly, do not change the menu open state within the raised event as that
        /// would cause recursion and break.</para>
        /// <para>Only children of a <see cref="MenuManagerAPI"/> receive events of that associated
        /// manager.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnMenuOpenStateChanged,
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
        public abstract void PushShownPopupOntoPage(RectTransform popup, float minDistanceFromPageEdge = 20f);

        /// <summary>
        /// <para>Can be <see langword="null"/> in which case no pages are visible at all.</para>
        /// <para>Whenever this value has been changed
        /// <see cref="MenuManagerEventType.OnMenuActivePageChanged"/> gets raised.</para>
        /// </summary>
        public abstract MenuPageRoot ActivePage { get; }
        /// <inheritdoc cref="ActivePage"/>
        public abstract string ActivePageInternalName { get; }
        /// <summary>
        /// <para>Is the menu as a whole currently open/visible?</para>
        /// <para>The menu manager does not actually manage the menu being open/visible, writing to this
        /// property does nothing but change its value and raising
        /// <see cref="MenuManagerEventType.OnMenuOpenStateChanged"/>.</para>
        /// <para>This property exists for systems which do manage the menu's open state with other systems
        /// that wish to check the open state.</para>
        /// </summary>
        public abstract bool IsMenuOpen { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="internalName"></param>
        /// <returns><see langword="null"/> if there is no page with the given
        /// <paramref name="internalName"/>.</returns>
        public abstract MenuPageRoot GetPageRoot(string internalName);
        /// <summary>
        /// </summary>
        /// <param name="internalName"></param>
        /// <returns><see langword="true"/> if a page with the given <paramref name="internalName"/> exists
        /// and its <see cref="MenuPageRoot.ShouldBeShown"/> is currently <see langword="true"/>.
        /// <see langword="false"/> means nothing happened.</returns>
        public abstract bool SetActivePage(string internalName);
        /// <summary>
        /// </summary>
        /// <param name="pageRoot"></param>
        /// <returns><see langword="true"/> if <see cref="MenuPageRoot.ShouldBeShown"/> for
        /// <paramref name="pageRoot"/> is currently <see langword="true"/>. <see langword="false"/> means
        /// nothing happened.</returns>
        public abstract bool SetActivePage(MenuPageRoot pageRoot);

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
