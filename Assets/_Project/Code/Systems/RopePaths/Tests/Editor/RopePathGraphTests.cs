using NUnit.Framework;
using UnityEngine;

namespace Project.RopePaths.Tests
{
    public sealed class RopePathGraphTests
    {
        [Test]
        public void FrontProjection_ConnectsEndpointsAcrossDifferentDepths()
        {
            GameObject firstObject = CreateSegment(
                new Vector3(-2f, 0f, 0f),
                new Vector3(0f, 0f, 0f));
            GameObject secondObject = CreateSegment(
                new Vector3(0f, 0f, 5f),
                new Vector3(0f, 2f, 5f));
            GameObject thirdObject = CreateSegment(
                new Vector3(0f, 2f, -3f),
                new Vector3(2f, 2f, -3f));

            RopeSegment[] segments =
            {
                firstObject.GetComponent<RopeSegment>(),
                secondObject.GetComponent<RopeSegment>(),
                thirdObject.GetComponent<RopeSegment>()
            };
            RopePathGraph graph = RopePathGraphBuilder.Build(
                segments,
                RopeProjectionDirection.Front,
                0.01f);

            Assert.That(graph.Connections.Count, Is.EqualTo(2));
            Assert.That(graph.ProjectedEndpoints.Count, Is.EqualTo(6));
            Assert.That(
                graph.ProjectedEndpoints[0].ProjectedDirection,
                Is.EqualTo(Vector2.right));
            Assert.That(graph.Paths.Count, Is.EqualTo(1));
            Assert.That(graph.Paths[0].Traversals.Count, Is.EqualTo(3));
            Assert.That(
                graph.TryGetNext(
                    segments[0],
                    RopeEndpoint.B,
                    out RopeEndpointReference next),
                Is.True);
            Assert.That(next.Segment, Is.EqualTo(segments[1]));
            Assert.That(next.Endpoint, Is.EqualTo(RopeEndpoint.A));

            Destroy(firstObject, secondObject, thirdObject);
        }

        [Test]
        public void FourDirections_CanProduceDifferentConnectionGraphs()
        {
            GameObject firstObject = CreateSegment(
                new Vector3(-2f, 0f, 0f),
                new Vector3(0f, 0f, 0f));
            GameObject secondObject = CreateSegment(
                new Vector3(0f, 0f, 5f),
                new Vector3(0f, 2f, 5f));
            RopeSegment[] segments =
            {
                firstObject.GetComponent<RopeSegment>(),
                secondObject.GetComponent<RopeSegment>()
            };

            RopePathGraph front = RopePathGraphBuilder.Build(
                segments,
                RopeProjectionDirection.Front,
                0.01f);
            RopePathGraph right = RopePathGraphBuilder.Build(
                segments,
                RopeProjectionDirection.Right,
                0.01f);

            Assert.That(front.Connections.Count, Is.EqualTo(1));
            Assert.That(right.Connections.Count, Is.EqualTo(0));

            Destroy(firstObject, secondObject);
        }

        [Test]
        public void Platform_BindsToEndpointWithoutImplementingMovement()
        {
            GameObject networkObject = new GameObject("Network");
            RopePathNetwork network = networkObject.AddComponent<RopePathNetwork>();
            GameObject segmentObject = CreateSegment(
                new Vector3(-1f, 0f, 0f),
                new Vector3(1f, 0f, 0f));
            segmentObject.transform.SetParent(networkObject.transform, true);
            RopeSegment segment = segmentObject.GetComponent<RopeSegment>();
            GameObject platformObject = new GameObject("Platform");
            RopePlatform platform = platformObject.AddComponent<RopePlatform>();

            Assert.That(platform.Bind(network, segment, RopeEndpoint.B), Is.True);
            platform.SnapTransformToBinding();

            Assert.That(platform.IsBound, Is.True);
            Assert.That(platform.BoundSegment, Is.EqualTo(segment));
            Assert.That(platform.transform.position, Is.EqualTo(segment.GetWorldEndpoint(RopeEndpoint.B)));

            Destroy(networkObject, platformObject);
        }

        [Test]
        public void Segment_CreatesPlaceholderRopeAndEndpointVisuals()
        {
            GameObject segmentObject = new GameObject("Rope");
            RopeSegment segment = segmentObject.AddComponent<RopeSegment>();

            segment.EnsureVisuals();
            Assert.That(segment.HasVisuals, Is.True);
            Assert.That(segmentObject.transform.Find("__RopeVisual"), Is.Not.Null);
            Assert.That(segmentObject.transform.Find("__RopeVisual/__RopeBody"), Is.Not.Null);
            Assert.That(segmentObject.transform.Find("__RopeVisual/__RopeEndpointA"), Is.Not.Null);
            Assert.That(segmentObject.transform.Find("__RopeVisual/__RopeEndpointB"), Is.Not.Null);

            Destroy(segmentObject);
        }

        private static GameObject CreateSegment(Vector3 endpointA, Vector3 endpointB)
        {
            GameObject gameObject = new GameObject("Rope");
            RopeSegment segment = gameObject.AddComponent<RopeSegment>();
            segment.SetLocalEndpoint(RopeEndpoint.A, endpointA);
            segment.SetLocalEndpoint(RopeEndpoint.B, endpointB);
            return gameObject;
        }

        private static void Destroy(params GameObject[] objects)
        {
            for (int index = 0; index < objects.Length; index++)
            {
                Object.DestroyImmediate(objects[index]);
            }
        }
    }
}
