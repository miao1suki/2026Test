using System.Collections.Generic;
using UnityEngine;

namespace Project.RopePaths
{
    [DisallowMultipleComponent]
    public sealed class RopePathNetwork : MonoBehaviour
    {
        [SerializeField, Min(0.001f)]
        private float connectionTolerance = 0.12f;

        [SerializeField]
        private RopeProjectionDirection editorDirection = RopeProjectionDirection.Front;

        [System.NonSerialized]
        private Dictionary<RopeProjectionDirection, RopePathGraph> cachedGraphs;

        public float ConnectionTolerance
        {
            get => connectionTolerance;
            set
            {
                connectionTolerance = Mathf.Max(0.001f, value);
                InvalidateCache();
            }
        }

        public RopeProjectionDirection EditorDirection
        {
            get => editorDirection;
            set => editorDirection = value;
        }

        public IReadOnlyList<RopeSegment> Segments =>
            GetComponentsInChildren<RopeSegment>(true);

        public RopePathGraph BuildPath(RopeProjectionDirection direction)
        {
            return RopePathGraphBuilder.Build(
                Segments,
                direction,
                connectionTolerance);
        }

        public RopePathGraph GetCachedPath(RopeProjectionDirection direction)
        {
            if (cachedGraphs == null)
            {
                cachedGraphs = new Dictionary<RopeProjectionDirection, RopePathGraph>();
            }

            if (!cachedGraphs.TryGetValue(direction, out RopePathGraph graph))
            {
                graph = BuildPath(direction);
                cachedGraphs.Add(direction, graph);
            }

            return graph;
        }

        public void RebuildPaths()
        {
            cachedGraphs = new Dictionary<RopeProjectionDirection, RopePathGraph>();
            for (int index = 0; index < 4; index++)
            {
                RopeProjectionDirection direction = (RopeProjectionDirection)index;
                cachedGraphs[direction] = BuildPath(direction);
            }
        }

        public bool TryGetNext(
            RopeProjectionDirection direction,
            RopeSegment currentSegment,
            RopeEndpoint leavingEndpoint,
            out RopeEndpointReference nextEndpoint)
        {
            return GetCachedPath(direction).TryGetNext(
                currentSegment,
                leavingEndpoint,
                out nextEndpoint);
        }

        public void InvalidateCache()
        {
            cachedGraphs?.Clear();
        }

        private void OnValidate()
        {
            connectionTolerance = Mathf.Max(0.001f, connectionTolerance);
            InvalidateCache();
        }
    }
}


