using NUnit.Framework;
using Project.ProjectedPlatforms.Editor;
using Project.RopePaths;
using UnityEngine;

namespace Project.ProjectedPlatforms.Tests
{
    internal sealed class ProjectedPlatformTestActor :
        MonoBehaviour,
        IProjectedPlatformActor,
        IProjectedPlatformAlignmentReceiver
    {
        public Rigidbody Body { get; set; }
        public RopeProjectionDirection Direction { get; set; }
        public bool Active { get; set; }

        public Rigidbody ProjectedPlatformBody => Body;
        public RopeProjectionDirection ProjectedPlatformDirection =>
            Direction;
        public bool IsProjectedPlatformModeActive => Active;
        public int AlignmentCount { get; private set; }
        public ProjectedPlatformAlignment LastAlignment { get; private set; }

        public bool TryAlignProjectedPlatformDepth(
            ProjectedPlatformAlignment alignment)
        {
            AlignmentCount++;
            LastAlignment = alignment;
            if (Body != null)
            {
                Body.position = alignment.WorldPosition;
            }

            return true;
        }
    }

    public sealed class ProjectedOneWayPlatformTests
    {
        [Test]
        public void IgnoresBelowAndAscendingButLandsFromAbove()
        {
            Bounds platform = new Bounds(Vector3.zero, new Vector3(4f, 1f, 2f));
            Bounds below = new Bounds(
                new Vector3(0f, -1f, 0f),
                new Vector3(1f, 1f, 1f));
            Bounds above = new Bounds(
                new Vector3(0f, 1.1f, 0f),
                new Vector3(1f, 1f, 1f));

            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                true, true, false, platform, below, -1f, 0.08f), Is.True);
            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                true, true, false, platform, above, -1f, 0.08f), Is.False);
            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                true, true, false, platform, above, 2f, 0.08f), Is.True);
        }

        [Test]
        public void ReleasesSafelyAfterLeavingTwoDMode()
        {
            Bounds platform = new Bounds(Vector3.zero, new Vector3(4f, 1f, 2f));
            Bounds overlapping = new Bounds(
                new Vector3(0f, 0.25f, 0f),
                Vector3.one);
            Bounds clear = new Bounds(
                new Vector3(0f, 3f, 0f),
                Vector3.one);
            Bounds standing = new Bounds(
                new Vector3(0f, 1f, 0f),
                Vector3.one);

            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                false, true, true, platform, overlapping, 0f, 0.08f), Is.True);
            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                false, true, true, platform, clear, 0f, 0.08f), Is.False);
            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                false, true, true, platform, standing, 0f, 0.08f), Is.False);
        }

        [Test]
        public void AuthoringWorksOnPlainObjectWithoutSurfacePainting()
        {
            GameObject target = new GameObject("PlainPlatform");
            try
            {
                ProjectedOneWayPlatform platform =
                    ProjectedPlatformAuthoringService.Enable(target);

                Assert.That(platform, Is.Not.Null);
                Assert.That(target.GetComponent<BoxCollider>(), Is.Not.Null);
                Assert.That(
                    target.transform.Find("__ProjectedPlatformSensor"),
                    Is.Not.Null);

                ProjectedPlatformAuthoringService.Disable(platform);
                Assert.That(
                    target.GetComponent<ProjectedOneWayPlatform>(),
                    Is.Null);
                Assert.That(
                    target.transform.Find("__ProjectedPlatformSensor"),
                    Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ProjectionProxy_ExtendsAlongCurrentViewDepth()
        {
            GameObject platformObject = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            GameObject actorObject = new GameObject("Actor");
            try
            {
                ProjectedOneWayPlatform platform =
                    platformObject.AddComponent<ProjectedOneWayPlatform>();
                platform.ProjectionDepth = 40f;
                platform.EnsureSetup();

                Rigidbody body = actorObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
                BoxCollider actorCollider =
                    actorObject.AddComponent<BoxCollider>();
                ProjectedPlatformTestActor actor =
                    actorObject.AddComponent<ProjectedPlatformTestActor>();
                actor.Body = body;
                actor.Active = true;
                actor.Direction = RopeProjectionDirection.Front;

                platform.RegisterCandidate(actorCollider);
                Assert.That(platform.ProjectionCollider.enabled, Is.True);
                Assert.That(
                    platform.ProjectionCollider.bounds.size.z,
                    Is.GreaterThanOrEqualTo(39.9f));

                actor.Direction = RopeProjectionDirection.Right;
                platform.RegisterCandidate(actorCollider);
                Assert.That(
                    platform.ProjectionCollider.bounds.size.x,
                    Is.GreaterThanOrEqualTo(39.9f));
            }
            finally
            {
                Object.DestroyImmediate(actorObject);
                Object.DestroyImmediate(platformObject);
            }
        }

        [Test]
        public void LandingAlignsActorToPhysicalDepthAcrossViewChanges()
        {
            GameObject platformObject = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            GameObject actorObject = new GameObject("Actor");
            try
            {
                platformObject.transform.position = new Vector3(4f, 0f, 9f);
                ProjectedOneWayPlatform platform =
                    platformObject.AddComponent<ProjectedOneWayPlatform>();
                platform.EnsureSetup();

                actorObject.transform.position = new Vector3(4f, 1f, -12f);
                Rigidbody body = actorObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
                BoxCollider actorCollider =
                    actorObject.AddComponent<BoxCollider>();
                ProjectedPlatformTestActor actor =
                    actorObject.AddComponent<ProjectedPlatformTestActor>();
                actor.Body = body;
                actor.Active = true;
                actor.Direction = RopeProjectionDirection.Front;

                Physics.SyncTransforms();
                platform.RegisterCandidate(actorCollider);

                Assert.That(actor.AlignmentCount, Is.EqualTo(1));
                Assert.That(body.position.x, Is.EqualTo(4f).Within(0.001f));
                Assert.That(body.position.z, Is.EqualTo(9f).Within(0.001f));
                Assert.That(
                    actor.LastAlignment.PlatformCollider,
                    Is.SameAs(platform.PlatformCollider));

                body.position = new Vector3(-8f, 1f, 9f);
                actor.Direction = RopeProjectionDirection.Right;
                Physics.SyncTransforms();
                platform.RegisterCandidate(actorCollider);

                Assert.That(actor.AlignmentCount, Is.EqualTo(2));
                Assert.That(body.position.x, Is.EqualTo(4f).Within(0.001f));
                Assert.That(body.position.z, Is.EqualTo(9f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(actorObject);
                Object.DestroyImmediate(platformObject);
            }
        }

        [Test]
        public void AlignmentOnlyCapturesDescendingActorAtProjectedTop()
        {
            Bounds platform = new Bounds(Vector3.zero, new Vector3(4f, 1f, 2f));
            Bounds standing = new Bounds(
                new Vector3(0f, 1f, 20f),
                Vector3.one);
            Bounds tooHigh = new Bounds(
                new Vector3(0f, 3f, 20f),
                Vector3.one);

            Assert.That(ProjectedOneWayPlatform.CanAlignProjectedLanding(
                platform,
                standing,
                -1f,
                0.08f,
                RopeProjectionDirection.Front), Is.True);
            Assert.That(ProjectedOneWayPlatform.CanAlignProjectedLanding(
                platform,
                standing,
                1f,
                0.08f,
                RopeProjectionDirection.Front), Is.False);
            Assert.That(ProjectedOneWayPlatform.CanAlignProjectedLanding(
                platform,
                tooHigh,
                -1f,
                0.08f,
                RopeProjectionDirection.Front), Is.False);
        }
    }
}
