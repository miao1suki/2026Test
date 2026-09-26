using NUnit.Framework;
using Project.LadderPaths;
using Project.PlatformPaths;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.RopePaths.Editor.Tests
{
    public sealed class PathMechanismAuthoringServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Selection.activeObject = null;
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void CreateRope_CreatesNetworkAndVisibleSegment()
        {
            RopeSegment rope = PathMechanismAuthoringService.CreateRope();

            Assert.That(rope, Is.Not.Null);
            Assert.That(rope.GetComponentInParent<RopePathNetwork>(), Is.Not.Null);
            Assert.That(rope.HasVisuals, Is.True);
            Assert.That(Selection.activeGameObject, Is.EqualTo(rope.gameObject));
        }

        [Test]
        public void ContinueRope_FromB_CreatesExactPhysicalConnection()
        {
            RopeSegment first = PathMechanismAuthoringService.CreateRope();
            Vector3 connection = first.GetWorldEndpoint(RopeEndpoint.B);

            RopeSegment second =
                PathMechanismAuthoringService.ContinueRope(RopeEndpoint.B);

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(
                Vector3.Distance(
                    connection,
                    second.GetWorldEndpoint(RopeEndpoint.A)),
                Is.LessThan(0.0001f));
        }

        [Test]
        public void ContinueLadder_Above_MatchesTopAndBottom()
        {
            LadderSegment first = PathMechanismAuthoringService.CreateLadder();
            Vector3 connection = first.GetWorldEndpoint(LadderEndpoint.Top);

            LadderSegment second = PathMechanismAuthoringService.ContinueLadder(true);

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(
                Vector3.Distance(
                    connection,
                    second.GetWorldEndpoint(LadderEndpoint.Bottom)),
                Is.LessThan(0.0001f));
        }

        [Test]
        public void CreateBoundPlatform_CreatesRopeWhenMissingAndBindsPlatform()
        {
            GameObject marker = new GameObject("CreationMarker");
            marker.transform.position = new Vector3(5f, 2f, -3f);
            Selection.activeGameObject = marker;

            RopePlatform platform =
                PathMechanismAuthoringService.CreateBoundPlatform();

            Assert.That(platform, Is.Not.Null);
            Assert.That(platform.IsBound, Is.True);
            Assert.That(platform.BoundSegment, Is.Not.Null);
            Assert.That(platform.Network, Is.Not.Null);
            Assert.That(platform.GetComponent<PlatformMove>(), Is.Not.Null);
            Assert.That(platform.GetComponent<Collider>(), Is.Not.Null);
            Assert.That(
                Vector3.Distance(
                    marker.transform.position,
                    platform.BoundSegment.transform.position),
                Is.LessThan(0.0001f));
        }

        [Test]
        public void RepairActiveScene_CollectsOrphansAndReportsUnboundPlatform()
        {
            RopeSegment rope = new GameObject("OrphanRope").AddComponent<RopeSegment>();
            LadderSegment ladder =
                new GameObject("OrphanLadder").AddComponent<LadderSegment>();
            RopePlatform platform =
                GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<RopePlatform>();

            PathMechanismAuthoringService.RepairReport report =
                PathMechanismAuthoringService.RepairActiveScene();

            Assert.That(rope.GetComponentInParent<RopePathNetwork>(), Is.Not.Null);
            Assert.That(ladder.GetComponentInParent<LadderPathNetwork>(), Is.Not.Null);
            Assert.That(platform.GetComponentInParent<RopePathNetwork>(), Is.Not.Null);
            Assert.That(report.UnboundPlatforms, Is.EqualTo(1));
            Assert.That(report.Repaired, Is.GreaterThanOrEqualTo(2));
        }
    }
}
