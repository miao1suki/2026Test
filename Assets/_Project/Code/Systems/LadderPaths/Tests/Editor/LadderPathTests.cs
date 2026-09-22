using NUnit.Framework;
using UnityEngine;

namespace Project.LadderPaths.Tests
{
    public sealed class LadderPathTests
    {
        [Test]
        public void FrontProjection_ConnectsTopToBottomAcrossDepth()
        {
            LadderSegment lower = CreateLadder(new Vector3(0f, 0f, 0f));
            LadderSegment upper = CreateLadder(new Vector3(0f, 2f, 4f));

            LadderPathGraph graph = LadderPathGraphBuilder.Build(
                new[] { lower, upper },
                LadderProjectionDirection.Front,
                0.01f);

            Assert.That(graph.Connections.Count, Is.EqualTo(1));
            Assert.That(
                graph.TryGetNext(lower, LadderEndpoint.Top, out LadderEndpointReference next),
                Is.True);
            Assert.That(next.Segment, Is.EqualTo(upper));
            Assert.That(next.Endpoint, Is.EqualTo(LadderEndpoint.Bottom));

            Destroy(lower.gameObject, upper.gameObject);
        }

        [Test]
        public void FrontFace_DoesNotConnectToSideFace()
        {
            LadderSegment lower = CreateLadder(Vector3.zero);
            LadderSegment upper = CreateLadder(new Vector3(0f, 2f, 3f));
            upper.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            LadderPathGraph graph = LadderPathGraphBuilder.Build(
                new[] { lower, upper },
                LadderProjectionDirection.Front,
                0.01f);

            Assert.That(
                lower.GetVisibleFaceFamily(LadderProjectionDirection.Front),
                Is.EqualTo(LadderFaceFamily.FrontBack));
            Assert.That(
                upper.GetVisibleFaceFamily(LadderProjectionDirection.Front),
                Is.EqualTo(LadderFaceFamily.LeftRight));
            Assert.That(graph.Connections.Count, Is.EqualTo(0));

            Destroy(lower.gameObject, upper.gameObject);
        }

        [Test]
        public void SideProjection_ConnectsLeftRightFaceFamilyAcrossDepth()
        {
            LadderSegment lower = CreateLadder(new Vector3(0f, 0f, 0f));
            LadderSegment upper = CreateLadder(new Vector3(4f, 2f, 0f));

            LadderPathGraph graph = LadderPathGraphBuilder.Build(
                new[] { lower, upper },
                LadderProjectionDirection.Right,
                0.01f);

            Assert.That(
                lower.GetVisibleFaceFamily(LadderProjectionDirection.Right),
                Is.EqualTo(LadderFaceFamily.LeftRight));
            Assert.That(graph.Connections.Count, Is.EqualTo(1));

            Destroy(lower.gameObject, upper.gameObject);
        }

        [Test]
        public void ProjectionFallback_RequiresClearLineOfSight()
        {
            GameObject networkObject = new GameObject("Network");
            LadderPathNetwork network = networkObject.AddComponent<LadderPathNetwork>();
            LadderSegment ladder = CreateLadder(Vector3.zero);
            ladder.transform.SetParent(networkObject.transform, true);
            network.RefreshSegments();
            Vector3 playerPosition = new Vector3(0f, 0f, -2f);

            Assert.That(
                network.TryFindClimbableLadder(
                    playerPosition,
                    LadderProjectionDirection.Front,
                    0.1f,
                    0.1f,
                    1 << 8,
                    out LadderClimbContact clearContact),
                Is.True);
            Assert.That(clearContact.Segment, Is.EqualTo(ladder));

            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Obstacle";
            obstacle.layer = 8;
            obstacle.transform.position = new Vector3(0f, 0f, -1f);
            obstacle.transform.localScale = new Vector3(1f, 2f, 0.2f);
            Physics.SyncTransforms();

            Assert.That(
                network.TryFindClimbableLadder(
                    playerPosition,
                    LadderProjectionDirection.Front,
                    0.1f,
                    0.1f,
                    1 << 8,
                    out _),
                Is.False);

            Destroy(networkObject, obstacle);
        }

        [Test]
        public void HighestLadder_AllowsTopExitGraceZone()
        {
            GameObject networkObject = new GameObject("Network");
            LadderPathNetwork network = networkObject.AddComponent<LadderPathNetwork>();
            LadderSegment ladder = CreateLadder(Vector3.zero);
            ladder.transform.SetParent(networkObject.transform, true);
            network.RefreshSegments();

            Assert.That(
                network.IsTopOfPath(ladder, LadderProjectionDirection.Front),
                Is.True);
            Assert.That(
                network.CanUseTopExitGrace(
                    ladder,
                    new Vector3(0f, 1.1f, 0f),
                    LadderProjectionDirection.Front,
                    0.1f,
                    0.15f),
                Is.True);

            Destroy(networkObject);
        }

        [Test]
        public void PlaceholderVisual_HasNoEndpointObjects()
        {
            LadderSegment ladder = CreateLadder(Vector3.zero);
            ladder.EnsureVisuals();

            Assert.That(ladder.HasVisuals, Is.True);
            Assert.That(ladder.transform.Find("__LadderVisual/Rail_Left"), Is.Not.Null);
            Assert.That(ladder.transform.Find("__LadderVisual/Rail_Right"), Is.Not.Null);
            Assert.That(ladder.transform.Find("__LadderEndpointA"), Is.Null);
            Assert.That(ladder.transform.Find("__LadderEndpointB"), Is.Null);

            Destroy(ladder.gameObject);
        }

        private static LadderSegment CreateLadder(Vector3 position)
        {
            GameObject gameObject = new GameObject("Ladder");
            gameObject.transform.position = position;
            return gameObject.AddComponent<LadderSegment>();
        }

        private static void Destroy(params GameObject[] objects)
        {
            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] != null)
                {
                    Object.DestroyImmediate(objects[index]);
                }
            }
        }
    }
}
