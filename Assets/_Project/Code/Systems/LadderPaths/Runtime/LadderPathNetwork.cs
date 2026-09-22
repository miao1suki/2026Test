using System.Collections.Generic;
using UnityEngine;

namespace Project.LadderPaths
{
    [DisallowMultipleComponent]
    public sealed class LadderPathNetwork : MonoBehaviour
    {
        [SerializeField, Min(0.001f)]
        private float connectionTolerance = 0.12f;

        [SerializeField]
        private LadderProjectionDirection editorDirection = LadderProjectionDirection.Front;

        [System.NonSerialized]
        private Dictionary<LadderProjectionDirection, LadderPathGraph> cachedGraphs;

        [System.NonSerialized]
        private LadderSegment[] cachedSegments;

        public float ConnectionTolerance
        {
            get => connectionTolerance;
            set
            {
                connectionTolerance = Mathf.Max(0.001f, value);
                InvalidateCache();
            }
        }

        public LadderProjectionDirection EditorDirection
        {
            get => editorDirection;
            set => editorDirection = value;
        }

        public IReadOnlyList<LadderSegment> Segments
        {
            get
            {
                EnsureSegments();
                return cachedSegments;
            }
        }

        public void RefreshSegments()
        {
            cachedSegments = GetComponentsInChildren<LadderSegment>(true);
            InvalidateCache();
        }

        public LadderPathGraph BuildPath(LadderProjectionDirection direction)
        {
            return LadderPathGraphBuilder.Build(
                Segments,
                direction,
                connectionTolerance);
        }

        public LadderPathGraph GetCachedPath(LadderProjectionDirection direction)
        {
            if (cachedGraphs == null)
            {
                cachedGraphs = new Dictionary<LadderProjectionDirection, LadderPathGraph>();
            }

            if (!cachedGraphs.TryGetValue(direction, out LadderPathGraph graph))
            {
                graph = BuildPath(direction);
                cachedGraphs.Add(direction, graph);
            }

            return graph;
        }

        public bool TryGetNext(
            LadderProjectionDirection direction,
            LadderSegment segment,
            LadderEndpoint leavingEndpoint,
            out LadderEndpointReference nextEndpoint)
        {
            return GetCachedPath(direction).TryGetNext(
                segment,
                leavingEndpoint,
                out nextEndpoint);
        }

        public bool IsTopOfPath(
            LadderSegment segment,
            LadderProjectionDirection direction)
        {
            return segment != null &&
                   !TryGetNext(direction, segment, LadderEndpoint.Top, out _);
        }

        public bool TryFindClimbableLadder(
            Vector3 playerPosition,
            LadderProjectionDirection direction,
            float horizontalPadding,
            float verticalPadding,
            LayerMask obstructionMask,
            out LadderClimbContact contact)
        {
            Vector2 playerProjected = LadderProjectionUtility.Project(
                playerPosition,
                direction);
            LadderSegment bestSegment = null;
            Vector3 bestPoint = Vector3.zero;
            float bestSquaredDistance = float.MaxValue;
            IReadOnlyList<LadderSegment> segments = Segments;
            for (int index = 0; index < segments.Count; index++)
            {
                LadderSegment segment = segments[index];
                if (segment == null || !segment.isActiveAndEnabled)
                {
                    continue;
                }

                Vector2 bottom = LadderProjectionUtility.Project(
                    segment.GetWorldEndpoint(LadderEndpoint.Bottom),
                    direction);
                Vector2 top = LadderProjectionUtility.Project(
                    segment.GetWorldEndpoint(LadderEndpoint.Top),
                    direction);
                float minY = Mathf.Min(bottom.y, top.y) - verticalPadding;
                float maxY = Mathf.Max(bottom.y, top.y) + verticalPadding;
                float centerX = (bottom.x + top.x) * 0.5f;
                float maxHorizontal = segment.GetProjectedHalfWidth(direction) +
                                      horizontalPadding;
                if (playerProjected.y < minY ||
                    playerProjected.y > maxY ||
                    Mathf.Abs(playerProjected.x - centerX) > maxHorizontal)
                {
                    continue;
                }

                Vector3 closestPoint = segment.GetClosestPointOnCenterLine(playerPosition);
                Vector2 closestProjected = LadderProjectionUtility.Project(
                    closestPoint,
                    direction);
                float squaredDistance =
                    (playerProjected - closestProjected).sqrMagnitude;
                if (squaredDistance < bestSquaredDistance)
                {
                    bestSegment = segment;
                    bestPoint = closestPoint;
                    bestSquaredDistance = squaredDistance;
                }
            }

            if (bestSegment == null ||
                Physics.Linecast(
                    playerPosition,
                    bestPoint,
                    obstructionMask,
                    QueryTriggerInteraction.Ignore))
            {
                contact = default;
                return false;
            }

            contact = new LadderClimbContact(
                this,
                bestSegment,
                direction,
                bestPoint,
                LadderContactSource.ProjectionFallback);
            return true;
        }

        public bool CanUseTopExitGrace(
            LadderSegment segment,
            Vector3 playerPosition,
            LadderProjectionDirection direction,
            float horizontalPadding,
            float verticalPadding)
        {
            if (!IsTopOfPath(segment, direction))
            {
                return false;
            }

            Vector2 player = LadderProjectionUtility.Project(playerPosition, direction);
            Vector2 top = LadderProjectionUtility.Project(
                segment.GetWorldEndpoint(LadderEndpoint.Top),
                direction);
            return player.y >= top.y - verticalPadding &&
                   Mathf.Abs(player.x - top.x) <=
                   segment.GetProjectedHalfWidth(direction) + horizontalPadding * 2f;
        }

        public void InvalidateCache()
        {
            cachedGraphs?.Clear();
        }

        private void Awake()
        {
            RefreshSegments();
        }

        private void OnTransformChildrenChanged()
        {
            RefreshSegments();
        }

        private void OnValidate()
        {
            connectionTolerance = Mathf.Max(0.001f, connectionTolerance);
            RefreshSegments();
        }

        private void EnsureSegments()
        {
            if (cachedSegments == null)
            {
                cachedSegments = GetComponentsInChildren<LadderSegment>(true);
            }
        }
    }
}
