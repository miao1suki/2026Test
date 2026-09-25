using Project.CameraModes;
using Project.InputAbstraction;
using Project.RopePaths;
using UnityEngine;

namespace Project.Player
{
    public sealed class PlayerNormalState : IPlayerState
    {
        public void Enter(PlayerStateContext context)
        {
            context.Controller.SetFallbackNormalState();
        }

        public void Tick(PlayerStateContext context)
        {
            PlayerController controller = context.Controller;

            if (GameInput.WasTriggeredThisFrame(
                    InputActionId.CameraModeSwitch))
            {
                controller.ToggleViewMode();
                return;
            }

            Vector2 move =
                GameInput.ReadVector2(InputActionId.Move);

            controller.UpdateCrouch();
            controller.UpdateSprint();

            if (controller.IsSide2D())
            {
                controller.Update2DRotation();
                controller.Move2D(move.x);
            }
            else
            {
                controller.Update3DLook();
                controller.Move3D(move);
            }

            if (controller.HasActionBinding(InputActionId.Jump))
            {
                TryAction(controller, InputActionId.Jump);
            }
            else
            {
                controller.UpdateJump();
            }

            TryAction(controller, InputActionId.Interact);
            TryAction(controller, InputActionId.Attack);
            TryAction(controller, InputActionId.Submit);
            TryAction(controller, InputActionId.Cancel);
            TryAction(controller, InputActionId.Pause);
        }

        public void Exit(PlayerStateContext context)
        {
        }

        private static void TryAction(
            PlayerController controller,
            InputActionId inputAction)
        {
            if (GameInput.WasTriggeredThisFrame(inputAction))
            {
                controller.TryPlayAction(inputAction);
            }
        }
    }
}
