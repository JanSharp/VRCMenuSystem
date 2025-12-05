using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TestInstantiatedPermissionResolvers : UdonSharpBehaviour
    {
        public GameObject evenRow;
        public GameObject oddRow;
        public Transform rowContainer;

        public void OnClick()
        {
            GameObject go = Instantiate((rowContainer.childCount % 2) == 1 ? evenRow : oddRow);
            go.transform.SetParent(rowContainer, worldPositionStays: false);
            foreach (PermissionResolver resolver in go.GetComponentsInChildren<PermissionResolver>(includeInactive: true))
                resolver.InitializeInstantiated();
            go.SetActive(true);
        }
    }
}
