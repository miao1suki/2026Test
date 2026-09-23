using Project.CubeMapEditing;

namespace Project.LevelAuthoring.Editor
{
    internal static class LevelProjectPaths
    {
        internal const string DevelopmentRoot = "Assets/_Project/Development";
        internal const string ReleaseRoot = "Assets/_Project/Release";
        internal const string LegacyWorkspacePath =
            "Assets/_Project/Content/Data/Levels/LV001/LV001_CubeMapWorkspace.asset";

        internal static string GetDevelopmentLevelRoot(string levelId) =>
            $"{DevelopmentRoot}/Levels/{levelId}";

        internal static string GetAuthoringRoot(string levelId) =>
            $"{GetDevelopmentLevelRoot(levelId)}/Authoring";

        internal static string GetDefinitionPath(string levelId) =>
            $"{GetAuthoringRoot(levelId)}/{levelId}_AuthoringDefinition.asset";

        internal static string GetChunkFolder(
            string levelId,
            int pieceIndex,
            CubeMapFace face) =>
            $"{GetAuthoringRoot(levelId)}/Pieces/Piece_{pieceIndex:00}/{face}";

        internal static string GetChunkPath(
            string levelId,
            int pieceIndex,
            CubeMapFace face,
            LevelContentKind kind) =>
            $"{GetChunkFolder(levelId, pieceIndex, face)}/{kind}.asset";

        internal static string GetPreviewSceneFolder(string levelId) =>
            $"{GetDevelopmentLevelRoot(levelId)}/Preview/Scenes";

        internal static string GetPreview2DScenePath(string levelId) =>
            $"{GetPreviewSceneFolder(levelId)}/{levelId}_Total2D_Preview.unity";

        internal static string GetPreview3DScenePath(string levelId) =>
            $"{GetPreviewSceneFolder(levelId)}/{levelId}_Folded3D_Preview.unity";

        internal static string GetPiecePreviewScenePath(string levelId, int pieceIndex) =>
            $"{GetPreviewSceneFolder(levelId)}/Pieces/" +
            $"{levelId}_Piece_{pieceIndex:00}_Preview.unity";

        internal static string GetReleaseLevelRoot(string levelId) =>
            $"{ReleaseRoot}/Levels/{levelId}";

        internal static string GetReleaseSceneFolder(string levelId) =>
            $"{GetReleaseLevelRoot(levelId)}/Scenes";

        internal static string GetReleaseMainScenePath(string levelId) =>
            $"{GetReleaseSceneFolder(levelId)}/{levelId}_Main.unity";

        internal static string GetReleaseMapScenePath(string levelId) =>
            $"{GetReleaseSceneFolder(levelId)}/{levelId}_GeneratedMap3D.unity";

        internal static string GetReleaseDataFolder(string levelId) =>
            $"{GetReleaseLevelRoot(levelId)}/Data";
    }
}
