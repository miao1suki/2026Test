using System.Collections.Generic;

namespace Project.Player
{
    public sealed class PlayerStateMachine
    {
        private readonly Dictionary<PlayerStateId, IPlayerState> states =
            new Dictionary<PlayerStateId, IPlayerState>();

        private IPlayerState currentState;

        public PlayerStateId CurrentId { get; private set; }

        public void Register(PlayerStateId id, IPlayerState state)
        {
            if (state != null)
            {
                states[id] = state;
            }
        }

        public bool Start(PlayerStateId id, PlayerStateContext context)
        {
            if (!states.TryGetValue(id, out IPlayerState state))
            {
                return false;
            }

            CurrentId = id;
            currentState = state;
            currentState.Enter(context);
            return true;
        }

        public bool Change(PlayerStateId id, PlayerStateContext context)
        {
            if (CurrentId == id)
            {
                return true;
            }

            if (!states.TryGetValue(id, out IPlayerState nextState))
            {
                return false;
            }

            currentState?.Exit(context);
            CurrentId = id;
            currentState = nextState;
            currentState.Enter(context);
            return true;
        }

        public void Tick(PlayerStateContext context)
        {
            currentState?.Tick(context);
        }
    }
}
