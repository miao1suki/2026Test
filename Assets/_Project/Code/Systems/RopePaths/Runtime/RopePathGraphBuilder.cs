using System.Collections.Generic;
using UnityEngine;

namespace Project.RopePaths
{
    public static class RopePathGraphBuilder
    {
        public static RopePathGraph Build(
            IReadOnlyList<RopeSegment> segments,
            RopeProjectionDirection direction,
            float connectionTolerance)
        {
            float tolerance = Mathf.Max(0.0001f, connectionTolerance);
            List<RopeProjectedEndpoint> projectedEndpoints =
                new List<RopeProjectedEndpoint>();
            List<RopePathConnection> connections = new List<RopePathConnection>();
            List<RopePath> paths = new List<RopePath>();
            if (segments == null || segments.Count == 0)
            {
                return new RopePathGraph(
                    direction,
                    tolerance,
                    projectedEndpoints,
                    connections,
                    paths);
            }

            int endpointCount = segments.Count * 2;
            Vector2[] projected = new Vector2[endpointCount];
            List<List<int>> clusters = new List<List<int>>();
            int[] clusterOf = new int[endpointCount];
            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                RopeSegment segment = segments[segmentIndex];
                for (int endpointIndex = 0; endpointIndex < 2; endpointIndex++)
                {
                    RopeEndpoint endpoint = (RopeEndpoint)endpointIndex;
                    int index = segmentIndex * 2 + endpointIndex;
                    projected[index] = RopeProjectionUtility.Project(
                        segment.GetWorldEndpoint(endpoint),
                        direction);
                    Vector2 otherProjected = RopeProjectionUtility.Project(
                        segment.GetWorldEndpoint(RopeSegment.GetOpposite(endpoint)),
                        direction);
                    Vector2 projectedDirection = otherProjected - projected[index];
                    if (projectedDirection.sqrMagnitude > 0.000001f)
                    {
                        projectedDirection.Normalize();
                    }

                    projectedEndpoints.Add(new RopeProjectedEndpoint(
                        new RopeEndpointReference(segment, endpoint),
                        projected[index],
                        projectedDirection,
                        RopeProjectionUtility.Depth(
                            segment.GetWorldEndpoint(endpoint),
                            direction)));
                    int clusterIndex = FindCluster(projected[index], clusters, projected, tolerance);
                    if (clusterIndex < 0)
                    {
                        clusterIndex = clusters.Count;
                        clusters.Add(new List<int>());
                    }

                    clusters[clusterIndex].Add(index);
                    clusterOf[index] = clusterIndex;
                }
            }

            List<RopeEndpointReference>[] neighbours =
                new List<RopeEndpointReference>[endpointCount];
            for (int index = 0; index < endpointCount; index++)
            {
                neighbours[index] = new List<RopeEndpointReference>();
            }

            for (int clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
            {
                List<int> cluster = clusters[clusterIndex];
                Vector2 center = GetClusterCenter(cluster, projected);
                for (int firstIndex = 0; firstIndex < cluster.Count; firstIndex++)
                {
                    int first = cluster[firstIndex];
                    for (int secondIndex = firstIndex + 1;
                         secondIndex < cluster.Count;
                         secondIndex++)
                    {
                        int second = cluster[secondIndex];
                        int firstSegment = first / 2;
                        int secondSegment = second / 2;
                        if (firstSegment == secondSegment)
                        {
                            continue;
                        }

                        RopeEndpointReference firstReference = CreateReference(
                            segments,
                            first);
                        RopeEndpointReference secondReference = CreateReference(
                            segments,
                            second);
                        connections.Add(new RopePathConnection(
                            firstReference,
                            secondReference,
                            center));
                        neighbours[first].Add(secondReference);
                        neighbours[second].Add(firstReference);
                    }
                }
            }

            BuildPaths(segments, neighbours, paths);
            return new RopePathGraph(
                direction,
                tolerance,
                projectedEndpoints,
                connections,
                paths);
        }

        private static int FindCluster(
            Vector2 position,
            List<List<int>> clusters,
            Vector2[] projected,
            float tolerance)
        {
            float squaredTolerance = tolerance * tolerance;
            for (int clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
            {
                Vector2 center = GetClusterCenter(clusters[clusterIndex], projected);
                if ((center - position).sqrMagnitude <= squaredTolerance)
                {
                    return clusterIndex;
                }
            }

            return -1;
        }

        private static Vector2 GetClusterCenter(List<int> cluster, Vector2[] projected)
        {
            Vector2 center = Vector2.zero;
            for (int index = 0; index < cluster.Count; index++)
            {
                center += projected[cluster[index]];
            }

            return center / Mathf.Max(1, cluster.Count);
        }

        private static RopeEndpointReference CreateReference(
            IReadOnlyList<RopeSegment> segments,
            int endpointIndex)
        {
            return new RopeEndpointReference(
                segments[endpointIndex / 2],
                (RopeEndpoint)(endpointIndex % 2));
        }

        private static void BuildPaths(
            IReadOnlyList<RopeSegment> segments,
            IReadOnlyList<RopeEndpointReference>[] neighbours,
            List<RopePath> paths)
        {
            HashSet<int> consumed = new HashSet<int>();
            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                if (neighbours[segmentIndex * 2].Count != 0 &&
                    neighbours[segmentIndex * 2 + 1].Count != 0)
                {
                    continue;
                }

                if (consumed.Contains(segmentIndex))
                {
                    continue;
                }

                RopePath path = WalkPath(
                    segments,
                    neighbours,
                    segmentIndex,
                    neighbours[segmentIndex * 2].Count == 0
                        ? RopeEndpoint.A
                        : RopeEndpoint.B,
                    consumed);
                if (path.Traversals.Count > 0)
                {
                    paths.Add(path);
                }
            }

            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                if (consumed.Contains(segmentIndex))
                {
                    continue;
                }

                RopePath path = WalkPath(
                    segments,
                    neighbours,
                    segmentIndex,
                    RopeEndpoint.A,
                    consumed);
                path.IsClosed = true;
                if (path.Traversals.Count > 0)
                {
                    paths.Add(path);
                }
            }
        }

        private static RopePath WalkPath(
            IReadOnlyList<RopeSegment> segments,
            IReadOnlyList<RopeEndpointReference>[] neighbours,
            int segmentIndex,
            RopeEndpoint entryEndpoint,
            HashSet<int> consumed)
        {
            List<RopePathTraversal> traversals = new List<RopePathTraversal>();
            HashSet<int> local = new HashSet<int>();
            RopeEndpointReference previous = default;
            bool hasPrevious = false;
            while (segmentIndex >= 0 && segmentIndex < segments.Count &&
                   !local.Contains(segmentIndex))
            {
                local.Add(segmentIndex);
                consumed.Add(segmentIndex);
                bool travelsAtoB = entryEndpoint == RopeEndpoint.A;
                traversals.Add(new RopePathTraversal(segments[segmentIndex], travelsAtoB));
                RopeEndpoint exitEndpoint = RopeSegment.GetOpposite(entryEndpoint);
                IReadOnlyList<RopeEndpointReference> options =
                    neighbours[segmentIndex * 2 + (int)exitEndpoint];
                RopeEndpointReference next = default;
                bool found = false;
                for (int index = 0; index < options.Count; index++)
                {
                    if (!hasPrevious || !options[index].Segment.Equals(previous.Segment))
                    {
                        next = options[index];
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    break;
                }

                previous = new RopeEndpointReference(
                    segments[segmentIndex],
                    exitEndpoint);
                hasPrevious = true;
                segmentIndex = FindSegmentIndex(segments, next.Segment);
                entryEndpoint = next.Endpoint;
            }

            return new RopePath(traversals);
        }

        private static int FindSegmentIndex(
            IReadOnlyList<RopeSegment> segments,
            RopeSegment target)
        {
            for (int index = 0; index < segments.Count; index++)
            {
                if (segments[index] == target)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}

