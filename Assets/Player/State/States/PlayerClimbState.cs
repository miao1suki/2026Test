using Project.InputAbstraction;
using Project.LadderPaths;
using UnityEngine;

namespace Project.Player
{
    public sealed class PlayerClimbState : IPlayerState
    {
        public void Enter(PlayerStateContext context)
        {
            context.Controller.SetClimbing(true);
        }

        public void Tick(PlayerStateContext context)
        {
            LadderSegment ladder = context.CurrentLadder;
            if (ladder == null)
            {
                context.Controller.ReturnToNormal();
                return;
            }

            float verticalInput =
                GameInput.ReadVector2(InputActionId.Move).y;
            if (Mathf.Abs(verticalInput) <= 0.01f)
            {
                return;
            }

            LadderEndpoint endpoint = verticalInput > 0f
                ? LadderEndpoint.Top
                : LadderEndpoint.Bottom;
            Vector3 target = ladder.GetWorldEndpoint(endpoint);
            Vector3 direction =
                (target - context.Controller.transform.position)
                .normalized;
            context.Motor.linearVelocity =
                direction * context.Controller.ClimbSpeed;
        }

        public void Exit(PlayerStateContext context)
        {
            context.Controller.SetClimbing(false);
            context.CurrentLadder = null;
        }
    }
}
