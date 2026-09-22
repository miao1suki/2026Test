using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.RopePaths.Editor
{
    [InitializeOnLoad]
    internal static class RopePathSceneDrawer
    {
        static RopePathSceneDrawer()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
            if (network == null)
            {
                return;
            }

            RopeProjectionDirection direction = network.EditorDirection;
            RopePathGraph graph = network.BuildPath(direction);
            Matrix4x4 previousMatrix = Handles.matrix;
            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;
            Handles.zTest = CompareFunction.Always;
            DrawSegments(network, sceneView);
            DrawConnections(graph, sceneView);
            DrawPlatforms(network);
            Handles.matrix = previousMatrix;
            Handles.color = previousColor;
            Handles.zTest = previousZTest;
        }

        private static void DrawSegments(RopePathNetwork network, SceneView sceneView)
        {
            RopeSegment selected = RopePathEditorService.FindSelectedSegment();
            foreach (RopeSegment segment in network.Segments)
            {
                if (segment == null)
                {
                    continue;
                }

                RopePathEditorService.EnsureSegmentVisuals(segment);

                Vector3 worldA = segment.GetWorldEndpoint(RopeEndpoint.A);
                Vector3 worldB = segment.GetWorldEndpoint(RopeEndpoint.B);
                bool isSelected = segment == selected;
                Handles.color = isSelected
                    ? new Color(0.2f, 1f, 0.45f, 1f)
                    : new Color(0.15f, 0.72f, 1f, 0.85f);
                Handles.DrawAAPolyLine(isSelected ? 4f : 2.5f, worldA, worldB);
                Handles.Label(
                    Vector3.Lerp(worldA, worldB, 0.5f),
                    $"绳 {ShortId(segment.SegmentId)}");
                DrawEndpoint(segment, RopeEndpoint.A, worldA, isSelected, sceneView);
                DrawEndpoint(segment, RopeEndpoint.B, worldB, isSelected, sceneView);

                if (isSelected)
                {
                    DrawSegmentTransformHandles(segment);
                }

                if (!isSelected)
                {
                    if (Handles.Button(
                            Vector3.Lerp(worldA, worldB, 0.5f),
                            Quaternion.identity,
                            HandleUtility.GetHandleSize(worldA) * 0.1f,
                            HandleUtility.GetHandleSize(worldA) * 0.12f,
                            Handles.DotHandleCap))
                    {
                        Selection.activeGameObject = segment.gameObject;
                        SceneView.RepaintAll();
                    }
                }
            }
        }

        private static void DrawSegmentTransformHandles(RopeSegment segment)
        {
            Vector3 midpoint = Vector3.Lerp(
                segment.GetWorldEndpoint(RopeEndpoint.A),
                segment.GetWorldEndpoint(RopeEndpoint.B),
                0.5f);
            EditorGUI.BeginChangeCheck();
            Vector3 movedPosition = Handles.PositionHandle(
                midpoint,
                segment.transform.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(segment.transform, "移动整条绳子");
                segment.transform.position += movedPosition - midpoint;
                segment.RefreshVisuals();
                RopePathNetwork network = segment.GetComponentInParent<RopePathNetwork>();
                network?.InvalidateCache();
                RopePathEditorService.MarkDirty(network, segment.gameObject);
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            Quaternion movedRotation = Handles.RotationHandle(
                segment.transform.rotation,
                midpoint);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(segment.transform, "旋转整条绳子");
                Quaternion delta = movedRotation *
                                   Quaternion.Inverse(segment.transform.rotation);
                segment.transform.position = midpoint +
                                             delta * (segment.transform.position - midpoint);
                segment.transform.rotation = movedRotation;
                segment.RefreshVisuals();
                RopePathNetwork network = segment.GetComponentInParent<RopePathNetwork>();
                network?.InvalidateCache();
                RopePathEditorService.MarkDirty(network, segment.gameObject);
                SceneView.RepaintAll();
            }
        }

        private static void DrawEndpoint(
            RopeSegment segment,
            RopeEndpoint endpoint,
            Vector3 worldPosition,
            bool isSelected,
            SceneView sceneView)
        {
            Handles.color = endpoint == RopeEndpoint.A
                ? new Color(1f, 0.78f, 0.2f, 1f)
                : new Color(1f, 0.35f, 0.2f, 1f);
            float size = HandleUtility.GetHandleSize(worldPosition) * 0.08f;
            bool isActiveEndpoint = isSelected &&
                                    RopePathEditorState.ActiveEndpoint == endpoint;
            if (isActiveEndpoint)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.DoPositionHandle(worldPosition, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(segment, "移动绳子端点");
                    segment.SetWorldEndpoint(endpoint, moved);
                    RopePathNetwork network = segment.GetComponentInParent<RopePathNetwork>();
                    network?.InvalidateCache();
                    RopePathEditorService.MarkDirty(network, segment.gameObject);
                    SceneView.RepaintAll();
                }
            }
            else
            {
                if (Handles.Button(
                        worldPosition,
                        Quaternion.identity,
                        size * 1.35f,
                        size * 1.55f,
                        Handles.SphereHandleCap))
                {
                    Selection.activeGameObject = segment.gameObject;
                    RopePathEditorState.ActiveEndpoint = endpoint;
                    SceneView.RepaintAll();
                }
            }

            Vector3 outward = segment.GetWorldDirection(endpoint);
            if (outward.sqrMagnitude > 0.000001f)
            {
                Handles.color = new Color(1f, 0.84f, 0.25f, 0.8f);
                Handles.DrawDottedLine(
                    worldPosition,
                    worldPosition + outward * Mathf.Max(0.8f, HandleUtility.GetHandleSize(worldPosition) * 0.65f),
                    4f);
                Handles.Label(worldPosition, endpoint == RopeEndpoint.A ? "A" : "B");
            }
        }

        private static void DrawConnections(RopePathGraph graph, SceneView sceneView)
        {
            Handles.color = new Color(1f, 0.9f, 0.12f, 0.98f);
            for (int index = 0; index < graph.Connections.Count; index++)
            {
                RopePathConnection connection = graph.Connections[index];
                Vector3 first = connection.First.Segment.GetWorldEndpoint(connection.First.Endpoint);
                Vector3 second = connection.Second.Segment.GetWorldEndpoint(connection.Second.Endpoint);
                Vector3 delta = second - first;
                Vector3 midpoint = Vector3.Lerp(first, second, 0.5f);
                float markerSize = HandleUtility.GetHandleSize(midpoint) * 0.14f;
                Handles.DrawDottedLine(first, second, 3f);
                if (delta.sqrMagnitude > 0.000001f)
                {
                    Handles.ArrowHandleCap(
                        0,
                        midpoint,
                        Quaternion.LookRotation(delta.normalized),
                        markerSize * 1.4f,
                        EventType.Repaint);
                }

                Vector3 cameraNormal = sceneView.camera != null
                    ? sceneView.camera.transform.forward
                    : Vector3.forward;
                Handles.DrawWireDisc(midpoint, cameraNormal, markerSize);
                Handles.Label(
                    midpoint + cameraNormal * markerSize * 0.1f,
                    $"{graph.Direction} · 3D接续");
            }
        }

        private static void DrawPlatforms(RopePathNetwork network)
        {
            foreach (RopePlatform platform in network.GetComponentsInChildren<RopePlatform>(true))
            {
                if (platform == null)
                {
                    continue;
                }

                Handles.color = platform.IsBound
                    ? new Color(0.35f, 1f, 0.8f, 0.9f)
                    : new Color(1f, 0.4f, 0.8f, 0.9f);
                Vector3 position = platform.transform.position;
                Handles.DrawWireDisc(
                    position,
                    Vector3.up,
                    HandleUtility.GetHandleSize(position) * 0.2f);
                Handles.Label(
                    position + Vector3.up * HandleUtility.GetHandleSize(position) * 0.18f,
                    platform.IsBound ? "平台 · 已绑定" : "平台 · 未绑定");
            }
        }

        private static string ShortId(string id)
        {
            return string.IsNullOrEmpty(id)
                ? "????"
                : id.Substring(0, Mathf.Min(6, id.Length));
        }
    }
}
