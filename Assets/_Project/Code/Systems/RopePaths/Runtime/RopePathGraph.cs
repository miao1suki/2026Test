using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.RopePaths
{
    public readonly struct RopeProjectedEndpoint
    {
        public RopeProjectedEndpoint(
            RopeEndpointReference reference,
            Vector2 projectedPosition,
            Vector2 projectedDirection,
            float depth)
        {
            Reference = reference;
            ProjectedPosition = projectedPosition;
            ProjectedDirection = projectedDirection;
            Depth = depth;
        }

        public RopeEndpointReference Reference { get; }
        public Vector2 ProjectedPosition { get; }
        public Vector2 ProjectedDirection { get; }
        public float Depth { get; }
    }

    public readonly struct RopeEndpointReference : IEquatable<RopeEndpointReference>
    {
        public RopeEndpointReference(RopeSegment segment, RopeEndpoint endpoint)
        {
            Segment = segment;
            Endpoint = endpoint;
        }

        public RopeSegment Segment { get; }
        public RopeEndpoint Endpoint { get; }

        public bool Equals(RopeEndpointReference other)
        {
            return Segment == other.Segment && Endpoint == other.Endpoint;
        }

        public override bool Equals(object obj)
        {
            return obj is RopeEndpointReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Segment != null ? Segment.GetInstanceID() : 0) * 397) +
                       (int)Endpoint;
            }
        }
    }

    public readonly struct RopePathConnection
    {
        public RopePathConnection(
            RopeEndpointReference first,
            RopeEndpointReference second,
            Vector2 projectedPosition)
        {
            First = first;
            Second = second;
            ProjectedPosition = projectedPosition;
        }

        public RopeEndpointReference First { get; }
        public RopeEndpointReference Second { get; }
        public Vector2 ProjectedPosition { get; }
    }

    public readonly struct RopePathTraversal
    {
        public RopePathTraversal(RopeSegment segment, bool travelsAtoB)
        {
            Segment = segment;
            TravelsAtoB = travelsAtoB;
        }

        public RopeSegment Segment { get; }
        public bool TravelsAtoB { get; }
        public RopeEndpoint EntryEndpoint =>
            TravelsAtoB ? RopeEndpoint.A : RopeEndpoint.B;
        public RopeEndpoint ExitEndpoint =>
            TravelsAtoB ? RopeEndpoint.B : RopeEndpoint.A;
    }

    public sealed class RopePath
    {
        private readonly List<RopePathTraversal> traversals;

        internal RopePath(List<RopePathTraversal> traversals)
        {
            this.traversals = traversals;
        }

        public IReadOnlyList<RopePathTraversal> Traversals => traversals;
        public bool IsClosed { get; internal set; }
    }

    public sealed class RopePathGraph
    {
        private readonly List<RopePathConnection> connections;
        private readonly List<RopePath> paths;

        internal RopePathGraph(
            RopeProjectionDirection direction,
            float tolerance,
            List<RopeProjectedEndpoint> projectedEndpoints,
            List<RopePathConnection> connections,
            List<RopePath> paths)
        {
            Direction = direction;
            Tolerance = tolerance;
            this.projectedEndpoints = projectedEndpoints;
            this.connections = connections;
            this.paths = paths;
        }

        private readonly List<RopeProjectedEndpoint> projectedEndpoints;
        public RopeProjectionDirection Direction { get; }
        public float Tolerance { get; }
        public IReadOnlyList<RopeProjectedEndpoint> ProjectedEndpoints => projectedEndpoints;
        public IReadOnlyList<RopePathConnection> Connections => connections;
        public IReadOnlyList<RopePath> Paths => paths;

        public bool TryGetNext(
            RopeSegment currentSegment,
            RopeEndpoint leavingEndpoint,
            out RopeEndpointReference nextEndpoint)
        {
            for (int index = 0; index < connections.Count; index++)
            {
                RopePathConnection connection = connections[index];
                if (connection.First.Segment == currentSegment &&
                    connection.First.Endpoint == leavingEndpoint)
                {
                    nextEndpoint = connection.Second;
                    return true;
                }

                if (connection.Second.Segment == currentSegment &&
                    connection.Second.Endpoint == leavingEndpoint)
                {
                    nextEndpoint = connection.First;
                    return true;
                }
            }

            nextEndpoint = default;
            return false;
        }
    }
}

