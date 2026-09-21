using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.CubeMapEditing.Editor
{
    internal static class CubeMapWorkspaceService
    {
        internal const int DefaultPieceCount = 4;
        internal const string GeneratedRootName = "[GENERATED] Cube Map";

        internal static CubeMapWorkspaceDefinition LoadWorkspace()
        {
            return AssetDatabase.LoadAssetAtPath<CubeMapWorkspaceDefinition>(
                CubeMapWorkspacePaths.WorkspaceAssetPath);
        }

        internal static void InitializeWorkspace()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            CubeMapWorkspaceDefinition workspace = GetOrCreateWorkspace();
            for (int index = 1; index <= DefaultPieceCount; index++)
            {
                string path = CubeMapWorkspacePaths.GetPieceScenePath(index);
                AddPiecePathIfNeeded(workspace, path);
                CreatePieceSceneIfMissing(workspace, path, index);
            }

            EditorUtility.SetDirty(workspace);
            AssetDatabase.SaveAssets();
            CreateTargetSceneIfMissing(workspace.Total2DScenePath, false);
            CreateTargetSceneIfMissing(workspace.Main3DScenePath, true);
            Build(workspace, false);
        }

        internal static void AddPieceAndOpen()
        {
            CubeMapWorkspaceDefinition workspace = LoadWorkspace();
            if (workspace == null)
            {
                InitializeWorkspace();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            int pieceIndex = workspace.PieceCount + 1;
            string path = CubeMapWorkspacePaths.GetPieceScenePath(pieceIndex);
            AddPiecePathIfNeeded(workspace, path);
            EditorUtility.SetDirty(workspace);
            AssetDatabase.SaveAssets();
            CreatePieceSceneIfMissing(workspace, path, pieceIndex);
            OpenScene(path, true);
        }

        internal static void OpenPiece(int zeroBasedIndex)
        {
            CubeMapWorkspaceDefinition workspace = LoadWorkspace();
            if (workspace == null || workspace.PieceCount == 0)
            {
                InitializeWorkspace();
                return;
            }

            int index = Mathf.Clamp(zeroBasedIndex, 0, workspace.PieceCount - 1);
            OpenScene(workspace.PieceScenePaths[index], true);
        }

        internal static void OpenTotal2D()
        {
            CubeMapWorkspaceDefinition workspace = LoadWorkspace();
            if (workspace == null)
            {
                InitializeWorkspace();
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(workspace.Total2DScenePath) == null)
            {
                BuildTotal2D();
                return;
            }

            OpenScene(workspace.Total2DScenePath, true);
            SetSceneViewMode(true);
        }

        internal static void OpenMain3D()
        {
            CubeMapWorkspaceDefinition workspace = LoadWorkspace();
            if (workspace == null)
            {
                InitializeWorkspace();
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(workspace.Main3DScenePath) == null)
            {
                BuildMain3D();
                return;
            }

            OpenScene(workspace.Main3DScenePath, true);
            SetSceneViewMode(false);
        }

        internal static void BuildTotal2D()
        {
            CubeMapWorkspaceDefinition workspace = LoadWorkspace();
            if (workspace == null)
            {
                InitializeWorkspace();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Build(workspace, false);
        }

        internal static void BuildMain3D()
        {
            CubeMapWorkspaceDefinition workspace = LoadWorkspace();
            if (workspace == null)
            {
                InitializeWorkspace();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Build(workspace, true);
        }

        internal static void SelectWorkspaceAsset()
        {
            CubeMapWorkspaceDefinition workspace = LoadWorkspace();
            if (workspace == null)
            {
                InitializeWorkspace();
                return;
            }

            Selection.activeObject = workspace;
            EditorGUIUtility.PingObject(workspace);
        }

        private static CubeMapWorkspaceDefinition GetOrCreateWorkspace()
        {
            CubeMapWorkspaceDefinition existing = LoadWorkspace();
            if (existing != null)
            {
                return existing;
            }

            EnsureAssetFolder(CubeMapWorkspacePaths.DataFolder);
            EnsureAssetFolder(CubeMapWorkspacePaths.PieceSceneFolder);

            CubeMapWorkspaceDefinition workspace =
                ScriptableObject.CreateInstance<CubeMapWorkspaceDefinition>();
            workspace.Configure(
                CubeMapWorkspacePaths.LevelId,
                12f,
                4f,
                6,
                2,
                CubeMapWorkspacePaths.Total2DScenePath,
                CubeMapWorkspacePaths.Main3DScenePath);
            AssetDatabase.CreateAsset(workspace, CubeMapWorkspacePaths.WorkspaceAssetPath);
            AssetDatabase.SaveAssets();
            return workspace;
        }

        private static void AddPiecePathIfNeeded(
            CubeMapWorkspaceDefinition workspace,
            string path)
        {
            if (workspace.AddPieceScene(path))
            {
                EditorUtility.SetDirty(workspace);
            }
        }

        private static void CreatePieceSceneIfMissing(
            CubeMapWorkspaceDefinition workspace,
            string scenePath,
            int pieceIndex)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                return;
            }

            EnsureAssetFolder(CubeMapWorkspacePaths.PieceSceneFolder);
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            GameObject authoringRoot = new GameObject(
                $"{workspace.LevelId}_Piece_{pieceIndex:00}_Authoring");
            CubeMapPieceAuthoring authoring =
                authoringRoot.AddComponent<CubeMapPieceAuthoring>();
            Transform[] faceRoots = new Transform[CubeMapLayoutMath.FaceCount];

            for (int faceIndex = 0; faceIndex < CubeMapLayoutMath.FaceCount; faceIndex++)
            {
                CubeMapFace face = (CubeMapFace)faceIndex;
                GameObject faceRoot = new GameObject(
                    $"Face_{faceIndex + 1:00}_{face}_{CubeMapLayoutMath.GetFaceLabel(face)}");
                faceRoot.transform.SetParent(authoringRoot.transform, false);
                faceRoot.transform.localPosition =
                    CubeMapLayoutMath.GetPieceFaceCenter(face, workspace.FaceWidth);

                GameObject content = new GameObject("Content");
                content.transform.SetParent(faceRoot.transform, false);
                faceRoots[faceIndex] = faceRoot.transform;
            }

            authoring.Configure(workspace, pieceIndex, faceRoots);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreateTargetSceneIfMissing(string scenePath, bool defaultObjects)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(
                defaultObjects ? NewSceneSetup.DefaultGameObjects : NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void Build(CubeMapWorkspaceDefinition workspace, bool folded)
        {
            string targetPath = folded
                ? workspace.Main3DScenePath
                : workspace.Total2DScenePath;
            CreateTargetSceneIfMissing(targetPath, folded);
            Scene targetScene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
            List<LoadedPiece> pieces = new List<LoadedPiece>();

            try
            {
                if (!LoadAndValidatePieces(workspace, pieces, out string error))
                {
                    EditorUtility.DisplayDialog("无法拼合地图", error, "确定");
                    return;
                }

                DestroyPreviousGeneratedRoot(targetScene);
                GameObject generatedRoot = new GameObject(GeneratedRootName);
                SceneManager.MoveGameObjectToScene(generatedRoot, targetScene);
                CubeMapGeneratedLayout marker =
                    generatedRoot.AddComponent<CubeMapGeneratedLayout>();
                marker.Configure(workspace, folded);

                foreach (LoadedPiece piece in pieces)
                {
                    GameObject pieceRoot = new GameObject(
                        $"Piece_{piece.Authoring.PieceIndex:00}");
                    SceneManager.MoveGameObjectToScene(pieceRoot, targetScene);
                    pieceRoot.transform.SetParent(generatedRoot.transform, false);

                    for (int faceIndex = 0;
                         faceIndex < CubeMapLayoutMath.FaceCount;
                         faceIndex++)
                    {
                        CubeMapFace face = (CubeMapFace)faceIndex;
                        Transform sourceRoot = piece.Authoring.GetFaceRoot(face);
                        GameObject clone = Object.Instantiate(sourceRoot.gameObject);
                        clone.name = $"{faceIndex + 1:00}_{face}_{CubeMapLayoutMath.GetFaceLabel(face)}";
                        SceneManager.MoveGameObjectToScene(clone, targetScene);
                        clone.transform.SetParent(pieceRoot.transform, false);
                        clone.transform.localScale = Vector3.one;
                        clone.transform.localPosition = folded
                            ? CubeMapLayoutMath.GetFoldedSectionCenter(
                                face,
                                workspace.FaceWidth,
                                workspace.PieceHeight,
                                piece.Authoring.PieceIndex)
                            : CubeMapLayoutMath.GetFlatSectionCenter(
                                face,
                                workspace.FaceWidth,
                                workspace.PieceHeight,
                                piece.Authoring.PieceIndex);
                        clone.transform.localRotation = folded
                            ? CubeMapLayoutMath.GetFoldedRotation(face)
                            : Quaternion.identity;
                    }
                }

                EditorUtility.SetDirty(marker);
                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
                Selection.activeObject = generatedRoot;
                SetSceneViewMode(!folded);
                FrameSelection();
            }
            finally
            {
                foreach (LoadedPiece piece in pieces)
                {
                    if (piece.Scene.isLoaded)
                    {
                        EditorSceneManager.CloseScene(piece.Scene, true);
                    }
                }
            }
        }

        private static bool LoadAndValidatePieces(
            CubeMapWorkspaceDefinition workspace,
            List<LoadedPiece> pieces,
            out string error)
        {
            if (workspace.PieceCount == 0)
            {
                error = "工作区还没有任何小拼图场景。";
                return false;
            }

            for (int index = 0; index < workspace.PieceCount; index++)
            {
                string path = workspace.PieceScenePaths[index];
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    error = $"找不到小拼图场景：\n{path}";
                    return false;
                }

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                CubeMapPieceAuthoring authoring = FindPieceAuthoring(scene);
                if (authoring == null)
                {
                    pieces.Add(new LoadedPiece(scene, null));
                    error = $"场景缺少 CubeMapPieceAuthoring：\n{path}";
                    return false;
                }

                for (int faceIndex = 0;
                     faceIndex < CubeMapLayoutMath.FaceCount;
                     faceIndex++)
                {
                    if (authoring.GetFaceRoot((CubeMapFace)faceIndex) == null)
                    {
                        pieces.Add(new LoadedPiece(scene, authoring));
                        error = $"场景的四个面引用不完整：\n{path}";
                        return false;
                    }
                }

                pieces.Add(new LoadedPiece(scene, authoring));
            }

            error = null;
            return true;
        }

        private static CubeMapPieceAuthoring FindPieceAuthoring(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                CubeMapPieceAuthoring authoring =
                    root.GetComponentInChildren<CubeMapPieceAuthoring>(true);
                if (authoring != null)
                {
                    return authoring;
                }
            }

            return null;
        }

        private static void DestroyPreviousGeneratedRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == GeneratedRootName)
                {
                    Object.DestroyImmediate(root);
                    return;
                }
            }
        }

        private static void OpenScene(string path, bool promptToSave)
        {
            if (promptToSave &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                EditorUtility.DisplayDialog("场景不存在", path, "确定");
                return;
            }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        private static void EnsureAssetFolder(string folderPath)
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

        private static void SetSceneViewMode(bool twoDimensional)
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null)
            {
                return;
            }

            view.in2DMode = twoDimensional;
            view.Repaint();
        }

        private static void FrameSelection()
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.FrameSelected();
                view.Repaint();
            }
        }

        private readonly struct LoadedPiece
        {
            internal LoadedPiece(Scene scene, CubeMapPieceAuthoring authoring)
            {
                Scene = scene;
                Authoring = authoring;
            }

            internal Scene Scene { get; }
            internal CubeMapPieceAuthoring Authoring { get; }
        }
    }
}
