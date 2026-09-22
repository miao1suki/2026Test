using UnityEngine;

namespace Project.LadderPaths
{
    public enum LadderEndpoint
    {
        Bottom = 0,
        Top = 1
    }

    public enum LadderProjectionDirection
    {
        Front = 0,
        Right = 1,
        Back = 2,
        Left = 3
    }

    public enum LadderFaceFamily
    {
        FrontBack = 0,
        LeftRight = 1
    }

    public enum LadderContactSource
    {
        Trigger = 0,
        ProjectionFallback = 1,
        TopExitGrace = 2
    }

    public enum LadderClimbExitReason
    {
        LostContact = 0,
        GraceExpired = 1,
        SensorDisabled = 2
    }

    public readonly struct LadderClimbContact
    {
        public LadderClimbContact(
            LadderPathNetwork network,
            LadderSegment segment,
            LadderProjectionDirection direction,
            Vector3 closestPoint,
            LadderContactSource source)
        {
            Network = network;
            Segment = segment;
            Direction = direction;
            ClosestPoint = closestPoint;
            Source = source;
        }

        public LadderPathNetwork Network { get; }
        public LadderSegment Segment { get; }
        public LadderProjectionDirection Direction { get; }
        public Vector3 ClosestPoint { get; }
        public LadderContactSource Source { get; }
        public bool IsTopExitGrace => Source == LadderContactSource.TopExitGrace;

        public LadderClimbContact WithSource(LadderContactSource source)
        {
            return new LadderClimbContact(
                Network,
                Segment,
                Direction,
                ClosestPoint,
                source);
        }
    }

    public interface ILadderClimbStateReceiver
    {
        void OnLadderClimbEnter(LadderClimbContact contact);
        void OnLadderClimbStay(LadderClimbContact contact);
        void OnLadderClimbExit(LadderClimbExitReason reason);
    }
}
