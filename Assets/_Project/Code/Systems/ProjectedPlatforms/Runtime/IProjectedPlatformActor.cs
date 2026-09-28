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
}
