using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.CubeMapEditing.Editor
{
    [InitializeOnLoad]
    internal static class GridMapSceneInteraction
    {
        private static readonly HashSet<GridMapPlacement> erasedDuringDrag =
            new HashSet<GridMapPlacement>();
        private static int controlId;
        private static bool mouseDown;
        private static bool dragging;
        private static bool rightDragging;
        private static Vector2 dragStartLocal;
        private static Vector2 currentLocal;
        private static GridMapPlacement pressedPlacement;

        static GridMapSceneInteraction()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Cleanup;
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        internal static void OnSceneGUI(SceneView sceneView)
        {
            GridMapPieceContext context = GridMapEditorService.FindActivePiece();
            if (!CubeMapEditorToolState.IsGridMap || !context.IsValid)
            {
                CleanupPreviewState();
                return;
            }

            Event evt = Event.current;
            controlId = GUIUtility.GetControlID(
                "GridMapEditor".GetHashCode(),
                FocusType.Passive);
            if (evt.type == EventType.Layout && !evt.alt)
            {
                HandleUtility.AddDefaultControl(controlId);
            }

            bool hasPointer = TryGetPointer(context, evt.mousePosition, out Vector2 localPoint);
            if (hasPointer)
            {
                currentLocal = localPoint;
            }

            if (evt.type == EventType.Repaint)
            {
                DrawGrid(context);
                DrawPlacementSelection(context);
                if (hasPointer && GridMapEditorState.CurrentItem != null)
                {
                    DrawPreview(context, currentLocal);
                }

                if (dragging)
                {
                    DrawDragBox(context, dragStartLocal, currentLocal);
                }
            }

            if (evt.alt)
            {
                return;
            }

            switch (evt.type)
            {
                case EventType.KeyDown:
                    HandleKey(context, evt);
                    break;
                case EventType.MouseDown:
                    HandleMouseDown(context, evt, hasPointer, localPoint);
                    break;
                case EventType.MouseDrag:
                    HandleMouseDrag(context, evt, hasPointer, localPoint);
                    break;
                case EventType.MouseUp:
                    HandleMouseUp(context, evt, hasPointer, localPoint);
                    break;
            }
        }

        internal static bool TryGetPointer(
            GridMapPieceContext context,
            Vector2 guiPosition,
            out Vector2 localPoint)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);
            Plane plane = new Plane(context.FaceRoot.forward, context.FaceRoot.position);
            if (plane.Raycast(ray, out float distance))
            {
                localPoint = context.FaceRoot.InverseTransformPoint(ray.GetPoint(distance));
                return GridMapEditorService.IsInsideFace(context, localPoint);
            }

            localPoint = default;
            return false;
        }

        private static void HandleKey(GridMapPieceContext context, Event evt)
        {
            if (evt.keyCode == KeyCode.R && GridMapEditorState.CurrentItem != null)
            {
                GridMapEditorState.Rotate(evt.shift ? -1 : 1);
                evt.Use();
                return;
            }

            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                List<GridMapPlacement> selected = GetSelectedPlacements();
                GridMapEditorService.DeletePlacements(selected);
                evt.Use();
            }
        }

        private static void HandleMouseDown(
            GridMapPieceContext context,
            Event evt,
            bool hasPointer,
            Vector2 localPoint)
        {
            if (!hasPointer || evt.button > 1)
            {
                return;
            }

            GUIUtility.hotControl = controlId;
            mouseDown = true;
            dragging = false;
            dragStartLocal = localPoint;
            currentLocal = localPoint;
            if (evt.button == 1)
            {
                rightDragging = true;
                erasedDuringDrag.Clear();
                EraseAt(context, localPoint);
            }
            else
            {
                pressedPlacement = FindPlacementAt(context, localPoint);
            }

            evt.Use();
        }

        private static void HandleMouseDrag(
            GridMapPieceContext context,
            Event evt,
            bool hasPointer,
            Vector2 localPoint)
        {
            if (!mouseDown)
            {
                return;
            }

            if (hasPointer)
            {
                currentLocal = localPoint;
            }

            if (Vector2.Distance(dragStartLocal, currentLocal) > 0.04f)
            {
                dragging = true;
            }

            if (rightDragging && hasPointer)
            {
                EraseAt(context, localPoint);
            }

            SceneView.RepaintAll();
            evt.Use();
        }

        private static void HandleMouseUp(
            GridMapPieceContext context,
            Event evt,
            bool hasPointer,
            Vector2 localPoint)
        {
            if (!mouseDown)
            {
                return;
            }

            if (hasPointer)
            {
                currentLocal = localPoint;
            }

            if (evt.button == 1)
            {
                rightDragging = false;
            }
            else if (GridMapEditorState.CurrentItem != null)
            {
                if (dragging)
                {
                    PlaceInBox(context, dragStartLocal, currentLocal);
                }
                else if (hasPointer)
                {
                    PlaceAt(context, currentLocal);
                }
            }
            else if (dragging)
            {
                SelectInBox(context, dragStartLocal, currentLocal);
            }
            else if (hasPointer)
            {
                SelectAt(context, currentLocal, pressedPlacement);
            }

            mouseDown = false;
            dragging = false;
            pressedPlacement = null;
            erasedDuringDrag.Clear();
            GUIUtility.hotControl = 0;
            SceneView.RepaintAll();
            evt.Use();
        }

        private static void PlaceAt(GridMapPieceContext context, Vector2 localPoint)
        {
            GridMapItemDefinition definition = GridMapEditorState.CurrentItem;
            if (definition == null)
            {
                return;
            }

            Vector2Int anchor = GetAnchorCell(context, localPoint, definition);
            Vector2 unsnapped = localPoint;
            if (CanPlace(context, definition, anchor, GridMapEditorState.RotationSteps,
                    GridMapEditorState.SnapToGrid, GridMapEditorState.AllowOverlap, unsnapped))
            {
                GridMapEditorService.Place(
                    context,
                    definition,
                    anchor,
                    GridMapEditorState.RotationSteps,
                    GridMapEditorState.SnapToGrid,
                    GridMapEditorState.AllowOverlap,
                    unsnapped);
            }
        }

        private static void PlaceInBox(
            GridMapPieceContext context,
            Vector2 from,
            Vector2 to)
        {
            GridMapItemDefinition definition = GridMapEditorState.CurrentItem;
            if (definition == null)
            {
                return;
            }

            Vector2Int first = GetAnchorCell(context, from, definition);
            Vector2Int last = GetAnchorCell(context, to, definition);
            int minX = Mathf.Min(first.x, last.x);
            int maxX = Mathf.Max(first.x, last.x);
            int minY = Mathf.Min(first.y, last.y);
            int maxY = Mathf.Max(first.y, last.y);
            Undo.SetCurrentGroupName("批量放置网格地图物品");
            int group = Undo.GetCurrentGroup();
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2Int anchor = new Vector2Int(x, y);
                    Vector2 unsnapped = CellCenter(context, anchor, definition);
                    if (CanPlace(context, definition, anchor, GridMapEditorState.RotationSteps,
                            true, GridMapEditorState.AllowOverlap, unsnapped))
                    {
                        GridMapEditorService.Place(
                            context,
                            definition,
                            anchor,
                            GridMapEditorState.RotationSteps,
                            true,
                            GridMapEditorState.AllowOverlap,
                            unsnapped);
                    }
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        private static void EraseAt(GridMapPieceContext context, Vector2 localPoint)
        {
            GridMapPlacement placement = FindPlacementAt(context, localPoint);
            if (placement == null || !erasedDuringDrag.Add(placement))
            {
                return;
            }

            Undo.DestroyObjectImmediate(placement.gameObject);
            EditorUtility.SetDirty(context.Piece);
            EditorSceneManager.MarkSceneDirty(context.Piece.gameObject.scene);
        }

        private static void SelectAt(
            GridMapPieceContext context,
            Vector2 localPoint,
            GridMapPlacement pressed)
        {
            GridMapPlacement placement = pressed ?? FindPlacementAt(context, localPoint);
            if (placement == null)
            {
                Selection.objects = System.Array.Empty<Object>();
                return;
            }

            Selection.activeObject = placement.gameObject;
        }

        private static void SelectInBox(
            GridMapPieceContext context,
            Vector2 from,
            Vector2 to)
        {
            Rect selectionRect = MakeRect(from, to);
            List<Object> selected = new List<Object>();
            foreach (GridMapPlacement placement in GridMapEditorService.EnumeratePlacements(context))
            {
                if (selectionRect.Overlaps(
                        GridMapEditorService.GetPlacementRect(context, placement),
                        true))
                {
                    selected.Add(placement.gameObject);
                }
            }

            Selection.objects = selected.ToArray();
        }

        private static List<GridMapPlacement> GetSelectedPlacements()
        {
            List<GridMapPlacement> selected = new List<GridMapPlacement>();
            foreach (Object selectedObject in Selection.objects)
            {
                GridMapPlacement placement = selectedObject as GridMapPlacement;
                if (placement == null && selectedObject is GameObject gameObject)
                {
                    placement = gameObject.GetComponentInParent<GridMapPlacement>();
                }

                if (placement != null && !selected.Contains(placement))
                {
                    selected.Add(placement);
                }
            }

            return selected;
        }

        private static GridMapPlacement FindPlacementAt(
            GridMapPieceContext context,
            Vector2 localPoint)
        {
            GridMapPlacement result = null;
            foreach (GridMapPlacement placement in GridMapEditorService.EnumeratePlacements(context))
            {
                if (GridMapEditorService.GetPlacementRect(context, placement).Contains(localPoint))
                {
                    result = placement;
                }
            }

            return result;
        }

        private static Vector2Int GetAnchorCell(
            GridMapPieceContext context,
            Vector2 localPoint,
            GridMapItemDefinition definition)
        {
            Vector2 cellSize = GridMapEditorService.GetCellSize(context.Workspace);
            Vector2 local = localPoint + new Vector2(
                context.Workspace.FaceWidth * 0.5f,
                context.Workspace.PieceHeight * 0.5f);
            Vector2Int footprint = GridMapEditorService.GetFootprintSize(
                definition,
                GridMapEditorState.RotationSteps);
            int x = Mathf.FloorToInt(local.x / cellSize.x - footprint.x * 0.5f);
            int y = Mathf.FloorToInt(local.y / cellSize.y - footprint.y * 0.5f);
            return new Vector2Int(x, y);
        }

        private static Vector2 CellCenter(
            GridMapPieceContext context,
            Vector2Int anchor,
            GridMapItemDefinition definition)
        {
            Vector2Int footprint = GridMapEditorService.GetFootprintSize(
                definition,
                GridMapEditorState.RotationSteps);
            Vector3 position = GridMapEditorService.GetPlacementLocalPosition(
                context,
                anchor,
                footprint,
                true,
                default);
            return new Vector2(position.x, position.y);
        }

        private static bool CanPlace(
            GridMapPieceContext context,
            GridMapItemDefinition definition,
            Vector2Int anchor,
            int rotation,
            bool snapToGrid,
            bool allowOverlap,
            Vector2 unsnappedPosition)
        {
            Vector2Int footprint = GridMapEditorService.GetFootprintSize(definition, rotation);
            Vector2 position = snapToGrid
                ? CellCenter(context, anchor, definition)
                : unsnappedPosition;
            Rect candidate = new Rect(
                position - Vector2.Scale(
                    footprint,
                    GridMapEditorService.GetCellSize(context.Workspace)) * 0.5f,
                Vector2.Scale(
                    footprint,
                    GridMapEditorService.GetCellSize(context.Workspace)));
            Rect faceRect = new Rect(
                -context.Workspace.FaceWidth * 0.5f,
                -context.Workspace.PieceHeight * 0.5f,
                context.Workspace.FaceWidth,
                context.Workspace.PieceHeight);
            if (!faceRect.Contains(candidate.min) || !faceRect.Contains(candidate.max))
            {
                return false;
            }

            if (allowOverlap)
            {
                return true;
            }

            foreach (GridMapPlacement placement in GridMapEditorService.EnumeratePlacements(context))
            {
                if (!placement.AllowOverlap &&
                    candidate.Overlaps(GridMapEditorService.GetPlacementRect(context, placement)))
                {
                    return false;
                }
            }

            return true;
        }

        private static void DrawGrid(GridMapPieceContext context)
        {
            Transform faceRoot = context.FaceRoot;
            Vector2 cellSize = GridMapEditorService.GetCellSize(context.Workspace);
            Matrix4x4 previous = Handles.matrix;
            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;
            Handles.matrix = faceRoot.localToWorldMatrix;
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = new Color(0.18f, 0.68f, 1f, 0.45f);
            float halfWidth = context.Workspace.FaceWidth * 0.5f;
            float halfHeight = context.Workspace.PieceHeight * 0.5f;
            for (int x = 0; x <= context.Workspace.ColumnsPerFace; x++)
            {
                float localX = -halfWidth + x * cellSize.x;
                Handles.DrawLine(
                    new Vector3(localX, -halfHeight, -0.01f),
                    new Vector3(localX, halfHeight, -0.01f));
            }

            for (int y = 0; y <= context.Workspace.RowsPerPiece; y++)
            {
                float localY = -halfHeight + y * cellSize.y;
                Handles.DrawLine(
                    new Vector3(-halfWidth, localY, -0.01f),
                    new Vector3(halfWidth, localY, -0.01f));
            }

            Handles.matrix = previous;
            Handles.color = previousColor;
            Handles.zTest = previousZTest;
        }

        private static void DrawPlacementSelection(GridMapPieceContext context)
        {
            foreach (GridMapPlacement placement in GridMapEditorService.EnumeratePlacements(context))
            {
                if (!Selection.Contains(placement.gameObject))
                {
                    continue;
                }

                DrawRect(context, GridMapEditorService.GetPlacementRect(context, placement),
                    new Color(1f, 0.8f, 0.15f, 0.95f),
                    new Color(1f, 0.8f, 0.15f, 0.12f));
            }
        }

        private static void DrawPreview(GridMapPieceContext context, Vector2 localPoint)
        {
            GridMapItemDefinition definition = GridMapEditorState.CurrentItem;
            Vector2Int anchor = GetAnchorCell(context, localPoint, definition);
            bool valid = CanPlace(
                context,
                definition,
                anchor,
                GridMapEditorState.RotationSteps,
                GridMapEditorState.SnapToGrid,
                GridMapEditorState.AllowOverlap,
                localPoint);
            Vector2Int footprint = GridMapEditorService.GetFootprintSize(
                definition,
                GridMapEditorState.RotationSteps);
            Vector2 center = GridMapEditorState.SnapToGrid
                ? CellCenter(context, anchor, definition)
                : localPoint;
            Vector2 size = Vector2.Scale(
                footprint,
                GridMapEditorService.GetCellSize(context.Workspace));
            DrawRect(
                context,
                new Rect(center - size * 0.5f, size),
                valid ? new Color(0.2f, 0.95f, 0.45f, 0.8f) : new Color(1f, 0.2f, 0.2f, 0.9f),
                valid ? new Color(0.2f, 0.95f, 0.45f, 0.22f) : new Color(1f, 0.2f, 0.2f, 0.22f));
            Handles.Label(
                context.FaceRoot.TransformPoint(center),
                $"{definition.DisplayName}  {footprint.x}×{footprint.y}",
                EditorStyles.boldLabel);
        }

        private static void DrawDragBox(
            GridMapPieceContext context,
            Vector2 from,
            Vector2 to)
        {
            DrawRect(
                context,
                MakeRect(from, to),
                new Color(0.25f, 0.75f, 1f, 0.9f),
                new Color(0.25f, 0.75f, 1f, 0.08f));
        }

        private static void DrawRect(
            GridMapPieceContext context,
            Rect rect,
            Color outline,
            Color fill)
        {
            Matrix4x4 previous = Handles.matrix;
            Color previousColor = Handles.color;
            Handles.matrix = context.FaceRoot.localToWorldMatrix;
            Handles.zTest = CompareFunction.Always;
            Vector3[] corners =
            {
                new Vector3(rect.xMin, rect.yMin, -0.02f),
                new Vector3(rect.xMin, rect.yMax, -0.02f),
                new Vector3(rect.xMax, rect.yMax, -0.02f),
                new Vector3(rect.xMax, rect.yMin, -0.02f)
            };
            Handles.DrawSolidRectangleWithOutline(corners, fill, outline);
            Handles.matrix = previous;
            Handles.color = previousColor;
        }

        private static Rect MakeRect(Vector2 from, Vector2 to)
        {
            return Rect.MinMaxRect(
                Mathf.Min(from.x, to.x),
                Mathf.Min(from.y, to.y),
                Mathf.Max(from.x, to.x),
                Mathf.Max(from.y, to.y));
        }

        private static void Cleanup()
        {
            mouseDown = false;
            dragging = false;
            rightDragging = false;
            erasedDuringDrag.Clear();
        }

        private static void CleanupPreviewState()
        {
            if (mouseDown)
            {
                Cleanup();
                GUIUtility.hotControl = 0;
            }
        }
    }
}
