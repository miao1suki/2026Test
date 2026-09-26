namespace Project.SurfaceTiles.Editor
{
    internal enum SurfaceTilePaintMode
    {
        Paint = 0,
        Erase = 1,
        Pick = 2
    }

    internal static class SurfaceTileEditorState
    {
        internal static bool Painting { get; set; }
        internal static SurfaceTilePaintMode Mode { get; set; }
        internal static string SelectedTileId { get; set; }
        internal static int QuarterTurns { get; set; }
        internal static bool FlipX { get; set; }
        internal static bool FlipY { get; set; }
        internal static SurfaceTileFace HoverFace { get; set; }
        internal static UnityEngine.Vector2Int HoverCell { get; set; }
        internal static SurfaceTileBlock HoverBlock { get; set; }
    }
}
