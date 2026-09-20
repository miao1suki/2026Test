using UnityEngine;

namespace Project.CameraModes
{
    /// <summary>
    /// Implement this on gameplay objects that want to provide a dedicated
    /// camera-follow transform without coupling gameplay code to the controller.
    /// </summary>
    public interface ICameraFollowTarget
    {
        Transform CameraFollowTransform { get; }
    }
}
