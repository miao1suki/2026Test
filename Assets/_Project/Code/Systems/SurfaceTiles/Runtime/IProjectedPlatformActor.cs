using Project.RopePaths;
using UnityEngine;

namespace Project.SurfaceTiles
{
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
