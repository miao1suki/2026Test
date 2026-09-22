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

        public string PassengerTag { get; set; } = "Player";

        public bool HasPassengers
        {
            get
            {
                foreach (Transform passenger in passengers)
                {
                    if (passenger != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Refresh(
            Transform platform,
            float checkHeight,
            float checkWidth)
        {
            detectedPassengers.Clear();
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
            float halfHeight = Mathf.Max(0.05f, checkHeight) * 0.5f;
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

            for (int index = 0; index < hitCount; index++)
            {
                Collider collider = overlapBuffer[index];
                if (collider == null ||
                    collider.transform == platform ||
                    string.IsNullOrWhiteSpace(PassengerTag) ||
                    !IsPassengerCollider(collider))
                {
                    continue;
                }

                Transform passenger =
                    ResolvePassengerTransform(collider);
                if (passenger != null)
                {
                    detectedPassengers.Add(passenger);
                }
            }

            releaseBuffer.Clear();
            foreach (Transform passenger in passengers)
            {
                if (passenger == null ||
                    !detectedPassengers.Contains(passenger))
                {
                    releaseBuffer.Add(passenger);
                }
            }

            for (int index = 0;
                 index < releaseBuffer.Count;
                 index++)
            {
                Transform passenger = releaseBuffer[index];
                passengers.Remove(passenger);
            }

            foreach (Transform passenger in detectedPassengers)
            {
                if (passenger == null ||
                    passengers.Contains(passenger))
                {
                    continue;
                }

                passengers.Add(passenger);
            }
        }

        public void Carry(
            Transform platform,
            Vector3 previousPosition,
            Quaternion previousRotation)
        {
            Quaternion inversePreviousRotation =
                Quaternion.Inverse(previousRotation);

            foreach (Transform passenger in passengers)
            {
                if (passenger == null)
                {
                    continue;
                }

                Vector3 localPosition = inversePreviousRotation *
                    (passenger.position - previousPosition);
                Quaternion localRotation = inversePreviousRotation *
                    passenger.rotation;
                Vector3 worldPosition = platform.position +
                    platform.rotation * localPosition;
                Quaternion worldRotation =
                    platform.rotation * localRotation;
                SetPassengerTransform(
                    passenger,
                    worldPosition,
                    worldRotation);
            }
        }

        public void ReleaseAll()
        {
            passengers.Clear();
        }

        private bool IsPassengerCollider(Collider collider)
        {
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

        private static void SetPassengerTransform(
            Transform passenger,
            Vector3 position,
            Quaternion rotation)
        {
            CharacterController controller =
                passenger.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                passenger.SetPositionAndRotation(position, rotation);
                controller.enabled = true;
                return;
            }

            Rigidbody body = passenger.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                body.position = position;
                body.rotation = rotation;
                return;
            }

            passenger.SetPositionAndRotation(position, rotation);
        }
    }
}
