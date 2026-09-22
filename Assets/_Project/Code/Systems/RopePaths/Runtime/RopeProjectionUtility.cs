using UnityEngine;

namespace Project.RopePaths
{
    public static class RopeProjectionUtility
    {
        public static Vector3 ScreenRight(RopeProjectionDirection direction)
        {
            switch (direction)
            {
                case RopeProjectionDirection.Right:
                    return Vector3.back;
                case RopeProjectionDirection.Back:
                    return Vector3.left;
                case RopeProjectionDirection.Left:
                    return Vector3.forward;
                default:
                    return Vector3.right;
            }
        }

        public static Vector3 ScreenUp(RopeProjectionDirection direction)
        {
            return Vector3.up;
        }

        public static Vector3 ViewDepth(RopeProjectionDirection direction)
        {
            switch (direction)
            {
                case RopeProjectionDirection.Right:
                    return Vector3.right;
                case RopeProjectionDirection.Back:
                    return Vector3.back;
                case RopeProjectionDirection.Left:
                    return Vector3.left;
                default:
                    return Vector3.forward;
            }
        }

        public static Vector2 Project(
            Vector3 worldPosition,
            RopeProjectionDirection direction)
        {
            Vector3 right = ScreenRight(direction);
            Vector3 up = ScreenUp(direction);
            return new Vector2(
                Vector3.Dot(worldPosition, right),
                Vector3.Dot(worldPosition, up));
        }

        public static float Depth(
            Vector3 worldPosition,
            RopeProjectionDirection direction)
        {
            return Vector3.Dot(worldPosition, ViewDepth(direction));
        }

        public static Vector3 Unproject(
            Vector2 screenPosition,
            float depth,
            RopeProjectionDirection direction)
        {
            return ScreenRight(direction) * screenPosition.x +
                   ScreenUp(direction) * screenPosition.y +
                   ViewDepth(direction) * depth;
        }
    }
}


