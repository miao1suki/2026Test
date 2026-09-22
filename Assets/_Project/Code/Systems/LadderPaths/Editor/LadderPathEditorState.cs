using UnityEditor;

namespace Project.LadderPaths.Editor
{
    internal static class LadderPathEditorState
    {
        private const string EndpointPreference =
            "2026Test.LadderPaths.ActiveEndpoint";

        internal static LadderEndpoint ActiveEndpoint
        {
            get => (LadderEndpoint)EditorPrefs.GetInt(
                EndpointPreference,
                (int)LadderEndpoint.Top);
            set => EditorPrefs.SetInt(EndpointPreference, (int)value);
        }
    }
}
