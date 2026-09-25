using Project.CameraModes;
using Project.LadderPaths;
using Project.RopePaths;
using UnityEngine;

namespace Project.Player
{
    public sealed class PlayerStateContext
    {
        public PlayerStateContext(
            PlayerController controller,
            PlayerActionRunner actionRunner)
        {
            Controller = controller;
            ActionRunner = actionRunner;
        }

        public PlayerController Controller { get; }
        public PlayerActionRunner ActionRunner { get; }
        public Rigidbody Motor => Controller.Motor;
        public CameraFollowController CameraFollow => Controller.CameraFollow;
        public CameraModeController CameraMode => Controller.CameraMode;
        public RopePathNetwork[] RopeNetworks => Controller.RopeNetworks;
        public LadderSegment CurrentLadder { get; set; }
        public ActSO CurrentAction { get; set; }
    }
}
