using UnityEngine;

namespace Project.PlatformPaths
{
    [DisallowMultipleComponent]
    public sealed class PlatformRider : MonoBehaviour
    {
        private Transform originalParent;
        private Rigidbody body;
        private bool originalUseGravity;
        private bool originalIsKinematic;
        private RigidbodyInterpolation originalInterpolation;

        public bool IsRiding { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void Attach(
            Transform anchor,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            if (IsRiding || anchor == null)
            {
                return;
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            originalParent = transform.parent;
            if (body != null)
            {
                originalUseGravity = body.useGravity;
                originalIsKinematic = body.isKinematic;
                originalInterpolation = body.interpolation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
                body.interpolation =
                    RigidbodyInterpolation.None;
            }

            transform.SetParent(anchor, true);
            transform.SetPositionAndRotation(
                worldPosition,
                worldRotation);
            IsRiding = true;
        }

        public void Detach()
        {
            if (!IsRiding)
            {
                return;
            }

            transform.SetParent(originalParent, true);
            if (body != null)
            {
                body.isKinematic = originalIsKinematic;
                body.useGravity = originalUseGravity;
                body.interpolation = originalInterpolation;
            }

            IsRiding = false;
        }

        private void LateUpdate()
        {
            if (!IsRiding || body == null)
            {
                return;
            }

            body.position = transform.position;
            body.rotation = transform.rotation;
        }

        private void OnDisable()
        {
            Detach();
        }
    }
}
