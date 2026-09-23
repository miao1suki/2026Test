using System;
using System.Collections.Generic;
using Project.CubeMapEditing;
using Project.LadderPaths;
using Project.PlatformPaths;
using Project.RopePaths;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.LevelAuthoring.Editor
{
    internal static class LegacyLevelImportService
    {
        internal static void ImportAll(LevelAuthoringDefinition definition)
        {
            if (definition == null || definition.Workspace == null)
            {
                throw new InvalidOperationException("关卡管线定义或工作区为空。");
            }

            List<LevelAuthoringChunk> chunks =
                LevelAuthoringRepository.LoadChunks(definition);
            for (int index = 0; index < chunks.Count; index++)
            {
                Undo.RecordObject(chunks[index], "Import legacy level authoring");
                chunks[index].Clear();
                EditorUtility.SetDirty(chunks[index]);
            }

            for (int index = 0; index < definition.Workspace.PieceScenePaths.Count; index++)
            {
                string path = definition.Workspace.PieceScenePaths[index];
                Scene scene = SceneManager.GetSceneByPath(path);
                bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
                if (!alreadyLoaded)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }

                try
                {
                    ImportPieceScene(definition, scene);
                }
                finally
                {
                    if (!alreadyLoaded)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(definition.Workspace.Main3DScenePath) &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    definition.Workspace.Main3DScenePath) != null)
            {
                Scene mainScene = SceneManager.GetSceneByPath(
                    definition.Workspace.Main3DScenePath);
                bool alreadyLoaded = mainScene.IsValid() && mainScene.isLoaded;
                if (!alreadyLoaded)
                {
                    mainScene = EditorSceneManager.OpenScene(
                        definition.Workspace.Main3DScenePath,
                        OpenSceneMode.Additive);
                }
                try
                {
                    ImportFoldedSceneSpecialObjects(definition, mainScene);
                }
                finally
                {
                    if (!alreadyLoaded)
                    {
                        EditorSceneManager.CloseScene(mainScene, true);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ImportPieceScene(
            LevelAuthoringDefinition definition,
            Scene scene)
        {
            CubeMapPieceAuthoring piece = FindInScene<CubeMapPieceAuthoring>(scene);
            if (piece == null)
            {
                return;
            }

            for (int faceIndex = 0; faceIndex < CubeMapLayoutMath.FaceCount; faceIndex++)
            {
                CubeMapFace face = (CubeMapFace)faceIndex;
                Transform faceRoot = piece.GetFaceRoot(face);
                if (faceRoot == null)
                {
                    continue;
                }

                LevelAuthoringChunk geometry = LevelAuthoringRepository.FindChunk(
                    definition,
                    piece.PieceIndex,
                    face,
                    LevelContentKind.Geometry);
                Transform content = faceRoot.Find("Content");
                if (geometry != null && content != null)
                {
                    GridMapPlacement[] placements =
                        content.GetComponentsInChildren<GridMapPlacement>(true);
                    for (int index = 0; index < placements.Length; index++)
                    {
                        GridMapPlacement placement = placements[index];
                        GameObject prefab = placement.Definition != null
                            ? placement.Definition.Prefab
                            : ResolvePrefab(placement.gameObject);
                        LevelMapItemRecord record = new();
                        record.Initialize(
                            GetStableSceneId(placement.gameObject),
                            placement.gameObject.name,
                            CapturePose(placement.transform, faceRoot));
                        record.Configure(prefab, placement.Definition);
                        geometry.Add(record);
                        EditorUtility.SetDirty(geometry);
                    }
                }
            }

            RopeSegment[] ropes = FindAllInScene<RopeSegment>(scene);
            for (int index = 0; index < ropes.Length; index++)
            {
                RopeSegment segment = ropes[index];
                CubeMapFace face = FindNearestPieceFace(piece, segment.transform.position);
                LevelAuthoringChunk chunk = GetTraversalChunk(
                    definition,
                    piece.PieceIndex,
                    face);
                if (chunk == null) continue;
                string id = string.IsNullOrWhiteSpace(segment.SegmentId)
                    ? GetStableSceneId(segment.gameObject)
                    : segment.SegmentId;
                LevelRopeRecord record = new();
                record.Initialize(
                    id,
                    segment.gameObject.name,
                    CapturePose(segment.transform, piece.GetFaceRoot(face)));
                record.Configure(
                    segment.LocalEndpointA,
                    segment.LocalEndpointB,
                    segment.RopeWidth,
                    segment.EndpointRadius);
                chunk.Add(record);
                EditorUtility.SetDirty(chunk);
            }

            LadderSegment[] ladders = FindAllInScene<LadderSegment>(scene);
            for (int index = 0; index < ladders.Length; index++)
            {
                LadderSegment segment = ladders[index];
                CubeMapFace face = FindNearestPieceFace(piece, segment.transform.position);
                LevelAuthoringChunk chunk = GetTraversalChunk(
                    definition,
                    piece.PieceIndex,
                    face);
                if (chunk == null) continue;
                LevelLadderRecord record = new();
                record.Initialize(
                    string.IsNullOrWhiteSpace(segment.SegmentId)
                        ? GetStableSceneId(segment.gameObject)
                        : segment.SegmentId,
                    segment.gameObject.name,
                    CapturePose(segment.transform, piece.GetFaceRoot(face)));
                record.Configure(
                    segment.Width,
                    segment.Height,
                    segment.Depth,
                    segment.RungCount);
                chunk.Add(record);
                EditorUtility.SetDirty(chunk);
            }

            RopePlatform[] platforms = FindAllInScene<RopePlatform>(scene);
            for (int index = 0; index < platforms.Length; index++)
            {
                RopePlatform platform = platforms[index];
                CubeMapFace face = FindNearestPieceFace(piece, platform.transform.position);
                LevelAuthoringChunk chunk = GetTraversalChunk(
                    definition,
                    piece.PieceIndex,
                    face);
                if (chunk == null) continue;
                LevelPlatformRecord record = new();
                record.Initialize(
                    GetStableSceneId(platform.gameObject),
                    platform.gameObject.name,
                    CapturePose(platform.transform, piece.GetFaceRoot(face)));
                string ropeId = platform.BoundSegment != null
                    ? platform.BoundSegment.SegmentId
                    : string.Empty;
                record.Configure(
                    ResolvePrefab(platform.gameObject),
                    ropeId,
                    platform.BoundEndpoint);
                chunk.Add(record);
                EditorUtility.SetDirty(chunk);
            }
        }

        private static void ImportFoldedSceneSpecialObjects(
            LevelAuthoringDefinition definition,
            Scene scene)
        {
            ImportFoldedRopes(definition, scene);
            ImportFoldedLadders(definition, scene);
            ImportFoldedPlatforms(definition, scene);
        }

        private static void ImportFoldedRopes(
            LevelAuthoringDefinition definition,
            Scene scene)
        {
            RopeSegment[] segments = FindAllInScene<RopeSegment>(scene);
            for (int index = 0; index < segments.Length; index++)
            {
                RopeSegment segment = segments[index];
                if (IsGenerated(segment.transform)) continue;
                ResolveFoldedSection(
                    definition.Workspace,
                    segment.transform.position,
                    out int piece,
                    out CubeMapFace face,
                    out LevelSectionFrame frame);
                LevelAuthoringChunk chunk = GetTraversalChunk(definition, piece, face);
                if (chunk == null) continue;
                LevelRopeRecord record = new();
                record.Initialize(
                    string.IsNullOrWhiteSpace(segment.SegmentId)
                        ? GetStableSceneId(segment.gameObject)
                        : segment.SegmentId,
                    segment.gameObject.name,
                    LevelCoordinateUtility.FromViewPose(segment.transform, frame));
                record.Configure(
                    segment.LocalEndpointA,
                    segment.LocalEndpointB,
                    segment.RopeWidth,
                    segment.EndpointRadius);
                chunk.Add(record);
                EditorUtility.SetDirty(chunk);
            }
        }

        private static void ImportFoldedLadders(
            LevelAuthoringDefinition definition,
            Scene scene)
        {
            LadderSegment[] segments = FindAllInScene<LadderSegment>(scene);
            for (int index = 0; index < segments.Length; index++)
            {
                LadderSegment segment = segments[index];
                if (IsGenerated(segment.transform)) continue;
                ResolveFoldedSection(
                    definition.Workspace,
                    segment.transform.position,
                    out int piece,
                    out CubeMapFace face,
                    out LevelSectionFrame frame);
                LevelAuthoringChunk chunk = GetTraversalChunk(definition, piece, face);
                if (chunk == null) continue;
                LevelLadderRecord record = new();
                record.Initialize(
                    string.IsNullOrWhiteSpace(segment.SegmentId)
                        ? GetStableSceneId(segment.gameObject)
                        : segment.SegmentId,
                    segment.gameObject.name,
                    LevelCoordinateUtility.FromViewPose(segment.transform, frame));
                record.Configure(
                    segment.Width,
                    segment.Height,
                    segment.Depth,
                    segment.RungCount);
                chunk.Add(record);
                EditorUtility.SetDirty(chunk);
            }
        }

        private static void ImportFoldedPlatforms(
            LevelAuthoringDefinition definition,
            Scene scene)
        {
            RopePlatform[] platforms = FindAllInScene<RopePlatform>(scene);
            for (int index = 0; index < platforms.Length; index++)
            {
                RopePlatform platform = platforms[index];
                if (IsGenerated(platform.transform)) continue;
                ResolveFoldedSection(
                    definition.Workspace,
                    platform.transform.position,
                    out int piece,
                    out CubeMapFace face,
                    out LevelSectionFrame frame);
                LevelAuthoringChunk chunk = GetTraversalChunk(definition, piece, face);
                if (chunk == null) continue;
                LevelPlatformRecord record = new();
                record.Initialize(
                    GetStableSceneId(platform.gameObject),
                    platform.gameObject.name,
                    LevelCoordinateUtility.FromViewPose(platform.transform, frame));
                record.Configure(
                    ResolvePrefab(platform.gameObject),
                    platform.BoundSegment != null
                        ? platform.BoundSegment.SegmentId
                        : string.Empty,
                    platform.BoundEndpoint);
                chunk.Add(record);
                EditorUtility.SetDirty(chunk);
            }
        }

        private static LevelAuthoringChunk GetTraversalChunk(
            LevelAuthoringDefinition definition,
            int piece,
            CubeMapFace face)
        {
            return LevelAuthoringRepository.FindChunk(
                definition,
                piece,
                face,
                LevelContentKind.Traversal);
        }

        private static LevelPose CapturePose(Transform target, Transform frame)
        {
            return new LevelPose(
                frame.InverseTransformPoint(target.position),
                Quaternion.Inverse(frame.rotation) * target.rotation,
                target.localScale);
        }

        private static CubeMapFace FindNearestPieceFace(
            CubeMapPieceAuthoring piece,
            Vector3 worldPosition)
        {
            CubeMapFace bestFace = CubeMapFace.Front;
            float bestScore = float.MaxValue;
            for (int faceIndex = 0; faceIndex < CubeMapLayoutMath.FaceCount; faceIndex++)
            {
                CubeMapFace face = (CubeMapFace)faceIndex;
                Transform root = piece.GetFaceRoot(face);
                if (root == null) continue;
                Vector3 local = root.InverseTransformPoint(worldPosition);
                float halfWidth = piece.Workspace != null
                    ? piece.Workspace.FaceWidth * 0.5f
                    : 6f;
                float score = Mathf.Abs(local.z) +
                              Mathf.Max(0f, Mathf.Abs(local.x) - halfWidth);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestFace = face;
                }
            }

            return bestFace;
        }

        private static void ResolveFoldedSection(
            CubeMapWorkspaceDefinition workspace,
            Vector3 worldPosition,
            out int pieceIndex,
            out CubeMapFace face,
            out LevelSectionFrame frame)
        {
            if (Mathf.Abs(worldPosition.x) > Mathf.Abs(worldPosition.z))
            {
                face = worldPosition.x >= 0f ? CubeMapFace.Right : CubeMapFace.Left;
            }
            else
            {
                face = worldPosition.z >= 0f ? CubeMapFace.Back : CubeMapFace.Front;
            }

            pieceIndex = Mathf.Clamp(
                Mathf.FloorToInt(worldPosition.y / workspace.PieceHeight) + 1,
                1,
                Mathf.Max(1, workspace.PieceCount));
            frame = LevelCoordinateUtility.GetFrame(
                workspace,
                pieceIndex,
                face,
                LevelViewMode.Folded3D);
        }

        private static bool IsGenerated(Transform target)
        {
            return target.GetComponentInParent<CubeMapGeneratedLayout>() != null ||
                   target.GetComponentInParent<LevelGeneratedSceneInfo>() != null;
        }

        private static GameObject ResolvePrefab(GameObject gameObject)
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
        }

        private static string GetStableSceneId(GameObject gameObject)
        {
            GlobalObjectId global = GlobalObjectId.GetGlobalObjectIdSlow(gameObject);
            return Hash128.Compute(global.ToString()).ToString();
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            T[] values = FindAllInScene<T>(scene);
            return values.Length > 0 ? values[0] : null;
        }

        private static T[] FindAllInScene<T>(Scene scene) where T : Component
        {
            List<T> values = new();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                values.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return values.ToArray();
        }
    }
}
