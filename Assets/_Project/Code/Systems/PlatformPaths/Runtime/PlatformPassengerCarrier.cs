using System.Collections.Generic;
using UnityEngine;

namespace Project.PlatformPaths
{
    internal sealed class PlatformPassengerCarrier
    {
        private readonly HashSet<Transform> passengers =
            new HashSet<Transform>();

        private readonly HashSet<Transform> detectedPassengers =
            new HashSet<Transform>();

        private readonly List<Transform> releaseBuffer =
            new List<Transform>();

        private readonly Collider[] overlapBuffer = new Collider[32];

        private Transform riderAnchor;

        public string PassengerTag { get; set; } = "Player";

        public bool HasPassengers
        {
            get
            {
                foreach (Transform passenger in passengers)
                {
                    PlatformRider rider = PassengerRider(passenger);
                    if (passenger != null && rider != null && rider.IsRiding)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Capture(
            Transform platform,
            Collider collider)
        {
            if (platform == null ||
                collider == null ||
                !IsPassengerCollider(collider))
            {
                return;
            }

            Transform passenger =
                ResolvePassengerTransform(collider);
            if (passenger == null)
            {
                return;
            }

            PlatformRider rider = PassengerRider(passenger);
            if (rider == null ||
                rider.IsRiding ||
                IsJumping(passenger))
            {
                return;
            }

            Collider platformCollider =
                platform.GetComponent<Collider>();
            if (platformCollider == null)
            {
                return;
            }

            CapsuleCollider capsule =
                passenger.GetComponent<CapsuleCollider>();
            Bounds passengerBounds = capsule != null
                ? capsule.bounds
                : collider.bounds;
            if (passengerBounds.min.y >
                platformCollider.bounds.max.y + 0.12f)
            {
                return;
            }

            float footOffset =
                passenger.position.y - passengerBounds.min.y;
            Vector3 targetPosition = passenger.position;
            targetPosition.y =
                platformCollider.bounds.max.y + footOffset + 0.02f;
            rider.Attach(
                EnsureRiderAnchor(platform),
                targetPosition,
                passenger.rotation);
            passengers.Add(passenger);
        }

        public void Refresh(
            Transform platform,
            float checkHeight,
            float checkWidth)
        {
            if (platform == null)
            {
                ReleaseAll();
                return;
            }

            Collider platformCollider =
                platform.GetComponent<Collider>();
            if (platformCollider == null)
            {
                ReleaseAll();
                return;
            }

            Bounds bounds = platformCollider.bounds;
            float zoneHeight = Mathf.Min(checkHeight, 0.3f);
            float halfHeight = Mathf.Max(0.05f, zoneHeight) * 0.5f;
            Vector3 center = new Vector3(
                bounds.center.x,
                bounds.max.y + halfHeight * 0.5f,
                bounds.center.z);
            Vector3 halfExtents = new Vector3(
                Mathf.Max(0.05f, bounds.extents.x * checkWidth),
                halfHeight,
                Mathf.Max(0.05f, bounds.extents.z * checkWidth));
            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                overlapBuffer,
                platform.rotation,
                ~0,
                QueryTriggerInteraction.Collide);

            detectedPassengers.Clear();
            for (int index = 0; index < hitCount; index++)
            {
                Collider collider = overlapBuffer[index];
                if (collider == null ||
                    !IsPassengerCollider(collider) ||
                    collider.bounds.min.y >
                    bounds.max.y + 0.12f)
                {
                    continue;
                }

                Transform passenger =
                    ResolvePassengerTransform(collider);
                if (passenger == null)
                {
                    continue;
                }

                detectedPassengers.Add(passenger);
                if (!IsJumping(passenger))
                {
                    Capture(platform, collider);
                }
            }

            releaseBuffer.Clear();
            foreach (Transform passenger in passengers)
            {
                if (passenger == null ||
                    !detectedPassengers.Contains(passenger) ||
                    IsJumping(passenger))
                {
                    releaseBuffer.Add(passenger);
                }
            }

            for (int index = 0;
                 index < releaseBuffer.Count;
                 index++)
            {
                Release(releaseBuffer[index]);
            }
        }

        public void Release(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            Release(ResolvePassengerTransform(collider));
        }

        public void Release(Transform passenger)
        {
            if (passenger == null)
            {
                return;
            }

            PassengerRider(passenger)?.Detach();
            passengers.Remove(passenger);
        }

        public void Carry(Transform platform)
        {
            UpdateRiderAnchor(platform);
        }

        public void ReleaseAll()
        {
            foreach (Transform passenger in passengers)
            {
                PassengerRider(passenger)?.Detach();
            }

            passengers.Clear();
        }

        private static bool IsJumping(Transform passenger)
        {
            Rigidbody body =
                passenger.GetComponent<Rigidbody>();
            return body != null &&
                   body.linearVelocity.y > 0.05f;
        }

        private bool IsPassengerCollider(Collider collider)
        {
            if (string.IsNullOrWhiteSpace(PassengerTag))
            {
                return false;
            }

            if (collider.CompareTag(PassengerTag))
            {
                return true;
            }

            Transform root = collider.transform.root;
            return root != null && root.CompareTag(PassengerTag);
        }

        private static Transform ResolvePassengerTransform(
            Collider collider)
        {
            Rigidbody body = collider.attachedRigidbody;
            return body != null
                ? body.transform
                : collider.transform.root;
        }

        private static PlatformRider PassengerRider(
            Transform passenger)
        {
            if (passenger == null)
            {
                return null;
            }

            PlatformRider rider =
                passenger.GetComponent<PlatformRider>();
            return rider != null
                ? rider
                : passenger.gameObject.AddComponent<PlatformRider>();
        }

        private Transform EnsureRiderAnchor(Transform platform)
        {
            if (riderAnchor != null &&
                riderAnchor.parent == platform)
            {
                UpdateRiderAnchor(platform);
                return riderAnchor;
            }

            Transform existing = platform.Find("__RiderAnchor");
            if (existing == null)
            {
                GameObject anchor = new GameObject("__RiderAnchor");
                anchor.transform.SetParent(platform, false);
                existing = anchor.transform;
            }

            riderAnchor = existing;
            UpdateRiderAnchor(platform);
            return riderAnchor;
        }

        private void UpdateRiderAnchor(Transform platform)
        {
            if (riderAnchor == null || platform == null)
            {
                return;
            }

            Vector3 scale = platform.lossyScale;
            riderAnchor.localPosition = Vector3.zero;
            riderAnchor.localRotation = Quaternion.identity;
            riderAnchor.localScale = new Vector3(
                Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x,
                Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y,
                Mathf.Approximately(scale.z, 0f) ? 1f : 1f / scale.z);
        }
    }
}
