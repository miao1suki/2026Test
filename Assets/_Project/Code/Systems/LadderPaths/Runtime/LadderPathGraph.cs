using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.LadderPaths
{
    public readonly struct LadderEndpointReference : IEquatable<LadderEndpointReference>
    {
        public LadderEndpointReference(LadderSegment segment, LadderEndpoint endpoint)
        {
            Segment = segment;
            Endpoint = endpoint;
        }

        public LadderSegment Segment { get; }
        public LadderEndpoint Endpoint { get; }

        public bool Equals(LadderEndpointReference other)
        {
            return Segment == other.Segment && Endpoint == other.Endpoint;
        }

        public override bool Equals(object obj)
        {
            return obj is LadderEndpointReference other && Equals(other);
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

    public readonly struct LadderProjectedEndpoint
    {
        public LadderProjectedEndpoint(
            LadderEndpointReference reference,
            Vector2 projectedPosition,
            float depth,
            LadderFaceFamily faceFamily)
        {
            Reference = reference;
            ProjectedPosition = projectedPosition;
            Depth = depth;
            FaceFamily = faceFamily;
        }

        public LadderEndpointReference Reference { get; }
        public Vector2 ProjectedPosition { get; }
        public float Depth { get; }
        public LadderFaceFamily FaceFamily { get; }
    }

    public readonly struct LadderPathConnection
    {
        public LadderPathConnection(
            LadderEndpointReference first,
            LadderEndpointReference second,
            Vector2 projectedPosition,
            LadderFaceFamily faceFamily)
        {
            First = first;
            Second = second;
            ProjectedPosition = projectedPosition;
            FaceFamily = faceFamily;
        }

        public LadderEndpointReference First { get; }
        public LadderEndpointReference Second { get; }
        public Vector2 ProjectedPosition { get; }
        public LadderFaceFamily FaceFamily { get; }
    }

    public sealed class LadderPathGraph
    {
        private readonly List<LadderProjectedEndpoint> projectedEndpoints;
        private readonly List<LadderPathConnection> connections;

        internal LadderPathGraph(
            LadderProjectionDirection direction,
            float tolerance,
            List<LadderProjectedEndpoint> projectedEndpoints,
            List<LadderPathConnection> connections)
        {
            Direction = direction;
            Tolerance = tolerance;
            this.projectedEndpoints = projectedEndpoints;
            this.connections = connections;
        }

        public LadderProjectionDirection Direction { get; }
        public float Tolerance { get; }
        public IReadOnlyList<LadderProjectedEndpoint> ProjectedEndpoints => projectedEndpoints;
        public IReadOnlyList<LadderPathConnection> Connections => connections;

        public bool TryGetNext(
            LadderSegment currentSegment,
            LadderEndpoint leavingEndpoint,
            out LadderEndpointReference nextEndpoint)
        {
            for (int index = 0; index < connections.Count; index++)
            {
                LadderPathConnection connection = connections[index];
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
