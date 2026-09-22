using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.LadderPaths.Editor
{
    [InitializeOnLoad]
    internal static class LadderPathSceneDrawer
    {
        static LadderPathSceneDrawer()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            LadderPathNetwork network = LadderPathEditorService.FindActiveNetwork();
            if (network == null)
            {
                return;
            }

            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;
            Handles.zTest = CompareFunction.Always;
            DrawSegments(network);
            DrawConnections(network.BuildPath(network.EditorDirection), sceneView);
            Handles.color = previousColor;
            Handles.zTest = previousZTest;
        }

        private static void DrawSegments(LadderPathNetwork network)
        {
            LadderSegment selected = LadderPathEditorService.FindSelectedSegment();
            foreach (LadderSegment segment in network.Segments)
            {
                if (segment == null)
                {
                    continue;
                }

                LadderPathEditorService.EnsureSegmentVisuals(segment);
                bool isSelected = segment == selected;
                Vector3 bottom = segment.GetWorldEndpoint(LadderEndpoint.Bottom);
                Vector3 top = segment.GetWorldEndpoint(LadderEndpoint.Top);
                LadderFaceFamily family =
                    segment.GetVisibleFaceFamily(network.EditorDirection);
                Handles.color = isSelected
                    ? new Color(0.25f, 1f, 0.55f, 1f)
                    : new Color(0.2f, 0.8f, 0.95f, 0.9f);
                using (new Handles.DrawingScope(
                           Handles.color,
                           segment.transform.localToWorldMatrix))
                {
                    Handles.DrawWireCube(
                        Vector3.zero,
                        new Vector3(segment.Width, segment.Height, segment.Depth));
                }
                Handles.Label(
                    segment.transform.position,
                    $"梯子 · {(family == LadderFaceFamily.FrontBack ? "正面族" : "侧面族")}");
                DrawEndpointRay(segment, bottom, Vector3.down);
                DrawEndpointRay(segment, top, Vector3.up);

                if (isSelected)
                {
                    DrawTransformHandles(segment, network);
                }
                else if (Handles.Button(
                             segment.transform.position,
                             Quaternion.identity,
                             HandleUtility.GetHandleSize(segment.transform.position) * 0.1f,
                             HandleUtility.GetHandleSize(segment.transform.position) * 0.12f,
                             Handles.DotHandleCap))
                {
                    Selection.activeGameObject = segment.gameObject;
                    SceneView.RepaintAll();
                }
            }
        }

        private static void DrawTransformHandles(
            LadderSegment segment,
            LadderPathNetwork network)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 position = Handles.PositionHandle(
                segment.transform.position,
                segment.transform.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(segment.transform, "移动梯子");
                segment.transform.position = position;
                network.InvalidateCache();
                LadderPathEditorService.MarkDirty(network, segment.gameObject);
            }

            EditorGUI.BeginChangeCheck();
            Quaternion rotation = Handles.RotationHandle(
                segment.transform.rotation,
                segment.transform.position);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(segment.transform, "旋转梯子");
                segment.transform.rotation = rotation;
                network.InvalidateCache();
                LadderPathEditorService.MarkDirty(network, segment.gameObject);
            }
        }

        private static void DrawEndpointRay(
            LadderSegment segment,
            Vector3 endpoint,
            Vector3 localDirection)
        {
            Handles.color = new Color(0.15f, 0.95f, 0.75f, 0.9f);
            Vector3 outward = segment.transform.TransformDirection(localDirection);
            float length = Mathf.Max(0.65f, HandleUtility.GetHandleSize(endpoint) * 0.5f);
            Handles.DrawDottedLine(endpoint, endpoint + outward * length, 3f);
        }

        private static void DrawConnections(
            LadderPathGraph graph,
            SceneView sceneView)
        {
            Handles.color = new Color(0.2f, 1f, 0.62f, 1f);
            foreach (LadderPathConnection connection in graph.Connections)
            {
                Vector3 first = connection.First.Segment.GetWorldEndpoint(
                    connection.First.Endpoint);
                Vector3 second = connection.Second.Segment.GetWorldEndpoint(
                    connection.Second.Endpoint);
                Vector3 midpoint = Vector3.Lerp(first, second, 0.5f);
                float markerSize = HandleUtility.GetHandleSize(midpoint) * 0.14f;
                Handles.DrawDottedLine(first, second, 3f);
                if ((second - first).sqrMagnitude > 0.000001f)
                {
                    Handles.ArrowHandleCap(
                        0,
                        midpoint,
                        Quaternion.LookRotation((second - first).normalized),
                        markerSize * 1.3f,
                        EventType.Repaint);
                }

                Vector3 normal = sceneView.camera != null
                    ? sceneView.camera.transform.forward
                    : Vector3.forward;
                Handles.DrawWireDisc(midpoint, normal, markerSize);
                Handles.Label(
                    midpoint,
                    $"{graph.Direction} · 梯子3D接续");
            }
        }
    }
}
