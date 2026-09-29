using Project.RopePaths;
using UnityEngine;

namespace Project.ProjectedPlatforms
{
    [System.Flags]
    public enum ProjectedPlatformDirections
    {
        None = 0,
        Front = 1 << 0,
        Right = 1 << 1,
        Back = 1 << 2,
        Left = 1 << 3,
        All = Front | Right | Back | Left
    }

    /// <summary>
    /// Implemented by a 3D physics actor that can use projected one-way platforms.
    /// The platform never controls the actor or the camera; it only reads this state.
    /// </summary>
    public interface IProjectedPlatformActor
    {
        Rigidbody ProjectedPlatformBody { get; }
        RopeProjectionDirection ProjectedPlatformDirection { get; }
        bool IsProjectedPlatformModeActive { get; }
    }

    /// <summary>
    /// Describes the invisible depth correction performed when an actor lands
    /// on a platform through its orthographic projection.
    /// </summary>
    public readonly struct ProjectedPlatformAlignment
    {
        public ProjectedPlatformAlignment(
            Collider platformCollider,
            RopeProjectionDirection direction,
            Vector3 worldPosition)
        {
            PlatformCollider = platformCollider;
            Direction = direction;
            WorldPosition = worldPosition;
        }

        public Collider PlatformCollider { get; }
        public RopeProjectionDirection Direction { get; }
        public Vector3 WorldPosition { get; }
    }

    /// <summary>
    /// Optional actor-side handshake. Implementors decide how their motor is
    /// teleported onto the physical platform depth without coupling the
    /// platform system to a concrete player controller.
    /// </summary>
    public interface IProjectedPlatformAlignmentReceiver
    {
        bool TryAlignProjectedPlatformDepth(
            ProjectedPlatformAlignment alignment);
    }
}
