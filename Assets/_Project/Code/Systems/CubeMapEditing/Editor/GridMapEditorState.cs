using UnityEditor;
using UnityEngine;

namespace Project.CubeMapEditing.Editor
{
    internal static class GridMapEditorState
    {
        private const string ItemGuidPreference = "2026Test.GridMap.ItemGuid";
        private const string FacePreference = "2026Test.GridMap.Face";
        private const string RotationPreference = "2026Test.GridMap.Rotation";
        private const string SnapPreference = "2026Test.GridMap.Snap";
        private const string OverlapPreference = "2026Test.GridMap.Overlap";

        internal static CubeMapFace ActiveFace
        {
            get => (CubeMapFace)Mathf.Clamp(
                EditorPrefs.GetInt(FacePreference, (int)CubeMapFace.Front),
                0,
                CubeMapLayoutMath.FaceCount - 1);
            set => EditorPrefs.SetInt(FacePreference, (int)value);
        }

        internal static int RotationSteps
        {
            get => ((EditorPrefs.GetInt(RotationPreference, 0) % 4) + 4) % 4;
            set => EditorPrefs.SetInt(RotationPreference, ((value % 4) + 4) % 4);
        }

        internal static bool SnapToGrid
        {
            get => EditorPrefs.GetBool(SnapPreference, true);
            set => EditorPrefs.SetBool(SnapPreference, value);
        }

        internal static bool AllowOverlap
        {
            get => EditorPrefs.GetBool(OverlapPreference, false);
            set => EditorPrefs.SetBool(OverlapPreference, value);
        }

        internal static GridMapItemDefinition CurrentItem
        {
            get
            {
                string guid = EditorPrefs.GetString(ItemGuidPreference, string.Empty);
                if (string.IsNullOrWhiteSpace(guid))
                {
                    return null;
                }

                string path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<GridMapItemDefinition>(path);
            }
            set
            {
                string guid = value == null
                    ? string.Empty
                    : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                EditorPrefs.SetString(ItemGuidPreference, guid ?? string.Empty);
            }
        }

        internal static void Rotate(int direction = 1)
        {
            RotationSteps += direction;
            SceneView.RepaintAll();
        }
    }
}
