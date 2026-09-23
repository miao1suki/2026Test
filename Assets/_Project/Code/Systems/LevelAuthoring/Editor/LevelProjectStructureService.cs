using System.IO;
using Project.CubeMapEditing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.LevelAuthoring.Editor
{
    public static class LevelProjectStructureService
    {
        internal const string DefaultLevelId = "LV001";

        [MenuItem("Tools/2026Test/关卡创作管线/初始化 LV001 目录与数据块")]
        public static void InitializeLv001()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Initialize(DefaultLevelId, true);
        }

        public static void InitializeLv001FromCommandLine()
        {
            Initialize(DefaultLevelId, false);
        }

        internal static LevelAuthoringDefinition Initialize(
            string levelId,
            bool selectDefinition)
        {
            CubeMapWorkspaceDefinition workspace =
                AssetDatabase.LoadAssetAtPath<CubeMapWorkspaceDefinition>(
                    LevelProjectPaths.LegacyWorkspacePath);
            if (workspace == null)
            {
                throw new FileNotFoundException(
                    "找不到现有立方体地图工作区。",
                    LevelProjectPaths.LegacyWorkspacePath);
            }

            EnsureFolder(LevelProjectPaths.GetAuthoringRoot(levelId));
            EnsureFolder(LevelProjectPaths.GetPreviewSceneFolder(levelId));
            EnsureFolder($"{LevelProjectPaths.GetDevelopmentLevelRoot(levelId)}/Sandbox");
            EnsureFolder(LevelProjectPaths.GetReleaseSceneFolder(levelId));
            EnsureFolder(LevelProjectPaths.GetReleaseDataFolder(levelId));

            string definitionPath = LevelProjectPaths.GetDefinitionPath(levelId);
            LevelAuthoringDefinition definition =
                AssetDatabase.LoadAssetAtPath<LevelAuthoringDefinition>(definitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<LevelAuthoringDefinition>();
                definition.Configure(
                    levelId,
                    workspace,
                    LevelProjectPaths.GetDevelopmentLevelRoot(levelId),
                    LevelProjectPaths.GetAuthoringRoot(levelId),
                    LevelProjectPaths.GetPreview2DScenePath(levelId),
                    LevelProjectPaths.GetPreview3DScenePath(levelId),
                    LevelProjectPaths.GetReleaseMainScenePath(levelId),
                    LevelProjectPaths.GetReleaseMapScenePath(levelId));
                AssetDatabase.CreateAsset(definition, definitionPath);
            }

            int pieceCount = Mathf.Max(1, workspace.PieceCount);
            for (int pieceIndex = 1; pieceIndex <= pieceCount; pieceIndex++)
            {
                for (int faceIndex = 0; faceIndex < CubeMapLayoutMath.FaceCount; faceIndex++)
                {
                    CubeMapFace face = (CubeMapFace)faceIndex;
                    EnsureChunk(levelId, pieceIndex, face, LevelContentKind.Geometry);
                    EnsureChunk(levelId, pieceIndex, face, LevelContentKind.Traversal);
                }
            }

            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            string releaseMainPath = LevelProjectPaths.GetReleaseMainScenePath(levelId);
            string releaseMapPath = LevelProjectPaths.GetReleaseMapScenePath(levelId);
            CreateReleaseMainSceneIfMissing(releaseMainPath, releaseMapPath);
            AssetDatabase.Refresh();
            definition = AssetDatabase.LoadAssetAtPath<LevelAuthoringDefinition>(
                definitionPath);
            if (selectDefinition)
            {
                Selection.activeObject = definition;
                EditorGUIUtility.PingObject(definition);
            }

            return definition;
        }

        internal static LevelAuthoringChunk EnsureChunk(
            string levelId,
            int pieceIndex,
            CubeMapFace face,
            LevelContentKind kind)
        {
            string folder = LevelProjectPaths.GetChunkFolder(levelId, pieceIndex, face);
            EnsureFolder(folder);
            string path = LevelProjectPaths.GetChunkPath(
                levelId,
                pieceIndex,
                face,
                kind);
            LevelAuthoringChunk chunk =
                AssetDatabase.LoadAssetAtPath<LevelAuthoringChunk>(path);
            if (chunk != null)
            {
                return chunk;
            }

            chunk = ScriptableObject.CreateInstance<LevelAuthoringChunk>();
            chunk.Configure(levelId, pieceIndex, face, "FullFace", kind);
            AssetDatabase.CreateAsset(chunk, path);
            return chunk;
        }

        internal static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void CreateReleaseMainSceneIfMissing(
            string releaseMainScenePath,
            string releaseMapScenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(releaseMainScenePath) != null)
            {
                return;
            }

            Scene previousActive = SceneManager.GetActiveScene();
            bool replaceUntitledScene = string.IsNullOrWhiteSpace(previousActive.path);
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                replaceUntitledScene ? NewSceneMode.Single : NewSceneMode.Additive);
            GameObject systems = new GameObject("ReleaseLevelSystems");
            SceneManager.MoveGameObjectToScene(systems, scene);
            ReleaseLevelLoader loader = systems.AddComponent<ReleaseLevelLoader>();
            loader.Configure(Path.GetFileNameWithoutExtension(releaseMapScenePath));
            EditorSceneManager.SaveScene(scene, releaseMainScenePath);
            EditorSceneManager.CloseScene(scene, true);
            if (!replaceUntitledScene && previousActive.IsValid() && previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(previousActive);
            }
        }
    }
}
