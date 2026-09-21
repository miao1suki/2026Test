using System;
using UnityEditor;

namespace Project.CubeMapEditing.Editor
{
    internal enum CubeMapEditorToolMode
    {
        Assembly = 0,
        GridMap = 1,
    }

    internal static class CubeMapEditorToolState
    {
        private const string Preference = "2026Test.CubeMap.ActiveTool";

        internal static event Action Changed;

        internal static CubeMapEditorToolMode ActiveMode
        {
            get => (CubeMapEditorToolMode)EditorPrefs.GetInt(Preference, 0);
            set
            {
                if (ActiveMode == value)
                {
                    return;
                }

                EditorPrefs.SetInt(Preference, (int)value);
                Changed?.Invoke();
                SceneView.RepaintAll();
            }
        }

        internal static bool IsAssembly => ActiveMode == CubeMapEditorToolMode.Assembly;
        internal static bool IsGridMap => ActiveMode == CubeMapEditorToolMode.GridMap;
    }
}
