using UnityEngine;

namespace Project.SurfaceTiles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ProjectedOneWayPlatformSensor : MonoBehaviour
    {
        [SerializeField] private ProjectedOneWayPlatform owner;

        public void Configure(ProjectedOneWayPlatform value)
        {
            owner = value;
            BoxCollider box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            owner?.RegisterCandidate(other);
        }

        private void OnTriggerStay(Collider other)
        {
            owner?.RegisterCandidate(other);
        }

        private void OnTriggerExit(Collider other)
        {
            owner?.UnregisterCandidate(other);
        }
    }
}
