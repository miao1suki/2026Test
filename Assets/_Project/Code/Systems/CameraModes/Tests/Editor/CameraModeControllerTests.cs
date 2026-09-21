using NUnit.Framework;
using UnityEngine;

namespace Project.CameraModes.Tests
{
    public sealed class CameraModeControllerTests
    {
        private GameObject cameraObject;
        private GameObject targetObject;
        private Camera cameraComponent;
        private CameraModeController controller;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Test Camera");
            cameraComponent = cameraObject.AddComponent<Camera>();
            controller = cameraObject.AddComponent<CameraModeController>();
            targetObject = new GameObject("Follow Target");
            targetObject.transform.position = new Vector3(4f, 2f, 3f);
            controller.Configure(cameraComponent, targetObject.transform, true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void Side2D_IsOrthographicAndHasNoHorizontalOffset()
        {
            controller.SnapToMode(CameraViewMode.Side2D);

            Assert.That(cameraComponent.orthographic, Is.True);
            Assert.That(cameraObject.transform.position.x, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(cameraObject.transform.rotation, Is.EqualTo(Quaternion.identity));
        }

        [Test]
        public void Perspective3D_LooksDownWithoutYawOffset()
        {
            controller.SnapToMode(CameraViewMode.Perspective3D);

            Vector3 expectedFocus = targetObject.transform.position + Vector3.up;
            Vector3 directionToFocus = (expectedFocus - cameraObject.transform.position).normalized;
            Assert.That(cameraComponent.orthographic, Is.False);
            Assert.That(cameraObject.transform.position.x, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(cameraObject.transform.eulerAngles.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(
                Vector3.Dot(cameraObject.transform.forward, directionToFocus),
                Is.GreaterThan(0.9999f));
        }

        [Test]
        public void InterruptedTransition_StartsFromCurrentVisualState()
        {
            controller.SnapToMode(CameraViewMode.Side2D);
            controller.SwitchMode(CameraViewMode.Perspective3D);
            controller.Tick(0.25f);

            Vector3 interruptedPosition = cameraObject.transform.position;
            Matrix4x4 interruptedProjection = cameraComponent.projectionMatrix;

            controller.SwitchMode(CameraViewMode.Side2D);
            controller.Tick(0f);

            Assert.That(cameraObject.transform.position, Is.EqualTo(interruptedPosition));
            AssertMatricesEqual(interruptedProjection, cameraComponent.projectionMatrix, 0.00001f);

            controller.Tick(2f);
            Assert.That(controller.IsTransitioning, Is.False);
            Assert.That(controller.CurrentMode, Is.EqualTo(CameraViewMode.Side2D));
            Assert.That(cameraComponent.orthographic, Is.True);
        }

        [Test]
        public void RepeatingSameTargetMode_DoesNotRestartTransition()
        {
            controller.SwitchMode(CameraViewMode.Perspective3D);
            controller.Tick(0.2f);
            float progress = controller.NormalizedTransitionTime;

            controller.SwitchMode(CameraViewMode.Perspective3D);

            Assert.That(controller.NormalizedTransitionTime, Is.EqualTo(progress).Within(0.0001f));
        }

        [Test]
        public void SnapToMode_NotifiesExactlyOnceWhenRequested()
        {
            int notificationCount = 0;
            controller.ModeChanged += _ => notificationCount++;

            controller.SnapToMode(CameraViewMode.Perspective3D);

            Assert.That(notificationCount, Is.EqualTo(1));
        }

        [Test]
        public void SnapToMode_CanSuppressNotification()
        {
            int notificationCount = 0;
            controller.ModeChanged += _ => notificationCount++;

            controller.SnapToMode(CameraViewMode.Perspective3D, false);

            Assert.That(notificationCount, Is.Zero);
        }

        [Test]
        public void DisablingDuringTransition_ClearsCustomProjectionState()
        {
            controller.SwitchMode(CameraViewMode.Perspective3D);
            controller.Tick(0.2f);

            controller.SettleTransitionForDisable();

            Assert.That(controller.IsTransitioning, Is.False);
            Assert.That(cameraComponent.orthographic, Is.False);
            Assert.That(controller.CurrentMode, Is.EqualTo(CameraViewMode.Perspective3D));
        }

        private static void AssertMatricesEqual(Matrix4x4 expected, Matrix4x4 actual, float tolerance)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    Assert.That(actual[row, column], Is.EqualTo(expected[row, column]).Within(tolerance));
                }
            }
        }
    }
}
