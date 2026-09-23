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
    internal static class LevelAuthoringComposer
    {
        private const string RopeMaterialPath =
            "Assets/_Project/Content/Materials/RopePaths/Rope_White.mat";

        internal static Scene BuildPreview(
            LevelAuthoringDefinition definition,
            LevelViewMode viewMode,
            bool openAfterBuild)
        {
            if (viewMode == LevelViewMode.Piece2D)
            {
                throw new System.ArgumentException(
                    "Piece2D 预览需要指定 PieceIndex。",
                    nameof(viewMode));
            }

            string path = viewMode == LevelViewMode.Total2D
                ? definition.Preview2DScenePath
                : definition.Preview3DScenePath;
            return Build(definition, viewMode, path, true, openAfterBuild, 0);
        }

        internal static Scene BuildPiecePreview(
            LevelAuthoringDefinition definition,
            int pieceIndex,
            bool openAfterBuild)
        {
            int clampedPiece = Mathf.Clamp(
                pieceIndex,
                1,
                definition.PieceCount);
            return Build(
                definition,
                LevelViewMode.Piece2D,
                LevelProjectPaths.GetPiecePreviewScenePath(
                    definition.LevelId,
                    clampedPiece),
                true,
                openAfterBuild,
                clampedPiece);
        }

        internal static Scene RebuildPreview(
            LevelAuthoringDefinition definition,
            LevelViewMode viewMode,
            int pieceIndex,
            bool openAfterBuild)
        {
            return viewMode == LevelViewMode.Piece2D
                ? BuildPiecePreview(definition, pieceIndex, openAfterBuild)
                : BuildPreview(definition, viewMode, openAfterBuild);
        }

        internal static Scene PublishRelease(LevelAuthoringDefinition definition)
        {
            return Build(
                definition,
                LevelViewMode.Release3D,
                definition.ReleaseMapScenePath,
                false,
                false,
                0);
        }

        private static Scene Build(
            LevelAuthoringDefinition definition,
            LevelViewMode viewMode,
            string scenePath,
            bool editablePreview,
            bool openAfterBuild,
            int pieceFilter)
        {
            if (definition == null || definition.Workspace == null)
            {
                throw new System.InvalidOperationException("关卡管线定义或工作区为空。");
            }

            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string workspacePath = AssetDatabase.GetAssetPath(definition.Workspace);
            string levelId = definition.LevelId;
            Scene previousActive = SceneManager.GetActiveScene();
            bool replaceCleanUntitledScene =
                previousActive.IsValid() &&
                string.IsNullOrWhiteSpace(previousActive.path) &&
                !previousActive.isDirty;
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                replaceCleanUntitledScene
                    ? NewSceneMode.Single
                    : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            try
            {
                definition = AssetDatabase.LoadAssetAtPath<LevelAuthoringDefinition>(
                    definitionPath);
                CubeMapWorkspaceDefinition workspace =
                    AssetDatabase.LoadAssetAtPath<CubeMapWorkspaceDefinition>(workspacePath);
                if (definition == null || workspace == null)
                {
                    throw new System.InvalidOperationException(
                        "创建预览场景后无法重新载入关卡管线资产。");
                }

                List<LevelAuthoringChunk> chunks =
                    LevelAuthoringRepository.LoadChunks(definition);
                if (pieceFilter > 0)
                {
                    chunks.RemoveAll(chunk => chunk.PieceIndex != pieceFilter);
                }

                string sourceHash =
                    LevelAuthoringRepository.ComputeSourceHash(chunks);
                GameObject root = new GameObject("[GENERATED] Level Content");
                SceneManager.MoveGameObjectToScene(root, scene);
                LevelGeneratedSceneInfo info = root.AddComponent<LevelGeneratedSceneInfo>();
                info.Configure(levelId, viewMode, sourceHash, pieceFilter);

                GameObject geometryRoot = CreateChild("Geometry", root.transform);
                GameObject traversalRoot = CreateChild("Traversal", root.transform);
                Dictionary<CubeMapFace, Transform> pieceContentRoots =
                    viewMode == LevelViewMode.Piece2D
                        ? CreatePieceAuthoring(
                            root.transform,
                            workspace,
                            pieceFilter)
                        : null;
                RopePathNetwork ropeNetwork =
                    CreateChild("RopePathNetwork", traversalRoot.transform)
                        .AddComponent<RopePathNetwork>();
                LadderPathNetwork ladderNetwork =
                    CreateChild("LadderPathNetwork", traversalRoot.transform)
                        .AddComponent<LadderPathNetwork>();
                Material whiteMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(RopeMaterialPath);
                Dictionary<string, RopeSegment> ropesById = new();
                List<PendingPlatform> pendingPlatforms = new();

                for (int index = 0; index < chunks.Count; index++)
                {
                    LevelAuthoringChunk chunk = chunks[index];
                    LevelSectionFrame frame = LevelCoordinateUtility.GetFrame(
                        workspace,
                        chunk.PieceIndex,
                        chunk.Face,
                        viewMode);
                    BuildMapItems(
                        chunk,
                        frame,
                        viewMode,
                        scene,
                        pieceContentRoots != null &&
                        pieceContentRoots.TryGetValue(
                            chunk.Face,
                            out Transform contentRoot)
                            ? contentRoot
                            : geometryRoot.transform,
                        editablePreview);
                    BuildRopes(
                        chunk,
                        frame,
                        viewMode,
                        ropeNetwork,
                        whiteMaterial,
                        editablePreview,
                        ropesById);
                    BuildLadders(
                        chunk,
                        frame,
                        viewMode,
                        ladderNetwork,
                        whiteMaterial,
                        editablePreview);
                    QueuePlatforms(
                        chunk,
                        frame,
                        viewMode,
                        scene,
                        traversalRoot.transform,
                        editablePreview,
                        pendingPlatforms);
                }

                for (int index = 0; index < pendingPlatforms.Count; index++)
                {
                    PendingPlatform pending = pendingPlatforms[index];
                    if (!string.IsNullOrWhiteSpace(pending.Record.BoundRopeId) &&
                        ropesById.TryGetValue(
                            pending.Record.BoundRopeId,
                            out RopeSegment segment))
                    {
                        pending.Platform.Bind(
                            ropeNetwork,
                            segment,
                            pending.Record.BoundEndpoint);
                    }
                }

                ropeNetwork.RebuildPaths();
                ladderNetwork.RefreshSegments();
                LevelProjectStructureService.EnsureFolder(
                    System.IO.Path.GetDirectoryName(scenePath)?.Replace('\\', '/'));
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            finally
            {
                if (!openAfterBuild && scene.isLoaded)
                {
                    if (SceneManager.sceneCount == 1)
                    {
                        EditorSceneManager.NewScene(
                            NewSceneSetup.EmptyScene,
                            NewSceneMode.Single);
                    }
                    else
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }

                if (previousActive.IsValid() && previousActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActive);
                }
            }

            if (openAfterBuild)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                return SceneManager.GetActiveScene();
            }

            AssetDatabase.Refresh();
            return default;
        }

        private static void BuildMapItems(
            LevelAuthoringChunk chunk,
            LevelSectionFrame frame,
            LevelViewMode viewMode,
            Scene scene,
            Transform parent,
            bool editablePreview)
        {
            for (int index = 0; index < chunk.MapItems.Count; index++)
            {
                LevelMapItemRecord record = chunk.MapItems[index];
                if (record == null || record.Prefab == null)
                {
                    continue;
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(record.Prefab, scene)
                    as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = record.DisplayName;
                instance.transform.SetParent(parent, true);
                ApplyPose(instance.transform, record.Pose, frame);
                if (record.GridDefinition != null)
                {
                    GridMapPlacement placement =
                        instance.GetComponent<GridMapPlacement>() ??
                        instance.AddComponent<GridMapPlacement>();
                    placement.Configure(
                        record.GridDefinition,
                        record.AnchorCell,
                        record.RotationSteps,
                        record.SnappedToGrid,
                        record.AllowOverlap,
                        record.UnsnappedLocalPosition);
                }

                AddMarkers(instance, chunk, record, viewMode, editablePreview);
            }
        }

        private static void BuildRopes(
            LevelAuthoringChunk chunk,
            LevelSectionFrame frame,
            LevelViewMode viewMode,
            RopePathNetwork network,
            Material material,
            bool editablePreview,
            Dictionary<string, RopeSegment> ropesById)
        {
            for (int index = 0; index < chunk.Ropes.Count; index++)
            {
                LevelRopeRecord record = chunk.Ropes[index];
                if (record == null)
                {
                    continue;
                }

                GameObject gameObject = new GameObject(record.DisplayName);
                gameObject.transform.SetParent(network.transform, false);
                ApplyPose(gameObject.transform, record.Pose, frame);
                RopeSegment segment = gameObject.AddComponent<RopeSegment>();
                segment.Configure(
                    record.EndpointA,
                    record.EndpointB,
                    record.RopeWidth,
                    record.EndpointRadius);
                segment.SetSegmentId(record.EntityId);
                segment.EnsureVisuals(material);
                AddMarkers(gameObject, chunk, record, viewMode, editablePreview);
                if (!ropesById.ContainsKey(record.EntityId))
                {
                    ropesById.Add(record.EntityId, segment);
                }
            }
        }

        private static void BuildLadders(
            LevelAuthoringChunk chunk,
            LevelSectionFrame frame,
            LevelViewMode viewMode,
            LadderPathNetwork network,
            Material material,
            bool editablePreview)
        {
            for (int index = 0; index < chunk.Ladders.Count; index++)
            {
                LevelLadderRecord record = chunk.Ladders[index];
                if (record == null)
                {
                    continue;
                }

                GameObject gameObject = new GameObject(record.DisplayName);
                gameObject.transform.SetParent(network.transform, false);
                ApplyPose(gameObject.transform, record.Pose, frame);
                LadderSegment segment = gameObject.AddComponent<LadderSegment>();
                segment.Configure(
                    record.Width,
                    record.Height,
                    record.Depth,
                    record.RungCount);
                segment.SetSegmentId(record.EntityId);
                segment.EnsureVisuals(material);
                AddMarkers(gameObject, chunk, record, viewMode, editablePreview);
            }
        }

        private static void QueuePlatforms(
            LevelAuthoringChunk chunk,
            LevelSectionFrame frame,
            LevelViewMode viewMode,
            Scene scene,
            Transform parent,
            bool editablePreview,
            List<PendingPlatform> pendingPlatforms)
        {
            for (int index = 0; index < chunk.Platforms.Count; index++)
            {
                LevelPlatformRecord record = chunk.Platforms[index];
                if (record == null)
                {
                    continue;
                }

                GameObject gameObject = record.Prefab != null
                    ? PrefabUtility.InstantiatePrefab(record.Prefab, scene) as GameObject
                    : GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (gameObject == null)
                {
                    continue;
                }

                SceneManager.MoveGameObjectToScene(gameObject, scene);
                gameObject.name = record.DisplayName;
                gameObject.transform.SetParent(parent, true);
                ApplyPose(gameObject.transform, record.Pose, frame);
                RopePlatform platform = gameObject.GetComponent<RopePlatform>() ??
                                        gameObject.AddComponent<RopePlatform>();
                AddMarkers(gameObject, chunk, record, viewMode, editablePreview);
                pendingPlatforms.Add(new PendingPlatform(record, platform));
            }
        }

        private static void AddMarkers(
            GameObject gameObject,
            LevelAuthoringChunk chunk,
            LevelEntityRecord record,
            LevelViewMode viewMode,
            bool editablePreview)
        {
            LevelGeneratedEntity marker = gameObject.AddComponent<LevelGeneratedEntity>();
            marker.Configure(
                record.EntityId,
                record.Kind,
                chunk.LevelId,
                chunk.PieceIndex,
                chunk.Face);
            if (editablePreview)
            {
                LevelAuthoringProxy proxy = gameObject.AddComponent<LevelAuthoringProxy>();
                proxy.Configure(chunk, record.EntityId, viewMode);
            }
        }

        private static void ApplyPose(
            Transform target,
            LevelPose source,
            LevelSectionFrame frame)
        {
            LevelPose pose = LevelCoordinateUtility.ToViewPose(source, frame);
            target.position = pose.LocalPosition;
            target.rotation = pose.LocalRotation;
            target.localScale = pose.LocalScale;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Dictionary<CubeMapFace, Transform> CreatePieceAuthoring(
            Transform parent,
            CubeMapWorkspaceDefinition workspace,
            int pieceIndex)
        {
            GameObject pieceRoot = CreateChild(
                $"{workspace.LevelId}_Piece_{pieceIndex:00}_Authoring",
                parent);
            CubeMapPieceAuthoring authoring =
                pieceRoot.AddComponent<CubeMapPieceAuthoring>();
            Transform[] faceRoots = new Transform[CubeMapLayoutMath.FaceCount];
            Dictionary<CubeMapFace, Transform> contentRoots = new();
            for (int faceIndex = 0;
                 faceIndex < CubeMapLayoutMath.FaceCount;
                 faceIndex++)
            {
                CubeMapFace face = (CubeMapFace)faceIndex;
                GameObject faceRoot = CreateChild(
                    $"Face_{faceIndex + 1:00}_{face}_" +
                    CubeMapLayoutMath.GetFaceLabel(face),
                    pieceRoot.transform);
                faceRoot.transform.localPosition =
                    CubeMapLayoutMath.GetPieceFaceCenter(face, workspace.FaceWidth);
                GameObject content = CreateChild("Content", faceRoot.transform);
                faceRoots[faceIndex] = faceRoot.transform;
                contentRoots.Add(face, content.transform);
            }

            authoring.Configure(workspace, pieceIndex, faceRoots);
            return contentRoots;
        }

        private readonly struct PendingPlatform
        {
            internal PendingPlatform(LevelPlatformRecord record, RopePlatform platform)
            {
                Record = record;
                Platform = platform;
            }

            internal LevelPlatformRecord Record { get; }
            internal RopePlatform Platform { get; }
        }
    }
}
