using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ShowPageByPermission : PermissionResolver
    {
        public MenuPageRoot menuPageRoot;

        public WhenConditionsAreMetType whenConditionsAreMet;

        public bool[] logicalAnds;
        [SerializeField] private string[] assetGuids;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public string[] AssetGuids => assetGuids;
#endif
        public PermissionDefinition[] permissionDefs;

        [HideInInspector][SerializeField] private bool isIgnored;
        public bool IsIgnored
        {
            get => isIgnored;
            set
            {
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

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
#if MENU_SYSTEM_DEBUG
            Debug.Log($"[MenuSystemDebug] ShowPageByPermission  Resolve");
#endif
            if (isIgnored)
                return;
            bool conditionsMatching = PermissionsUtil.ResolveConditionsList(logicalAnds, permissionDefs);
            PageShouldBeShown = (whenConditionsAreMet == WhenConditionsAreMetType.Show) == conditionsMatching;
        }
    }
}
