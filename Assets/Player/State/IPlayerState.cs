namespace Project.Player
{
    public interface IPlayerState
    {
        void Enter(PlayerStateContext context);
        void Tick(PlayerStateContext context);
        void Exit(PlayerStateContext context);
    }
}
