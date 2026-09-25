using System.Collections.Generic;
using Project.CameraModes;
using Project.RopePaths;
using UnityEngine;

namespace Project.PlatformPaths
{
    [System.Serializable]
    public enum PlatformMoveMode
    {
        [InspectorName("游荡模式")]
        Wander = 0,
        [InspectorName("按压触发模式")]
        PressTrigger = 1
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RopePlatform))]
    public sealed class PlatformMove : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField]
        private RopePlatform ropePlatform;

        [SerializeField]
        private RopePathNetwork network;

        [SerializeField]
        private CameraModeController cameraModeController;

        [SerializeField]
        private PlatformRiderZone riderZone;

        [SerializeField]
        private RopeProjectionDirection projectionDirection =
            RopeProjectionDirection.Front;

        [Header("通用")]
        [SerializeField]
        private PlatformMoveMode moveMode = PlatformMoveMode.PressTrigger;

        [SerializeField, Min(0.01f)]
        private float moveSpeed = 2f;

        [SerializeField]
        private string playerTag = "Player";

        [SerializeField, Min(0.05f)]
        private float passengerCheckHeight = 0.6f;

        [SerializeField, Range(0.1f, 1f)]
        private float passengerCheckWidth = 0.9f;

        [Header("游荡模式")]
        [SerializeField]
        private bool useButtonFeature;

        [SerializeField, Min(0f)]
        private float buttonWaitDuration = 5f;

        private readonly Dictionary<RopeEndpointReference, List<RopeEndpointReference>>
            routeNeighbours =
                new Dictionary<RopeEndpointReference, List<RopeEndpointReference>>();

        private readonly PlatformPassengerCarrier passengerCarrier =
            new PlatformPassengerCarrier();

        private RopeSegment initialSegment;
        private RopeEndpoint initialEndpoint;
        private RopeSegment currentSegment;
        private RopeEndpoint entryEndpoint;
        private RopeEndpoint exitEndpoint;
        private RopePathGraph graph;
        private List<RopeEndpointReference> buttonRoute;
        private int buttonRouteIndex;
        private bool moving;
        private bool movingToButtonTarget;
        private bool waitingAtButtonTarget;
        private float buttonWaitTimer;

        public PlatformMoveMode MoveMode
        {
            get => moveMode;
            set
            {
                if (moveMode == value)
                {
                    return;
                }

                moveMode = value;
                StopMovement();
                if (moveMode == PlatformMoveMode.PressTrigger)
                {
                    ReturnToInitialPosition();
                }
                else
                {
                    StartWandering();
                }
            }
        }

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0.01f, value);
        }

        public string PlayerTag
        {
            get => playerTag;
            set
            {
                playerTag = value;
                passengerCarrier.PassengerTag = value;
                if (riderZone != null)
                {
                    riderZone.Configure(this, value);
                }
            }
        }

        public RopeProjectionDirection ProjectionDirection
        {
            get => projectionDirection;
            set => SetProjectionDirection(value);
        }

        public bool UseButtonFeature
        {
            get => useButtonFeature;
            set => useButtonFeature = value;
        }

        public bool HasPlayerOnPlatform =>
            passengerCarrier.HasPassengers;

        private void Reset()
        {
            ropePlatform = GetComponent<RopePlatform>();
            network = ropePlatform != null ? ropePlatform.Network : null;
        }

        private void Awake()
        {
            ResolveReferences();
            passengerCarrier.PassengerTag = playerTag;
            EnsureRiderZone();
            CaptureInitialBinding();
            BuildGraph();

            if (moveMode == PlatformMoveMode.PressTrigger)
            {
                ReturnToInitialPosition();
                return;
            }

            StartWandering();
        }

        private void Update()
        {
            passengerCarrier.Refresh(
                transform,
                passengerCheckHeight,
                passengerCheckWidth);

            if (moveMode == PlatformMoveMode.PressTrigger)
            {
                TickPressTriggerMode();
                return;
            }

            TickWanderMode();
        }

        private void OnDisable()
        {
            passengerCarrier.ReleaseAll();
        }

        public void SetMoveMode(PlatformMoveMode mode)
        {
            MoveMode = mode;
        }

        public void SetProjectionDirection(
            RopeProjectionDirection direction)
        {
            if (projectionDirection == direction)
            {
                return;
            }

            RopePathGraph nextGraph = network != null
                ? network.GetCachedPath(direction)
                : null;

            projectionDirection = direction;
            graph = nextGraph;

            if (moving &&
                nextGraph != null &&
                currentSegment != null &&
                !nextGraph.TryGetNext(
                    currentSegment,
                    exitEndpoint,
                    out _))
            {
                StopMovement();
            }
        }

        public void ReceiveButtonSignal(Transform button)
        {
            if (button == null)
            {
                return;
            }

            ReceiveButtonSignal(button.position);
        }

        public void ReceiveButtonSignal(Vector3 buttonWorldPosition)
        {
            if (!useButtonFeature ||
                moveMode != PlatformMoveMode.Wander ||
                network == null)
            {
                return;
            }

            if (!TryBuildRouteToNearestEndpoint(
                    buttonWorldPosition,
                    out List<RopeEndpointReference> route))
            {
                return;
            }

            buttonRoute = route;
            buttonRouteIndex = 0;
            moving = false;
            movingToButtonTarget = true;
            waitingAtButtonTarget = false;
            buttonWaitTimer = 0f;
        }

        public void CancelButtonSignal()
        {
            if (moveMode != PlatformMoveMode.Wander)
            {
                return;
            }

            buttonRoute = null;
            buttonRouteIndex = 0;
            movingToButtonTarget = false;
            waitingAtButtonTarget = false;
            StartWandering();
        }

        public void CapturePassenger(Collider collider)
        {
            passengerCarrier.Capture(transform, collider);
        }

        public void ReleasePassenger(Transform passenger)
        {
            passengerCarrier.Release(passenger);
        }

        public void ReleaseAllPassengers()
        {
            passengerCarrier.ReleaseAll();
        }

        private void ResolveReferences()
        {
            if (ropePlatform == null)
            {
                ropePlatform = GetComponent<RopePlatform>();
            }

            if (network == null && ropePlatform != null)
            {
                network = ropePlatform.Network;
            }

            if (cameraModeController == null)
            {
                cameraModeController =
                    FindFirstObjectByType<CameraModeController>();
            }
        }

        private void EnsureRiderZone()
        {
            if (riderZone == null)
            {
                riderZone = GetComponentInChildren<PlatformRiderZone>(
                    true);
            }

            if (riderZone == null)
            {
                Collider platformCollider = GetComponent<Collider>();
                if (platformCollider == null)
                {
                    return;
                }

                GameObject zoneObject =
                    new GameObject("__RiderZone");
                zoneObject.transform.SetParent(transform, false);
                riderZone =
                    zoneObject.AddComponent<PlatformRiderZone>();
            }

            riderZone.Configure(this, playerTag);
            ConfigureRiderZoneCollider();
        }

        private void ConfigureRiderZoneCollider()
        {
            Collider platformCollider = GetComponent<Collider>();
            BoxCollider zoneCollider =
                riderZone.GetComponent<BoxCollider>();
            if (platformCollider == null ||
                zoneCollider == null)
            {
                return;
            }

            Bounds bounds = platformCollider.bounds;
            float zoneHeight = Mathf.Min(
                passengerCheckHeight,
                0.3f);
            Vector3 scale = transform.lossyScale;
            Transform zoneTransform = riderZone.transform;
            zoneTransform.localScale = new Vector3(
                Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x,
                Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y,
                Mathf.Approximately(scale.z, 0f) ? 1f : 1f / scale.z);
            zoneTransform.position = new Vector3(
                bounds.center.x,
                bounds.max.y + zoneHeight * 0.5f,
                bounds.center.z);
            zoneCollider.isTrigger = true;
            zoneCollider.center = Vector3.zero;
            zoneCollider.size = new Vector3(
                bounds.size.x * passengerCheckWidth,
                zoneHeight,
                bounds.size.z * passengerCheckWidth);
        }

        private void CaptureInitialBinding()
        {
            if (ropePlatform != null && ropePlatform.IsBound)
            {
                initialSegment = ropePlatform.BoundSegment;
                initialEndpoint = ropePlatform.BoundEndpoint;
            }

            if (initialSegment == null &&
                network != null &&
                network.Segments.Count > 0)
            {
                initialSegment = network.Segments[0];
                initialEndpoint = RopeEndpoint.A;
                ropePlatform?.Bind(
                    network,
                    initialSegment,
                    initialEndpoint);
            }

            currentSegment = initialSegment;
            entryEndpoint = initialEndpoint;
            exitEndpoint = RopeSegment.GetOpposite(entryEndpoint);
        }

        private void BuildGraph()
        {
            if (network == null)
            {
                graph = null;
                return;
            }

            projectionDirection =
                network.CurrentProjectionDirection;
            graph = network.GetCachedPath(projectionDirection);
        }

        private void TickPressTriggerMode()
        {
            if (!HasPlayerOnPlatform)
            {
                if (moving || !IsAtInitialBinding())
                {
                    ReturnToInitialPosition();
                }

                return;
            }

            if (!moving)
            {
                moving = true;
            }

            TickSegmentMovement();
        }

        private void TickWanderMode()
        {
            if (waitingAtButtonTarget)
            {
                TickButtonWait();
                return;
            }

            if (movingToButtonTarget)
            {
                TickButtonRoute();
                return;
            }

            if (!moving)
            {
                moving = currentSegment != null && graph != null;
            }

            TickSegmentMovement();
        }

        private void TickButtonWait()
        {
            if (HasPlayerOnPlatform)
            {
                waitingAtButtonTarget = false;
                StartWandering();
                return;
            }

            buttonWaitTimer -= Time.deltaTime;
            if (buttonWaitTimer <= 0f)
            {
                waitingAtButtonTarget = false;
                StartWandering();
            }
        }

        private void TickButtonRoute()
        {
            if (buttonRoute == null ||
                buttonRouteIndex >= buttonRoute.Count)
            {
                waitingAtButtonTarget = true;
                movingToButtonTarget = false;
                moving = false;
                buttonWaitTimer = Mathf.Max(0f, buttonWaitDuration);
                return;
            }

            if (buttonRouteIndex == 0)
            {
                RopeEndpointReference first = buttonRoute[0];
                if (!MoveTowardEndpoint(first))
                {
                    return;
                }

                SetCurrentEndpoint(first);
                buttonRouteIndex++;
                return;
            }

            RopeEndpointReference previous =
                buttonRoute[buttonRouteIndex - 1];
            RopeEndpointReference next =
                buttonRoute[buttonRouteIndex];

            if (previous.Segment == next.Segment)
            {
                if (!MoveTowardEndpoint(next))
                {
                    return;
                }
            }
            else
            {
                SyncProjectionDirection();
                if (!CanUseProjectionTeleport())
                {
                    buttonRoute = null;
                    buttonRouteIndex = 0;
                    movingToButtonTarget = false;
                    waitingAtButtonTarget = false;
                    ReverseCurrentSegment();
                    return;
                }

                if (!HasExactConnection(
                        graph,
                        previous,
                        next))
                {
                    StopMovement();
                    return;
                }

                TeleportToEndpoint(next.Segment, next.Endpoint);
            }

            SetCurrentEndpoint(next);
            buttonRouteIndex++;
        }

        private void TickSegmentMovement()
        {
            if (currentSegment == null || graph == null)
            {
                StopMovement();
                return;
            }

            if (!MoveTowardEndpoint(
                    new RopeEndpointReference(
                        currentSegment,
                        exitEndpoint)))
            {
                return;
            }

            RopeSegment completedSegment = currentSegment;
            RopeEndpoint completedExit = exitEndpoint;
            SyncProjectionDirection();
            ropePlatform?.Bind(
                network,
                completedSegment,
                completedExit);

            if (!CanUseProjectionTeleport())
            {
                ReverseCurrentSegment();
                return;
            }

            if (graph.TryGetNext(
                    completedSegment,
                    completedExit,
                    out RopeEndpointReference next))
            {
                if (!HasExactConnection(
                        graph,
                        new RopeEndpointReference(
                            completedSegment,
                            completedExit),
                        next))
                {
                    StopMovement();
                    return;
                }

                currentSegment = next.Segment;
                entryEndpoint = next.Endpoint;
                exitEndpoint =
                    RopeSegment.GetOpposite(entryEndpoint);
                TeleportToEndpoint(
                    currentSegment,
                    entryEndpoint);
                moving = true;
                return;
            }

            ReverseCurrentSegment();
        }

        private void ReverseCurrentSegment()
        {
            entryEndpoint = exitEndpoint;
            exitEndpoint =
                RopeSegment.GetOpposite(entryEndpoint);
            moving = true;
        }

        private bool CanUseProjectionTeleport()
        {
            ResolveReferences();
            return cameraModeController == null ||
                   (cameraModeController.CurrentMode ==
                    CameraViewMode.Side2D &&
                    cameraModeController.TargetMode ==
                    CameraViewMode.Side2D);
        }

        private void SyncProjectionDirection()
        {
            if (network == null)
            {
                return;
            }

            RopeProjectionDirection currentDirection =
                network.CurrentProjectionDirection;
            if (projectionDirection == currentDirection)
            {
                return;
            }

            projectionDirection = currentDirection;
            graph = network.GetCachedPath(currentDirection);
        }

        private static bool HasExactConnection(
            RopePathGraph currentGraph,
            RopeEndpointReference first,
            RopeEndpointReference second)
        {
            if (currentGraph == null)
            {
                return false;
            }

            for (int index = 0;
                 index < currentGraph.Connections.Count;
                 index++)
            {
                RopePathConnection connection =
                    currentGraph.Connections[index];
                if ((connection.First.Equals(first) &&
                     connection.Second.Equals(second)) ||
                    (connection.First.Equals(second) &&
                     connection.Second.Equals(first)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool MoveTowardEndpoint(
            RopeEndpointReference endpoint)
        {
            if (endpoint.Segment == null)
            {
                return false;
            }

            Vector3 target = endpoint.Segment.GetWorldEndpoint(
                endpoint.Endpoint);
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime);
            passengerCarrier.Carry(transform);

            return (transform.position - target).sqrMagnitude <=
                   0.000001f;
        }

        private void SetCurrentEndpoint(
            RopeEndpointReference endpoint)
        {
            currentSegment = endpoint.Segment;
            entryEndpoint = endpoint.Endpoint;
            exitEndpoint =
                RopeSegment.GetOpposite(entryEndpoint);
            moving = true;
            ropePlatform?.Bind(
                network,
                currentSegment,
                entryEndpoint);
        }

        private void TeleportToEndpoint(
            RopeSegment segment,
            RopeEndpoint endpoint)
        {
            if (segment == null)
            {
                return;
            }

            transform.position =
                segment.GetWorldEndpoint(endpoint);
            passengerCarrier.Carry(transform);
        }

        private void ReturnToInitialPosition()
        {
            if (initialSegment == null)
            {
                StopMovement();
                return;
            }

            currentSegment = initialSegment;
            entryEndpoint = initialEndpoint;
            exitEndpoint =
                RopeSegment.GetOpposite(entryEndpoint);
            StopMovement();
            TeleportToEndpoint(
                initialSegment,
                initialEndpoint);
            ropePlatform?.Bind(
                network,
                initialSegment,
                initialEndpoint);
        }

        private bool IsAtInitialBinding()
        {
            if (initialSegment == null)
            {
                return true;
            }

            Vector3 initialPosition =
                initialSegment.GetWorldEndpoint(
                    initialEndpoint);
            return (transform.position - initialPosition).sqrMagnitude <=
                   0.000001f;
        }

        private void StopMovement()
        {
            moving = false;
            movingToButtonTarget = false;
            waitingAtButtonTarget = false;
            buttonRoute = null;
            buttonRouteIndex = 0;
            buttonWaitTimer = 0f;
        }

        private void StartWandering()
        {
            StopMovement();
            if (currentSegment == null || graph == null)
            {
                return;
            }

            moving = true;
        }

        private bool TryBuildRouteToNearestEndpoint(
            Vector3 buttonWorldPosition,
            out List<RopeEndpointReference> route)
        {
            route = null;
            if (graph == null ||
                currentSegment == null ||
                graph.ProjectedEndpoints.Count == 0)
            {
                return false;
            }

            RopeEndpointReference target = default;
            float bestDistance = float.MaxValue;
            for (int index = 0;
                 index < graph.ProjectedEndpoints.Count;
                 index++)
            {
                RopeEndpointReference endpoint =
                    graph.ProjectedEndpoints[index].Reference;
                float distance = Vector3.Distance(
                    buttonWorldPosition,
                    endpoint.Segment.GetWorldEndpoint(
                        endpoint.Endpoint));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    target = endpoint;
                }
            }

            RopeEndpointReference startA =
                new RopeEndpointReference(
                    currentSegment,
                    RopeEndpoint.A);
            RopeEndpointReference startB =
                new RopeEndpointReference(
                    currentSegment,
                    RopeEndpoint.B);
            RopeEndpointReference start =
                Vector3.Distance(
                    transform.position,
                    currentSegment.GetWorldEndpoint(
                        RopeEndpoint.A)) <=
                Vector3.Distance(
                    transform.position,
                    currentSegment.GetWorldEndpoint(
                        RopeEndpoint.B))
                    ? startA
                    : startB;

            return TryFindRoute(
                start,
                target,
                out route);
        }

        private bool TryFindRoute(
            RopeEndpointReference start,
            RopeEndpointReference target,
            out List<RopeEndpointReference> route)
        {
            route = null;
            routeNeighbours.Clear();

            Dictionary<RopeEndpointReference, RopeEndpointReference>
                previous =
                    new Dictionary<RopeEndpointReference, RopeEndpointReference>();
            Queue<RopeEndpointReference> queue =
                new Queue<RopeEndpointReference>();
            previous[start] = start;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                RopeEndpointReference current = queue.Dequeue();
                if (current.Equals(target))
                {
                    return TryReconstructRoute(
                        previous,
                        start,
                        target,
                        out route);
                }

                List<RopeEndpointReference> neighbours =
                    GetNeighbours(current);
                for (int index = 0;
                     index < neighbours.Count;
                     index++)
                {
                    RopeEndpointReference neighbour =
                        neighbours[index];
                    if (previous.ContainsKey(neighbour))
                    {
                        continue;
                    }

                    previous[neighbour] = current;
                    queue.Enqueue(neighbour);
                }
            }

            return false;
        }

        private List<RopeEndpointReference> GetNeighbours(
            RopeEndpointReference endpoint)
        {
            if (routeNeighbours.TryGetValue(
                    endpoint,
                    out List<RopeEndpointReference> cached))
            {
                return cached;
            }

            List<RopeEndpointReference> neighbours =
                new List<RopeEndpointReference>();
            if (endpoint.Segment != null)
            {
                neighbours.Add(
                    new RopeEndpointReference(
                        endpoint.Segment,
                        RopeSegment.GetOpposite(
                            endpoint.Endpoint)));
            }

            for (int index = 0;
                 index < graph.Connections.Count;
                 index++)
            {
                RopePathConnection connection =
                    graph.Connections[index];
                if (connection.First.Equals(endpoint))
                {
                    neighbours.Add(connection.Second);
                }
                else if (connection.Second.Equals(endpoint))
                {
                    neighbours.Add(connection.First);
                }
            }

            routeNeighbours[endpoint] = neighbours;
            return neighbours;
        }

        private static bool TryReconstructRoute(
            Dictionary<RopeEndpointReference, RopeEndpointReference>
                previous,
            RopeEndpointReference start,
            RopeEndpointReference target,
            out List<RopeEndpointReference> route)
        {
            route = new List<RopeEndpointReference>();
            RopeEndpointReference current = target;
            route.Add(current);
            while (!current.Equals(start))
            {
                if (!previous.TryGetValue(
                        current,
                        out current))
                {
                    route = null;
                    return false;
                }

                route.Add(current);
            }

            route.Reverse();
            return true;
        }
    }
}
