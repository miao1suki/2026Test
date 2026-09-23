using Project.CubeMapEditing;
using Project.LadderPaths;
using Project.PlatformPaths;
using Project.RopePaths;
using UnityEditor;
using UnityEngine;

namespace Project.LevelAuthoring.Editor
{
    internal static class LevelAuthoringAdoptionService
    {
        internal static bool AdoptSelected(LevelAuthoringChunk chunk)
        {
            GameObject selected = Selection.activeGameObject;
            if (chunk == null || selected == null)
            {
                return false;
            }

            Transform sourceTransform;
            LevelEntityRecord record;
            if (selected.GetComponentInParent<RopeSegment>() is RopeSegment rope)
            {
                sourceTransform = rope.transform;
                LevelRopeRecord ropeRecord = new();
                ropeRecord.Initialize(
                    string.IsNullOrWhiteSpace(rope.SegmentId)
                        ? System.Guid.NewGuid().ToString("N")
                        : rope.SegmentId,
                    rope.gameObject.name,
                    CapturePose(chunk, rope.transform));
                ropeRecord.Configure(
                    rope.LocalEndpointA,
                    rope.LocalEndpointB,
                    rope.RopeWidth,
                    rope.EndpointRadius);
                record = ropeRecord;
            }
            else if (selected.GetComponentInParent<LadderSegment>() is LadderSegment ladder)
            {
                sourceTransform = ladder.transform;
                LevelLadderRecord ladderRecord = new();
                ladderRecord.Initialize(
                    string.IsNullOrWhiteSpace(ladder.SegmentId)
                        ? System.Guid.NewGuid().ToString("N")
                        : ladder.SegmentId,
                    ladder.gameObject.name,
                    CapturePose(chunk, ladder.transform));
                ladderRecord.Configure(
                    ladder.Width,
                    ladder.Height,
                    ladder.Depth,
                    ladder.RungCount);
                record = ladderRecord;
            }
            else if (selected.GetComponentInParent<RopePlatform>() is RopePlatform platform)
            {
                sourceTransform = platform.transform;
                LevelPlatformRecord platformRecord = new();
                platformRecord.Initialize(
                    System.Guid.NewGuid().ToString("N"),
                    platform.gameObject.name,
                    CapturePose(chunk, platform.transform));
                LevelAuthoringProxy ropeProxy = platform.BoundSegment != null
                    ? platform.BoundSegment.GetComponent<LevelAuthoringProxy>()
                    : null;
                platformRecord.Configure(
                    PrefabUtility.GetCorrespondingObjectFromSource(platform.gameObject),
                    ropeProxy != null
                        ? ropeProxy.EntityId
                        : platform.BoundSegment != null
                            ? platform.BoundSegment.SegmentId
                            : string.Empty,
                    platform.BoundEndpoint);
                record = platformRecord;
            }
            else if (selected.GetComponentInParent<GridMapPlacement>() is GridMapPlacement placement)
            {
                sourceTransform = placement.transform;
                LevelMapItemRecord mapRecord = new();
                mapRecord.Initialize(
                    System.Guid.NewGuid().ToString("N"),
                    placement.gameObject.name,
                    CapturePose(chunk, placement.transform));
                mapRecord.Configure(
                    placement.Definition != null
                        ? placement.Definition.Prefab
                        : PrefabUtility.GetCorrespondingObjectFromSource(placement.gameObject),
                    placement.Definition);
                mapRecord.ConfigureGrid(
                    placement.AnchorCell,
                    placement.RotationSteps,
                    placement.SnappedToGrid,
                    placement.AllowOverlap,
                    placement.UnsnappedLocalPosition);
                record = mapRecord;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "无法收编",
                    "选中对象需要包含 RopeSegment、LadderSegment、RopePlatform 或 GridMapPlacement。",
                    "确定");
                return false;
            }

            LevelContentKind expectedKind = record is LevelMapItemRecord
                ? LevelContentKind.Geometry
                : LevelContentKind.Traversal;
            if (chunk.ContentKind != expectedKind)
            {
                EditorUtility.DisplayDialog(
                    "数据块类型不匹配",
                    $"{record.DisplayName} 应收编到 {expectedKind}.asset，" +
                    $"当前选择的是 {chunk.ContentKind}.asset。",
                    "确定");
                return false;
            }

            if (!ValidatePreviewTarget(chunk, sourceTransform))
            {
                return false;
            }

            Undo.RecordObject(chunk, "Adopt object into level authoring");
            switch (record)
            {
                case LevelRopeRecord value:
                    chunk.Add(value);
                    break;
                case LevelLadderRecord value:
                    chunk.Add(value);
                    break;
                case LevelPlatformRecord value:
                    chunk.Add(value);
                    break;
                case LevelMapItemRecord value:
                    chunk.Add(value);
                    break;
            }

            EditorUtility.SetDirty(chunk);
            Undo.DestroyObjectImmediate(sourceTransform.gameObject);
            AssetDatabase.SaveAssets();
            Selection.activeObject = chunk;
            return true;
        }

        private static LevelPose CapturePose(
            LevelAuthoringChunk chunk,
            Transform target)
        {
            LevelGeneratedSceneInfo info =
                Object.FindFirstObjectByType<LevelGeneratedSceneInfo>();
            LevelViewMode mode = info != null
                ? info.ViewMode
                : LevelViewMode.Piece2D;
            LevelAuthoringDefinition definition =
                AssetDatabase.LoadAssetAtPath<LevelAuthoringDefinition>(
                    LevelProjectPaths.GetDefinitionPath(chunk.LevelId));
            if (definition == null || definition.Workspace == null)
            {
                return LevelPose.Identity;
            }

            LevelSectionFrame frame = LevelCoordinateUtility.GetFrame(
                definition.Workspace,
                chunk.PieceIndex,
                chunk.Face,
                mode);
            return LevelCoordinateUtility.FromViewPose(target, frame);
        }

        private static bool ValidatePreviewTarget(
            LevelAuthoringChunk chunk,
            Transform source)
        {
            LevelGeneratedSceneInfo info =
                Object.FindFirstObjectByType<LevelGeneratedSceneInfo>();
            if (info == null || info.ViewMode != LevelViewMode.Piece2D)
            {
                return true;
            }

            if (info.PieceIndex != chunk.PieceIndex)
            {
                EditorUtility.DisplayDialog(
                    "Piece 不匹配",
                    $"当前预览是 Piece {info.PieceIndex:00}，" +
                    $"但数据块属于 Piece {chunk.PieceIndex:00}。",
                    "确定");
                return false;
            }

            CubeMapPieceAuthoring piece =
                Object.FindFirstObjectByType<CubeMapPieceAuthoring>();
            if (piece == null)
            {
                return true;
            }

            for (int index = 0; index < CubeMapLayoutMath.FaceCount; index++)
            {
                CubeMapFace face = (CubeMapFace)index;
                Transform faceRoot = piece.GetFaceRoot(face);
                if (faceRoot != null && source.IsChildOf(faceRoot) && face != chunk.Face)
                {
                    EditorUtility.DisplayDialog(
                        "Face 不匹配",
                        $"选中对象位于 {face}，但数据块属于 {chunk.Face}。",
                        "确定");
                    return false;
                }
            }

            return true;
        }
    }
}
