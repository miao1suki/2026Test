using NUnit.Framework;
using Project.ProjectedPlatforms.Editor;
using UnityEngine;

namespace Project.ProjectedPlatforms.Tests
{
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

            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                false, true, true, platform, overlapping, 0f, 0.08f), Is.True);
            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                false, true, true, platform, clear, 0f, 0.08f), Is.False);
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
    }
}
