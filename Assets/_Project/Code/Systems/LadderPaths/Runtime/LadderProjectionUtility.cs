using UnityEngine;

namespace Project.LadderPaths
{
    public static class LadderProjectionUtility
    {
        public static Vector3 ScreenRight(LadderProjectionDirection direction)
        {
            switch (direction)
            {
                case LadderProjectionDirection.Right:
                    return Vector3.back;
                case LadderProjectionDirection.Back:
                    return Vector3.left;
                case LadderProjectionDirection.Left:
                    return Vector3.forward;
                default:
                    return Vector3.right;
            }
        }

        public static Vector3 ViewDepth(LadderProjectionDirection direction)
        {
            switch (direction)
            {
                case LadderProjectionDirection.Right:
                    return Vector3.right;
                case LadderProjectionDirection.Back:
                    return Vector3.back;
                case LadderProjectionDirection.Left:
                    return Vector3.left;
                default:
                    return Vector3.forward;
            }
        }

        public static Vector2 Project(
            Vector3 worldPosition,
            LadderProjectionDirection direction)
        {
            return new Vector2(
                Vector3.Dot(worldPosition, ScreenRight(direction)),
                worldPosition.y);
        }

        public static float Depth(
            Vector3 worldPosition,
            LadderProjectionDirection direction)
        {
            return Vector3.Dot(worldPosition, ViewDepth(direction));
        }

        public static Vector3 Unproject(
            Vector2 screenPosition,
            float depth,
            LadderProjectionDirection direction)
        {
            return ScreenRight(direction) * screenPosition.x +
                   Vector3.up * screenPosition.y +
                   ViewDepth(direction) * depth;
        }
    }
}
