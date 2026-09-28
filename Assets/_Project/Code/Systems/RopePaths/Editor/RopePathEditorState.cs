using System.Collections.Generic;
using UnityEditor;

namespace Project.RopePaths.Editor
{
    internal enum RopePathPreviewMode
    {
        Front = 0,
        Right = 1,
        Back = 2,
        Left = 3,
        All = 4
    }

    internal static class RopePathEditorState
    {
        private const string EndpointPreference = "2026Test.RopePaths.ActiveEndpoint";
        private static readonly RopeProjectionDirection[] AllDirections =
        {
            RopeProjectionDirection.Front,
            RopeProjectionDirection.Right,
            RopeProjectionDirection.Back,
            RopeProjectionDirection.Left
        };

        internal static RopeEndpoint ActiveEndpoint
        {
            get => (RopeEndpoint)EditorPrefs.GetInt(
                EndpointPreference,
                (int)RopeEndpoint.A);
            set => EditorPrefs.SetInt(EndpointPreference, (int)value);
        }

        internal static IReadOnlyList<RopeProjectionDirection> PreviewDirections(
            RopePathPreviewMode mode)
        {
            if (mode == RopePathPreviewMode.All)
            {
                return AllDirections;
            }

            return new[] { (RopeProjectionDirection)mode };
        }
    }
}
