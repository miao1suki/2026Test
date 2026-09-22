using System;
using UnityEngine;

namespace Project.RopePaths
{
    [DisallowMultipleComponent]
    public sealed class RopeSegment : MonoBehaviour
    {
        [SerializeField]
        private Vector3 endpointA = new Vector3(-1f, 0f, 0f);

        [SerializeField]
        private Vector3 endpointB = new Vector3(1f, 0f, 0f);

        [SerializeField, HideInInspector]
        private string segmentId;

        public Vector3 LocalEndpointA => endpointA;
        public Vector3 LocalEndpointB => endpointB;
        public string SegmentId => segmentId;

        public Vector3 GetWorldEndpoint(RopeEndpoint endpoint)
        {
            return transform.TransformPoint(
                endpoint == RopeEndpoint.A ? endpointA : endpointB);
        }

        public Vector3 GetWorldDirection(RopeEndpoint endpoint)
        {
            Vector3 delta = GetWorldEndpoint(RopeEndpoint.B) -
                            GetWorldEndpoint(RopeEndpoint.A);
            if (endpoint == RopeEndpoint.A)
            {
                delta = -delta;
            }

            return delta.sqrMagnitude > 0.000001f ? delta.normalized : Vector3.zero;
        }

        public Vector3 GetLocalEndpoint(RopeEndpoint endpoint)
        {
            return endpoint == RopeEndpoint.A ? endpointA : endpointB;
        }

        public void SetLocalEndpoint(RopeEndpoint endpoint, Vector3 localPosition)
        {
            if (endpoint == RopeEndpoint.A)
            {
                endpointA = localPosition;
            }
            else
            {
                endpointB = localPosition;
            }
        }

        public void SetWorldEndpoint(RopeEndpoint endpoint, Vector3 worldPosition)
        {
            SetLocalEndpoint(endpoint, transform.InverseTransformPoint(worldPosition));
        }

        public static RopeEndpoint GetOpposite(RopeEndpoint endpoint)
        {
            return endpoint == RopeEndpoint.A ? RopeEndpoint.B : RopeEndpoint.A;
        }

        private void Reset()
        {
            EnsureId();
        }

        private void OnValidate()
        {
            EnsureId();
        }

        private void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(segmentId))
            {
                segmentId = Guid.NewGuid().ToString("N");
            }
        }
    }
}


