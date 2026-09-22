using System.Collections.Generic;
using UnityEngine;

namespace Project.LadderPaths
{
    public static class LadderPathGraphBuilder
    {
        public static LadderPathGraph Build(
            IReadOnlyList<LadderSegment> segments,
            LadderProjectionDirection direction,
            float connectionTolerance)
        {
            float tolerance = Mathf.Max(0.0001f, connectionTolerance);
            List<LadderProjectedEndpoint> endpoints =
                new List<LadderProjectedEndpoint>();
            List<LadderPathConnection> connections =
                new List<LadderPathConnection>();
            if (segments == null)
            {
                return new LadderPathGraph(direction, tolerance, endpoints, connections);
            }

            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                LadderSegment segment = segments[segmentIndex];
                if (segment == null)
                {
                    continue;
                }

                LadderFaceFamily family = segment.GetVisibleFaceFamily(direction);
                for (int endpointIndex = 0; endpointIndex < 2; endpointIndex++)
                {
                    LadderEndpoint endpoint = (LadderEndpoint)endpointIndex;
                    Vector3 world = segment.GetWorldEndpoint(endpoint);
                    endpoints.Add(new LadderProjectedEndpoint(
                        new LadderEndpointReference(segment, endpoint),
                        LadderProjectionUtility.Project(world, direction),
                        LadderProjectionUtility.Depth(world, direction),
                        family));
                }
            }

            float squaredTolerance = tolerance * tolerance;
            for (int firstIndex = 0; firstIndex < endpoints.Count; firstIndex++)
            {
                LadderProjectedEndpoint first = endpoints[firstIndex];
                for (int secondIndex = firstIndex + 1;
                     secondIndex < endpoints.Count;
                     secondIndex++)
                {
                    LadderProjectedEndpoint second = endpoints[secondIndex];
                    if (first.Reference.Segment == second.Reference.Segment ||
                        first.Reference.Endpoint == second.Reference.Endpoint ||
                        first.FaceFamily != second.FaceFamily ||
                        (first.ProjectedPosition - second.ProjectedPosition).sqrMagnitude >
                        squaredTolerance)
                    {
                        continue;
                    }

                    connections.Add(new LadderPathConnection(
                        first.Reference,
                        second.Reference,
                        (first.ProjectedPosition + second.ProjectedPosition) * 0.5f,
                        first.FaceFamily));
                }
            }

            return new LadderPathGraph(direction, tolerance, endpoints, connections);
        }
    }
}
