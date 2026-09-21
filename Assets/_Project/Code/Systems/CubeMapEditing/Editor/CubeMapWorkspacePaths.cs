namespace Project.CubeMapEditing.Editor
{
    internal static class CubeMapWorkspacePaths
    {
        internal const string LevelId = "LV001";
        internal const string LevelSceneFolder =
            "Assets/_Project/Scenes/Levels/LV001";
        internal const string PieceSceneFolder =
            "Assets/_Project/Scenes/Levels/LV001/Pieces";
        internal const string DataFolder =
            "Assets/_Project/Content/Data/Levels/LV001";
        internal const string WorkspaceAssetPath =
            DataFolder + "/LV001_CubeMapWorkspace.asset";
        internal const string Total2DScenePath =
            LevelSceneFolder + "/LV001_Map2D.unity";
        internal const string Main3DScenePath =
            LevelSceneFolder + "/LV001_Main3D.unity";

        internal static string GetPieceScenePath(int pieceIndex)
        {
            return $"{PieceSceneFolder}/LV001_Piece_{pieceIndex:00}.unity";
        }
    }
}
