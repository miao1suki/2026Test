using System.Collections.Generic;
using UnityEngine;

namespace Project.PlatformPaths
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PlatformRiderZone : MonoBehaviour
    {
        [SerializeField]
        private PlatformMove platform;

        [SerializeField]
        private string playerTag = "Player";

        private readonly Dictionary<Transform, int> contacts =
            new Dictionary<Transform, int>();

        private readonly HashSet<Collider> activeColliders =
            new HashSet<Collider>();

        public void Configure(
            PlatformMove owner,
            string tag)
        {
            platform = owner;
            playerTag = tag;
        }

        private void OnTriggerEnter(Collider other)
        {
            AddContact(other);
        }

        private void OnTriggerStay(Collider other)
        {
            AddContact(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryGetPassenger(other, out Transform passenger))
            {
                return;
            }

            if (!activeColliders.Remove(other) ||
                !contacts.TryGetValue(
                    passenger,
                    out int count))
            {
                return;
            }

            count--;
            if (count > 0)
            {
                contacts[passenger] = count;
                return;
            }

            contacts.Remove(passenger);
            platform?.ReleasePassenger(passenger);
        }

        private void OnDisable()
        {
            contacts.Clear();
            activeColliders.Clear();
            platform?.ReleaseAllPassengers();
        }

        private void AddContact(Collider other)
        {
            if (!TryGetPassenger(other, out Transform passenger))
            {
                return;
            }

            bool newCollider = activeColliders.Add(other);
            if (!newCollider)
            {
                platform?.CapturePassenger(other);
                return;
            }

            if (contacts.TryGetValue(
                    passenger,
                    out int count))
            {
                contacts[passenger] = count + 1;
            }
            else
            {
                contacts[passenger] = 1;
            }

            platform?.CapturePassenger(other);
        }

        private bool TryGetPassenger(
            Collider other,
            out Transform passenger)
        {
            passenger = null;
            if (other == null ||
                string.IsNullOrWhiteSpace(playerTag))
            {
                return false;
            }

            Rigidbody body = other.attachedRigidbody;
            passenger = body != null
                ? body.transform
                : other.transform.root;
            return passenger != null &&
                   (other.CompareTag(playerTag) ||
                    passenger.CompareTag(playerTag));
        }
    }
}
