using UnityEngine;

namespace Project.RopePaths
{
    [DisallowMultipleComponent]
    public sealed class RopePlatform : MonoBehaviour
    {
        [SerializeField]
        private RopePathNetwork network;

        [SerializeField]
        private RopeSegment boundSegment;

        [SerializeField]
        private RopeEndpoint boundEndpoint = RopeEndpoint.A;

        [SerializeField]
        private bool isBound;

        public RopePathNetwork Network => network;
        public RopeSegment BoundSegment => boundSegment;
        public RopeEndpoint BoundEndpoint => boundEndpoint;
        public bool IsBound => isBound && boundSegment != null;

        public bool Bind(
            RopePathNetwork pathNetwork,
            RopeSegment segment,
            RopeEndpoint endpoint)
        {
            if (pathNetwork == null || segment == null)
            {
                return false;
            }

            network = pathNetwork;
            boundSegment = segment;
            boundEndpoint = endpoint;
            isBound = true;
            return true;
        }

        public void ClearBinding()
        {
            network = null;
            boundSegment = null;
            isBound = false;
        }

        public Vector3 GetBoundWorldPosition()
        {
            return IsBound
                ? boundSegment.GetWorldEndpoint(boundEndpoint)
                : transform.position;
        }

        public void SnapTransformToBinding()
        {
            if (IsBound)
            {
                Vector3 centerOffset = GetVisualCenterWorld() - transform.position;
                transform.position = GetBoundWorldPosition() - centerOffset;
            }
        }

        private Vector3 GetVisualCenterWorld()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return transform.position;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds.center;
        }
    }
}

