using System.Collections.Generic;
using Project.RopePaths;
using UnityEngine;

namespace Project.ProjectedPlatforms
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ProjectedOneWayPlatform : MonoBehaviour
    {
        private const string SensorName = "__ProjectedPlatformSensor";
        private const string ProxyName = "__ProjectedCollisionProxy";

        private sealed class Contact
        {
            public Collider collider;
            public IProjectedPlatformActor actor;
            public bool originalIgnored;
            public bool proxyIgnored;
            public bool depthAligned;
            public RopeProjectionDirection alignedDirection;
            public bool pendingDepthAlignment;
            public RopeProjectionDirection pendingDirection;
        }

        [SerializeField]
        private ProjectedPlatformDirections directions =
            ProjectedPlatformDirections.All;

        [SerializeField, Min(0.01f)]
        private float landingTolerance = 0.08f;

        [SerializeField, Min(1f)]
        [Tooltip("2D 正交模式下，临时碰撞代理沿视角纵深延伸的世界长度。")]
        private float projectionDepth = 100f;

        [SerializeField, Min(0.1f)]
        private float sensorPadding = 2f;

        [SerializeField, HideInInspector]
        private BoxCollider platformCollider;

        [SerializeField, HideInInspector]
        private ProjectedOneWayPlatformSensor sensor;

        [SerializeField, HideInInspector]
        private BoxCollider projectionCollider;

        private readonly Dictionary<Collider, Contact> contacts =
            new Dictionary<Collider, Contact>();
        private readonly List<Collider> releaseBuffer = new List<Collider>();
        private RopeProjectionDirection activeDirection;

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

        public float ProjectionDepth
        {
            get => projectionDepth;
            set
            {
                projectionDepth = Mathf.Max(1f, value);
                ConfigureSensorCollider();
                if (projectionCollider != null && projectionCollider.enabled)
                {
                    ConfigureProjectionCollider(activeDirection);
                }
            }
        }

        public BoxCollider PlatformCollider => platformCollider;
        public BoxCollider ProjectionCollider => projectionCollider;
        public Collider ActiveSupportCollider =>
            projectionCollider != null && projectionCollider.enabled
                ? projectionCollider
                : platformCollider;

        public void EnsureSetup()
        {
            platformCollider = platformCollider != null
                ? platformCollider
                : GetComponent<BoxCollider>();
            if (platformCollider == null)
            {
                return;
            }

            sensor = EnsureSensor();
            projectionCollider = EnsureProjectionCollider();
            ConfigureSensorCollider();
            if (!Application.isPlaying && projectionCollider != null)
            {
                projectionCollider.enabled = false;
            }
        }

        public bool IsDirectionActive(RopeProjectionDirection direction)
        {
            ProjectedPlatformDirections flag =
                (ProjectedPlatformDirections)(1 << (int)direction);
            return (directions & flag) != 0;
        }

        public bool TryAlignCandidateToPhysicalDepth(Collider other)
        {
            if (other == null || platformCollider == null ||
                projectionCollider == null ||
                !projectionCollider.enabled)
            {
                return false;
            }

            if (!contacts.TryGetValue(other, out Contact contact))
            {
                if (!TryGetActor(other, out IProjectedPlatformActor actor))
                {
                    return false;
                }

                contact = new Contact
                {
                    collider = other,
                    actor = actor,
                };
                contacts.Add(other, contact);
            }

            RopeProjectionDirection direction =
                contact.actor.ProjectedPlatformDirection;
            if (!contact.actor.IsProjectedPlatformModeActive ||
                activeDirection != direction ||
                !IsDirectionActive(direction))
            {
                return false;
            }

            return TryAlignContactDepth(contact, direction);
        }

        public static Vector3 AlignPositionToPlatformDepth(
            Vector3 actorPosition,
            Vector3 platformCenter,
            RopeProjectionDirection direction)
        {
            Vector3 depth = RopeProjectionUtility.ViewDepth(direction);
            if (Mathf.Abs(depth.x) > 0.5f)
            {
                actorPosition.x = platformCenter.x;
            }
            else
            {
                actorPosition.z = platformCenter.z;
            }

            return actorPosition;
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
            float safeTolerance = Mathf.Max(0.001f, tolerance);
            if (!modeActive || !directionActive)
            {
                return wasIgnored &&
                       platformBounds.Intersects(actorBounds) &&
                       actorBounds.min.y <
                           platformBounds.max.y - safeTolerance;
            }

            float top = platformBounds.max.y;
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
                other == platformCollider || other == projectionCollider ||
                other.isTrigger)
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
                    actor = actor,
                };
                contacts.Add(other, contact);
            }
            else
            {
                contact.actor = actor;
            }

            RefreshProjectionState();
            RefreshContact(contact);
        }

        public void UnregisterCandidate(Collider other)
        {
            if (other == null || !contacts.TryGetValue(other, out Contact contact))
            {
                return;
            }

            ClearIgnored(contact);
            contacts.Remove(other);
            RefreshProjectionState();
        }

        private void Awake()
        {
            EnsureSetup();
        }

        private void OnEnable()
        {
            EnsureSetup();
            RefreshProjectionState();
        }

        private void FixedUpdate()
        {
            RefreshProjectionState();
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
                    ClearIgnored(contact);
                }

                contacts.Remove(key);
            }
        }

        private void OnDisable()
        {
            foreach (Contact contact in contacts.Values)
            {
                ClearIgnored(contact);
            }

            contacts.Clear();
            if (projectionCollider != null)
            {
                projectionCollider.enabled = false;
            }
        }

        private void OnValidate()
        {
            landingTolerance = Mathf.Max(0.01f, landingTolerance);
            projectionDepth = Mathf.Max(1f, projectionDepth);
            sensorPadding = Mathf.Max(0.1f, sensorPadding);
            platformCollider = platformCollider != null
                ? platformCollider
                : GetComponent<BoxCollider>();
            if (platformCollider != null && sensor != null)
            {
                ConfigureSensorCollider();
            }
        }

        private ProjectedOneWayPlatformSensor EnsureSensor()
        {
            if (sensor != null)
            {
                sensor.Configure(this);
                return sensor;
            }

            Transform existing = transform.Find(SensorName);
            GameObject sensorObject = existing != null
                ? existing.gameObject
                : new GameObject(SensorName);
            if (existing == null)
            {
                sensorObject.transform.SetParent(transform, false);
            }

            sensor = sensorObject.GetComponent<ProjectedOneWayPlatformSensor>();
            if (sensor == null)
            {
                sensor = sensorObject.AddComponent<ProjectedOneWayPlatformSensor>();
            }

            sensor.Configure(this);
            return sensor;
        }

        private BoxCollider EnsureProjectionCollider()
        {
            if (projectionCollider != null)
            {
                projectionCollider.isTrigger = false;
                return projectionCollider;
            }

            Transform existing = transform.Find(ProxyName);
            GameObject proxyObject = existing != null
                ? existing.gameObject
                : new GameObject(ProxyName);
            if (existing == null)
            {
                proxyObject.transform.SetParent(transform, false);
            }

            projectionCollider = proxyObject.GetComponent<BoxCollider>();
            if (projectionCollider == null)
            {
                projectionCollider = proxyObject.AddComponent<BoxCollider>();
            }

            projectionCollider.isTrigger = false;
            projectionCollider.enabled = false;
            return projectionCollider;
        }

        private void RefreshProjectionState()
        {
            if (projectionCollider == null || platformCollider == null)
            {
                return;
            }

            bool shouldEnable = false;
            RopeProjectionDirection direction = activeDirection;
            foreach (Contact contact in contacts.Values)
            {
                if (contact?.actor == null ||
                    !contact.actor.IsProjectedPlatformModeActive)
                {
                    continue;
                }

                RopeProjectionDirection candidate =
                    contact.actor.ProjectedPlatformDirection;
                if (!IsDirectionActive(candidate))
                {
                    continue;
                }

                direction = candidate;
                shouldEnable = true;
                break;
            }

            if (!shouldEnable)
            {
                projectionCollider.enabled = false;
                return;
            }

            if (!projectionCollider.enabled || activeDirection != direction)
            {
                activeDirection = direction;
                ConfigureProjectionCollider(direction);
            }

            projectionCollider.enabled = true;
        }

        private void ConfigureSensorCollider()
        {
            if (sensor == null || platformCollider == null)
            {
                return;
            }

            BoxCollider sensorCollider = sensor.GetComponent<BoxCollider>();
            Bounds source = platformCollider.bounds;
            Vector3 size = source.size + Vector3.one * (sensorPadding * 2f);
            size.x = Mathf.Max(size.x, projectionDepth);
            size.z = Mathf.Max(size.z, projectionDepth);
            SetWorldAlignedBox(sensorCollider, source.center, size);
            sensorCollider.isTrigger = true;
        }

        private void ConfigureProjectionCollider(
            RopeProjectionDirection direction)
        {
            if (projectionCollider == null || platformCollider == null)
            {
                return;
            }

            Bounds source = platformCollider.bounds;
            Vector3 size = source.size;
            Vector3 depth = RopeProjectionUtility.ViewDepth(direction);
            if (Mathf.Abs(depth.x) > 0.5f)
            {
                size.x = Mathf.Max(size.x, projectionDepth);
            }
            else
            {
                size.z = Mathf.Max(size.z, projectionDepth);
            }

            SetWorldAlignedBox(projectionCollider, source.center, size);
            projectionCollider.isTrigger = false;
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
            bool projected = contact.actor.IsProjectedPlatformModeActive &&
                             IsDirectionActive(
                                 contact.actor.ProjectedPlatformDirection) &&
                             projectionCollider != null &&
                             projectionCollider.enabled &&
                             activeDirection ==
                                 contact.actor.ProjectedPlatformDirection;

            if (projected)
            {
                SetIgnored(
                    contact,
                    platformCollider,
                    ref contact.originalIgnored,
                    true);
                bool ignoreProxy = ShouldIgnoreCollision(
                    true,
                    true,
                    contact.proxyIgnored,
                    projectionCollider.bounds,
                    contact.collider.bounds,
                    velocity,
                    landingTolerance);
                SetIgnored(
                    contact,
                    projectionCollider,
                    ref contact.proxyIgnored,
                    ignoreProxy);
                if (ignoreProxy)
                {
                    contact.depthAligned = false;
                    contact.pendingDepthAlignment = false;
                }
                else
                {
                    TryAlignContactDepth(
                        contact,
                        contact.actor.ProjectedPlatformDirection);
                }
                return;
            }

            contact.depthAligned = false;
            contact.pendingDepthAlignment = false;

            bool keepOriginalIgnored = ShouldIgnoreCollision(
                false,
                false,
                contact.originalIgnored,
                platformCollider.bounds,
                contact.collider.bounds,
                velocity,
                landingTolerance);
            SetIgnored(
                contact,
                platformCollider,
                ref contact.originalIgnored,
                keepOriginalIgnored);
            SetIgnored(
                contact,
                projectionCollider,
                ref contact.proxyIgnored,
                false);
        }

        private bool TryAlignContactDepth(
            Contact contact,
            RopeProjectionDirection direction)
        {
            if (contact == null || contact.collider == null ||
                contact.actor == null ||
                contact.actor is not IProjectedPlatformAlignmentReceiver receiver ||
                platformCollider == null)
            {
                return false;
            }

            if (contact.depthAligned &&
                contact.alignedDirection == direction)
            {
                return true;
            }

            Rigidbody body = contact.actor.ProjectedPlatformBody;
            Bounds platformBounds = platformCollider.bounds;
            Bounds actorBounds = contact.collider.bounds;
            float verticalVelocity = body != null
                ? body.linearVelocity.y
                : 0f;
            bool completingDeferredTurn =
                contact.pendingDepthAlignment &&
                contact.pendingDirection != direction;
            bool canAlign = completingDeferredTurn
                ? verticalVelocity <= 0.05f &&
                  IsFootAtSupportTop(
                      platformBounds,
                      actorBounds,
                      landingTolerance,
                      landingTolerance)
                : CanAlignProjectedLanding(
                    platformBounds,
                    actorBounds,
                    verticalVelocity,
                    landingTolerance,
                    direction);
            if (!canAlign)
            {
                return false;
            }

            Vector3 actorPosition = body != null
                ? body.position
                : contact.collider.transform.position;
            Vector3 alignedPosition = AlignPositionToPlatformDepth(
                actorPosition,
                platformBounds.center,
                direction);
            if (completingDeferredTurn)
            {
                alignedPosition = AlignPositionToPlatformDepth(
                    alignedPosition,
                    platformBounds.center,
                    contact.pendingDirection);
            }

            bool accepted = receiver.TryAlignProjectedPlatformDepth(
                new ProjectedPlatformAlignment(
                    platformCollider,
                    direction,
                    alignedPosition));
            if (accepted)
            {
                contact.depthAligned = true;
                contact.alignedDirection = direction;
                contact.pendingDepthAlignment = false;
            }
            else
            {
                contact.pendingDepthAlignment = true;
                contact.pendingDirection = direction;
            }

            return accepted;
        }

        public static bool CanAlignProjectedLanding(
            Bounds platformBounds,
            Bounds actorBounds,
            float verticalVelocity,
            float tolerance,
            RopeProjectionDirection direction)
        {
            float safeTolerance = Mathf.Max(0.01f, tolerance);
            if (verticalVelocity > 0.05f ||
                !IsFootAtSupportTop(
                    platformBounds,
                    actorBounds,
                    safeTolerance,
                    safeTolerance))
            {
                return false;
            }

            Vector3 depth = RopeProjectionUtility.ViewDepth(direction);
            if (Mathf.Abs(depth.x) > 0.5f)
            {
                return actorBounds.max.z >= platformBounds.min.z - safeTolerance &&
                       actorBounds.min.z <= platformBounds.max.z + safeTolerance;
            }

            return actorBounds.max.x >= platformBounds.min.x - safeTolerance &&
                   actorBounds.min.x <= platformBounds.max.x + safeTolerance;
        }

        public static bool IsFootAtSupportTop(
            Bounds supportBounds,
            Bounds actorBounds,
            float belowTolerance,
            float aboveTolerance)
        {
            float footGap = actorBounds.min.y - supportBounds.max.y;
            return footGap >= -Mathf.Max(0f, belowTolerance) &&
                   footGap <= Mathf.Max(0f, aboveTolerance);
        }

        private void ClearIgnored(Contact contact)
        {
            if (contact == null)
            {
                return;
            }

            SetIgnored(
                contact,
                platformCollider,
                ref contact.originalIgnored,
                false);
            SetIgnored(
                contact,
                projectionCollider,
                ref contact.proxyIgnored,
                false);
        }

        private static void SetIgnored(
            Contact contact,
            Collider target,
            ref bool state,
            bool ignored)
        {
            if (contact?.collider == null || target == null || state == ignored)
            {
                return;
            }

            Physics.IgnoreCollision(target, contact.collider, ignored);
            state = ignored;
        }

        private static void SetWorldAlignedBox(
            BoxCollider collider,
            Vector3 worldCenter,
            Vector3 worldSize)
        {
            if (collider == null)
            {
                return;
            }

            Transform colliderTransform = collider.transform;
            colliderTransform.SetPositionAndRotation(
                worldCenter,
                Quaternion.identity);
            Transform parent = colliderTransform.parent;
            if (parent != null)
            {
                Vector3 scale = parent.lossyScale;
                colliderTransform.localScale = new Vector3(
                    SafeInverse(scale.x),
                    SafeInverse(scale.y),
                    SafeInverse(scale.z));
            }
            else
            {
                colliderTransform.localScale = Vector3.one;
            }

            collider.center = Vector3.zero;
            collider.size = worldSize;
        }

        private static float SafeInverse(float value)
        {
            return Mathf.Approximately(value, 0f) ? 1f : 1f / value;
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
