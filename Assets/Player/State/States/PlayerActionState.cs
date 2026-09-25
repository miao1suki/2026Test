using Project.InputAbstraction;

namespace Project.Player
{
    public sealed class PlayerActionState : IPlayerState
    {
        private ActSO activeAction;

        public void Enter(PlayerStateContext context)
        {
            activeAction = context.CurrentAction;
            if (activeAction != null &&
                activeAction.LockMovement)
            {
                context.Controller.StopHorizontalMovement();
            }

            if (!context.ActionRunner.Play(activeAction))
            {
                activeAction = null;
                context.Controller.ReturnToNormal();
            }
        }

        public void Tick(PlayerStateContext context)
        {
            if (activeAction == null)
            {
                return;
            }

            if (!activeAction.LockMovement)
            {
                context.Controller.UpdateMovementFromCurrentView(
                    GameInput.ReadVector2(InputActionId.Move));
            }

            if (context.ActionRunner.IsPlaying)
            {
                return;
            }

            ActSO nextAction =
                context.Controller.HasActionInput()
                    ? null
                    : activeAction.DefaultNextAction;
            context.Controller.NotifyActionCompleted(activeAction);
            activeAction = null;
            context.CurrentAction = null;

            if (nextAction != null &&
                context.Controller.TryPlayAction(nextAction))
            {
                return;
            }

            context.Controller.ReturnToNormal();
        }

        public void Exit(PlayerStateContext context)
        {
            if (context.ActionRunner.IsPlaying)
            {
                context.ActionRunner.Stop();
            }

            context.CurrentAction = null;
            activeAction = null;
        }
    }
}
