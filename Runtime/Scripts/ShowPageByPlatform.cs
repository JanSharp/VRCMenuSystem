using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ShowPageByPlatform : UdonSharpBehaviour
    {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public const string MenuPageRootPropName = nameof(menuPageRoot);
        public const string IsIgnoredPropName = nameof(isIgnored);
#endif

        [HideInInspector][SerializeField] private MenuPageRoot menuPageRoot;
        public MenuPageRoot MenuPageRoot => menuPageRoot;

        [SerializeField] private bool showInVR = false;
        public bool ShowInVR
        {
            get => showInVR;
            set
            {
                if (showInVR == value)
                    return;
                showInVR = value;
                Resolve();
            }
        }

        [SerializeField] private bool showInDesktop = false;
        public bool ShowInDesktop
        {
            get => showInDesktop;
            set
            {
                if (showInDesktop == value)
                    return;
                showInDesktop = value;
                Resolve();
            }
        }

        [HideInInspector][SerializeField] private bool isIgnored;
        public bool IsIgnored
        {
            get => isIgnored;
            set
            {
                if (isIgnored == value)
                    return;
                isIgnored = value;
                if (isIgnored)
                    PageShouldBeShown = false;
                else
                    Resolve();
            }
        }

        private bool pageShouldBeShown;
        public bool PageShouldBeShown
        {
            get => pageShouldBeShown;
            private set
            {
                if (pageShouldBeShown == value)
                    return;
                pageShouldBeShown = value;
                if (menuPageRoot == null) // TODO: Log error or nah?
                    return;
                if (value)
                    menuPageRoot.IncrementShouldBeShown();
                else
                    menuPageRoot.DecrementShouldBeShown();
            }
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart() => Resolve();

        public void Resolve()
        {
            if (isIgnored)
                return;
            PageShouldBeShown = Networking.LocalPlayer.IsUserInVR() ? showInVR : showInDesktop;
        }
    }
}
