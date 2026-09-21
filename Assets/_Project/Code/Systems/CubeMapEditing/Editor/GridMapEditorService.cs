using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.CubeMapEditing.Editor
{
    internal static class GridMapEditorService
    {
        internal static GridMapPalette LoadPalette()
        {
            return AssetDatabase.LoadAssetAtPath<GridMapPalette>(
                CubeMapWorkspacePaths.GridMapPalettePath);
        }

        internal static GridMapPalette EnsurePalette()
        {
            GridMapPalette palette = LoadPalette();
            if (palette != null)
            {
                return palette;
            }

            EnsureFolder(CubeMapWorkspacePaths.MapItemDataFolder);
            palette = ScriptableObject.CreateInstance<GridMapPalette>();
            AssetDatabase.CreateAsset(palette, CubeMapWorkspacePaths.GridMapPalettePath);
            AssetDatabase.SaveAssets();
            return palette;
        }

        internal static GridMapItemDefinition CreateItemDefinition(
            GameObject source,
            string displayName,
            Vector2Int size,
            Vector2 pivot,
            bool allowOverlap,
            bool snapToGrid)
        {
            if (source == null)
            {
                return null;
            }

            EnsureFolder(CubeMapWorkspacePaths.MapItemPrefabFolder);
            EnsureFolder(CubeMapWorkspacePaths.MapItemDataFolder);
            GameObject prefab = ResolvePrefab(source, displayName);
            if (prefab == null)
            {
                return null;
            }

            string safeName = SanitizeFileName(
                string.IsNullOrWhiteSpace(displayName) ? source.name : displayName);
            string itemPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{CubeMapWorkspacePaths.MapItemDataFolder}/{safeName}.asset");
            GridMapItemDefinition definition = ScriptableObject.CreateInstance<GridMapItemDefinition>();
            definition.Configure(
                string.IsNullOrWhiteSpace(displayName) ? source.name : displayName,
                prefab,
                size,
                pivot,
                allowOverlap,
                snapToGrid);
            AssetDatabase.CreateAsset(definition, itemPath);
            GridMapPalette palette = EnsurePalette();
            palette.Add(definition);
            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = definition;
            return definition;
        }

        internal static GridMapPieceContext FindActivePiece()
        {
            Scene scene = SceneManager.GetActiveScene();
            CubeMapPieceAuthoring piece = FindInScene<CubeMapPieceAuthoring>(scene);
            CubeMapWorkspaceDefinition workspace = piece != null ? piece.Workspace : null;
            if (piece == null || workspace == null)
            {
                return default;
            }

            CubeMapFace face = GridMapEditorState.ActiveFace;
            Transform faceRoot = piece.GetFaceRoot(face);
            Transform content = faceRoot != null ? faceRoot.Find("Content") : null;
            return new GridMapPieceContext(piece, workspace, face, faceRoot, content);
        }

        internal static IEnumerable<GridMapPlacement> EnumeratePlacements(
            GridMapPieceContext context)
        {
            if (context.Content == null)
            {
                yield break;
            }

            foreach (GridMapPlacement placement in context.Content.GetComponentsInChildren<GridMapPlacement>(true))
            {
                yield return placement;
            }
        }

        internal static Vector2 GetCellSize(CubeMapWorkspaceDefinition workspace)
        {
            return workspace != null ? workspace.CellSize : Vector2.one;
        }

        internal static Vector2Int GetFootprintSize(
            GridMapItemDefinition definition,
            int rotationSteps)
        {
            if (definition == null)
            {
                return Vector2Int.one;
            }

            Vector2Int size = definition.SizeInCells;
            return rotationSteps % 2 == 0
                ? size
                : new Vector2Int(size.y, size.x);
        }

        internal static Rect GetPlacementRect(
            GridMapPieceContext context,
            GridMapPlacement placement)
        {
            Vector2 cellSize = GetCellSize(context.Workspace);
            Vector2Int size = placement.RotatedSizeInCells;
            if (!placement.SnappedToGrid)
            {
                Vector2 center = placement.UnsnappedLocalPosition;
                return new Rect(
                    center - Vector2.Scale(size, cellSize) * 0.5f,
                    Vector2.Scale(size, cellSize));
            }

            Vector2 min = new Vector2(
                -context.Workspace.FaceWidth * 0.5f + placement.AnchorCell.x * cellSize.x,
                -context.Workspace.PieceHeight * 0.5f + placement.AnchorCell.y * cellSize.y);
            return new Rect(min, Vector2.Scale(size, cellSize));
        }

        internal static Vector3 GetPlacementLocalPosition(
            GridMapPieceContext context,
            Vector2Int anchorCell,
            Vector2Int footprint,
            bool snapToGrid,
            Vector2 unsnappedPosition)
        {
            if (!snapToGrid)
            {
                return new Vector3(unsnappedPosition.x, unsnappedPosition.y, 0f);
            }

            Vector2 cellSize = GetCellSize(context.Workspace);
            return new Vector3(
                -context.Workspace.FaceWidth * 0.5f +
                (anchorCell.x + footprint.x * 0.5f) * cellSize.x,
                -context.Workspace.PieceHeight * 0.5f +
                (anchorCell.y + footprint.y * 0.5f) * cellSize.y,
                0f);
        }

        internal static GridMapPlacement Place(
            GridMapPieceContext context,
            GridMapItemDefinition definition,
            Vector2Int anchorCell,
            int rotationSteps,
            bool snapToGrid,
            bool allowOverlap,
            Vector2 unsnappedPosition)
        {
            if (context.Content == null || definition == null || definition.Prefab == null)
            {
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(
                definition.Prefab,
                context.Content.gameObject.scene) as GameObject;
            if (instance == null)
            {
                return null;
            }

            Undo.RegisterCreatedObjectUndo(instance, "放置网格地图物品");
            instance.transform.SetParent(context.Content, false);
            Vector2Int footprint = GetFootprintSize(definition, rotationSteps);
            instance.transform.localPosition = GetPlacementLocalPosition(
                context,
                anchorCell,
                footprint,
                snapToGrid,
                unsnappedPosition);
            instance.transform.localRotation = Quaternion.Euler(0f, 0f, rotationSteps * 90f);
            GridMapPlacement placement = instance.GetComponent<GridMapPlacement>();
            if (placement == null)
            {
                placement = Undo.AddComponent<GridMapPlacement>(instance);
            }

            placement.Configure(
                definition,
                anchorCell,
                rotationSteps,
                snapToGrid,
                allowOverlap,
                unsnappedPosition);
            EditorUtility.SetDirty(placement);
            EditorSceneManager.MarkSceneDirty(context.Piece.gameObject.scene);
            Selection.activeObject = instance;
            return placement;
        }

        internal static void DeletePlacements(ICollection<GridMapPlacement> placements)
        {
            if (placements == null || placements.Count == 0)
            {
                return;
            }

            Undo.SetCurrentGroupName("删除网格地图物品");
            int group = Undo.GetCurrentGroup();
            foreach (GridMapPlacement placement in placements)
            {
                if (placement != null)
                {
                    Undo.DestroyObjectImmediate(placement.gameObject);
                }
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.objects = Array.Empty<UnityEngine.Object>();
        }

        internal static bool IsInsideFace(
            GridMapPieceContext context,
            Vector2 localPoint)
        {
            return context.Workspace != null &&
                   localPoint.x >= -context.Workspace.FaceWidth * 0.5f &&
                   localPoint.x <= context.Workspace.FaceWidth * 0.5f &&
                   localPoint.y >= -context.Workspace.PieceHeight * 0.5f &&
                   localPoint.y <= context.Workspace.PieceHeight * 0.5f;
        }

        private static GameObject ResolvePrefab(GameObject source, string displayName)
        {
            string assetPath = AssetDatabase.GetAssetPath(source);
            if (PrefabUtility.IsPartOfPrefabAsset(source) && !string.IsNullOrWhiteSpace(assetPath))
            {
                return source;
            }

            string safeName = SanitizeFileName(
                string.IsNullOrWhiteSpace(displayName) ? source.name : displayName);
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{CubeMapWorkspacePaths.MapItemPrefabFolder}/{safeName}.prefab");
            return PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
        }

        private static void EnsureFolder(string folderPath)
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

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(value) ? "GridMapItem" : value.Trim();
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }

    internal readonly struct GridMapPieceContext
    {
        internal GridMapPieceContext(
            CubeMapPieceAuthoring piece,
            CubeMapWorkspaceDefinition workspace,
            CubeMapFace face,
            Transform faceRoot,
            Transform content)
        {
            Piece = piece;
            Workspace = workspace;
            Face = face;
            FaceRoot = faceRoot;
            Content = content;
        }

        internal CubeMapPieceAuthoring Piece { get; }
        internal CubeMapWorkspaceDefinition Workspace { get; }
        internal CubeMapFace Face { get; }
        internal Transform FaceRoot { get; }
        internal Transform Content { get; }
        internal bool IsValid => Piece != null && Workspace != null && FaceRoot != null && Content != null;
    }
}
