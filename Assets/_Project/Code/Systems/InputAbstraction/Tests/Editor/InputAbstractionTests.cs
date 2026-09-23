using NUnit.Framework;
using UnityEngine;

namespace Project.InputAbstraction.Tests
{
    public sealed class InputAbstractionTests
    {
        [TearDown]
        public void TearDown()
        {
            if (InputService.Instance != null)
            {
                Object.DestroyImmediate(InputService.Instance.gameObject);
            }
        }

        [Test]
        public void ExternalSource_IsVisibleThroughStableFacade()
        {
            FakeInputSource fake = new FakeInputSource
            {
                Pressed = true,
                Move = new Vector2(0.25f, 0.75f),
                DeviceMode = InputDeviceMode.Touch,
            };
            GameInput.UseExternalSource(fake);

            Assert.That(GameInput.IsPressed(InputActionId.Jump), Is.True);
            Assert.That(GameInput.ReadVector2(InputActionId.Move), Is.EqualTo(fake.Move));
            Assert.That(GameInput.ActiveDeviceMode, Is.EqualTo(InputDeviceMode.Touch));

            GameInput.ClearExternalSource(fake);
        }

        [Test]
        public void DefaultActionIds_AreIndependentOfDeviceBindings()
        {
            Assert.That(InputActionId.Move.ToString(), Is.EqualTo("Move"));
            Assert.That(InputActionId.CameraModeSwitch.ToString(), Is.EqualTo("CameraModeSwitch"));
            Assert.That(InputActionId.PointerPrimary.ToString(), Is.EqualTo("PointerPrimary"));
        }

        private sealed class FakeInputSource : IInputSource
        {
            public bool Pressed;
            public Vector2 Move;
            public InputDeviceMode DeviceMode;

            public InputDeviceMode ActiveDeviceMode => DeviceMode;
            public bool IsActionPressed(InputActionId action) => Pressed;
            public bool WasActionPressedThisFrame(InputActionId action) => Pressed;
            public bool WasActionReleasedThisFrame(InputActionId action) => false;
            public float ReadAxis(InputActionId action) => Move.x;
            public Vector2 ReadVector2(InputActionId action) => Move;
            public Vector2 PointerPosition => Vector2.zero;
            public Vector2 PointerDelta => Vector2.zero;
            public bool HasKeyboard => false;
            public bool HasMouse => false;
            public bool HasGamepad => false;
            public bool HasTouch => true;
        }
    }
}
