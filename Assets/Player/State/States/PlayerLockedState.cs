namespace Project.Player
{
    public sealed class PlayerLockedState : IPlayerState
    {
        public void Enter(PlayerStateContext context)
        {
            context.Controller.StopHorizontalMovement();
        }

        public void Tick(PlayerStateContext context)
        {
        }

        public void Exit(PlayerStateContext context)
        {
        }
    }
}
