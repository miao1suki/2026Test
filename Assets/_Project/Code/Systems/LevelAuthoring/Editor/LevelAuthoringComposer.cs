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
            string path = viewMode == LevelViewMode.Total2D
                ? definition.Preview2DScenePath
                : definition.Preview3DScenePath;
            return Build(definition, viewMode, path, true, openAfterBuild);
        }

        internal static Scene PublishRelease(LevelAuthoringDefinition definition)
        {
            return Build(
                definition,
                LevelViewMode.Release3D,
                definition.ReleaseMapScenePath,
                false,
                false);
        }

        private static Scene Build(
            LevelAuthoringDefinition definition,
            LevelViewMode viewMode,
            string scenePath,
            bool editablePreview,
            bool openAfterBuild)
        {
            if (definition == null || definition.Workspace == null)
            {
                throw new System.InvalidOperationException("关卡管线定义或工作区为空。");
            }

            List<LevelAuthoringChunk> chunks =
                LevelAuthoringRepository.LoadChunks(definition);
            string sourceHash = LevelAuthoringRepository.ComputeSourceHash(chunks);
            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            try
            {
                GameObject root = new GameObject("[GENERATED] Level Content");
                SceneManager.MoveGameObjectToScene(root, scene);
                LevelGeneratedSceneInfo info = root.AddComponent<LevelGeneratedSceneInfo>();
                info.Configure(definition.LevelId, viewMode, sourceHash);

                GameObject geometryRoot = CreateChild("Geometry", root.transform);
                GameObject traversalRoot = CreateChild("Traversal", root.transform);
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
                        definition.Workspace,
                        chunk.PieceIndex,
                        chunk.Face,
                        viewMode);
                    BuildMapItems(
                        chunk,
                        frame,
                        viewMode,
                        scene,
                        geometryRoot.transform,
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
                    EditorSceneManager.CloseScene(scene, true);
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
