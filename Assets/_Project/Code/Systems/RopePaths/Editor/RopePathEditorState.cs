using UnityEditor;

namespace Project.RopePaths.Editor
{
    internal static class RopePathEditorState
    {
        private const string EndpointPreference = "2026Test.RopePaths.ActiveEndpoint";

        internal static RopeEndpoint ActiveEndpoint
        {
            get => (RopeEndpoint)EditorPrefs.GetInt(
                EndpointPreference,
                (int)RopeEndpoint.A);
            set => EditorPrefs.SetInt(EndpointPreference, (int)value);
        }
    }
}
