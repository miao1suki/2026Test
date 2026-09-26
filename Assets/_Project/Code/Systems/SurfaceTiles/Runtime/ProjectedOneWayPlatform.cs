using System.Collections.Generic;
using Project.RopePaths;
using UnityEngine;

namespace Project.SurfaceTiles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ProjectedOneWayPlatform : MonoBehaviour
    {
        private sealed class Contact
        {
            public Collider collider;
            public IProjectedPlatformActor actor;
            public bool ignored;
        }

        [SerializeField]
        private ProjectedPlatformDirections directions =
            ProjectedPlatformDirections.All;

        [SerializeField, Min(0.01f)] private float landingTolerance = 0.08f;
        [SerializeField, Min(0.1f)] private float sensorPadding = 2f;
        [SerializeField, HideInInspector] private BoxCollider platformCollider;
        [SerializeField, HideInInspector] private ProjectedOneWayPlatformSensor sensor;

        private readonly Dictionary<Collider, Contact> contacts =
            new Dictionary<Collider, Contact>();
        private readonly List<Collider> releaseBuffer = new List<Collider>();

        public ProjectedPlatformDirections Directions
        {
            get => directions;
            set => directions = value;
        }

        public float LandingTolerance
        {
            get => landingTolerance;
            set => landingTolerance = Mathf.Max(0.01f, value);
        }

        public BoxCollider PlatformCollider => platformCollider;

        public void EnsureSetup()
        {
            platformCollider = platformCollider != null
                ? platformCollider
                : GetComponent<BoxCollider>();
            if (platformCollider == null)
            {
                return;
            }

            if (sensor == null)
            {
                Transform existing = transform.Find("__ProjectedPlatformSensor");
                GameObject sensorObject;
                if (existing != null)
                {
                    sensorObject = existing.gameObject;
                }
                else
                {
                    sensorObject = new GameObject("__ProjectedPlatformSensor");
                    sensorObject.transform.SetParent(transform, false);
                }

                sensor = sensorObject.GetComponent<ProjectedOneWayPlatformSensor>();
                if (sensor == null)
                {
                    sensor = sensorObject.AddComponent<ProjectedOneWayPlatformSensor>();
                }
            }

            sensor.Configure(this);
            BoxCollider sensorCollider = sensor.GetComponent<BoxCollider>();
            sensorCollider.center = platformCollider.center;
            sensorCollider.size = platformCollider.size + new Vector3(
                sensorPadding * 2f,
                sensorPadding * 2f,
                sensorPadding * 2f);
            sensorCollider.isTrigger = true;
        }

        public bool IsDirectionActive(RopeProjectionDirection direction)
        {
            ProjectedPlatformDirections flag =
                (ProjectedPlatformDirections)(1 << (int)direction);
            return (directions & flag) != 0;
        }

        public static bool ShouldIgnoreCollision(
            bool modeActive,
            bool directionActive,
            bool wasIgnored,
            Bounds platformBounds,
            Bounds actorBounds,
            float verticalVelocity,
            float tolerance)
        {
            if (!modeActive || !directionActive)
            {
                return wasIgnored && platformBounds.Intersects(actorBounds);
            }

            float top = platformBounds.max.y;
            float safeTolerance = Mathf.Max(0.001f, tolerance);
            if (verticalVelocity > 0.05f)
            {
                return true;
            }

            if (wasIgnored)
            {
                return actorBounds.min.y < top + safeTolerance;
            }

            return actorBounds.min.y < top - safeTolerance;
        }

        public void RegisterCandidate(Collider other)
        {
            if (other == null || platformCollider == null ||
                other == platformCollider || other.isTrigger)
            {
                return;
            }

            if (!TryGetActor(other, out IProjectedPlatformActor actor))
            {
                return;
            }

            if (!contacts.TryGetValue(other, out Contact contact))
            {
                contact = new Contact
                {
                    collider = other,
                    actor = actor
                };
                contacts.Add(other, contact);
            }
            else
            {
                contact.actor = actor;
            }

            RefreshContact(contact);
        }

        public void UnregisterCandidate(Collider other)
        {
            if (other == null || !contacts.TryGetValue(other, out Contact contact))
            {
                return;
            }

            SetIgnored(contact, false);
            contacts.Remove(other);
        }

        private void Awake()
        {
            EnsureSetup();
        }

        private void OnEnable()
        {
            EnsureSetup();
        }

        private void FixedUpdate()
        {
            releaseBuffer.Clear();
            foreach (KeyValuePair<Collider, Contact> pair in contacts)
            {
                Contact contact = pair.Value;
                if (contact.collider == null || contact.actor == null)
                {
                    releaseBuffer.Add(pair.Key);
                    continue;
                }

                RefreshContact(contact);
            }

            for (int index = 0; index < releaseBuffer.Count; index++)
            {
                Collider key = releaseBuffer[index];
                if (key != null && contacts.TryGetValue(key, out Contact contact))
                {
                    SetIgnored(contact, false);
                }

                contacts.Remove(key);
            }
        }

        private void OnDisable()
        {
            foreach (Contact contact in contacts.Values)
            {
                SetIgnored(contact, false);
            }

            contacts.Clear();
        }

        private void OnValidate()
        {
            landingTolerance = Mathf.Max(0.01f, landingTolerance);
            sensorPadding = Mathf.Max(0.1f, sensorPadding);
            platformCollider = platformCollider != null
                ? platformCollider
                : GetComponent<BoxCollider>();
            if (sensor != null && platformCollider != null)
            {
                sensor.Configure(this);
                BoxCollider sensorCollider = sensor.GetComponent<BoxCollider>();
                sensorCollider.center = platformCollider.center;
                sensorCollider.size = platformCollider.size + Vector3.one *
                    (sensorPadding * 2f);
            }
        }

        private void RefreshContact(Contact contact)
        {
            if (platformCollider == null || contact?.collider == null ||
                contact.actor == null)
            {
                return;
            }

            Rigidbody body = contact.actor.ProjectedPlatformBody;
            float velocity = body != null ? body.linearVelocity.y : 0f;
            bool ignore = ShouldIgnoreCollision(
                contact.actor.IsProjectedPlatformModeActive,
                IsDirectionActive(contact.actor.ProjectedPlatformDirection),
                contact.ignored,
                platformCollider.bounds,
                contact.collider.bounds,
                velocity,
                landingTolerance);
            SetIgnored(contact, ignore);
        }

        private void SetIgnored(Contact contact, bool ignored)
        {
            if (contact == null || contact.collider == null ||
                platformCollider == null || contact.ignored == ignored)
            {
                return;
            }

            Physics.IgnoreCollision(platformCollider, contact.collider, ignored);
            contact.ignored = ignored;
        }

        private static bool TryGetActor(
            Collider collider,
            out IProjectedPlatformActor actor)
        {
            actor = null;
            if (collider == null)
            {
                return false;
            }

            Component[] components = collider.GetComponentsInParent<Component>(true);
            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] is IProjectedPlatformActor candidate)
                {
                    actor = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
