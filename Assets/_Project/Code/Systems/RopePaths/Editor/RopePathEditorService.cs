using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.RopePaths.Editor
{
    internal static class RopePathEditorService
    {
        internal static RopePathNetwork FindActiveNetwork()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RopePathNetwork network = root.GetComponentInChildren<RopePathNetwork>(true);
                if (network != null)
                {
                    return network;
                }
            }

            return null;
        }

        internal static RopeSegment FindSelectedSegment()
        {
            if (Selection.activeGameObject == null)
            {
                return null;
            }

            return Selection.activeGameObject.GetComponentInParent<RopeSegment>();
        }

        internal static RopePlatform FindSelectedPlatform()
        {
            if (Selection.activeGameObject == null)
            {
                return null;
            }

            return Selection.activeGameObject.GetComponentInParent<RopePlatform>();
        }

        internal static RopePathNetwork CreateNetwork()
        {
            GameObject gameObject = new GameObject("RopePathNetwork");
            Undo.RegisterCreatedObjectUndo(gameObject, "创建绳子路径网络");
            RopePathNetwork network = gameObject.AddComponent<RopePathNetwork>();
            Selection.activeGameObject = gameObject;
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            return network;
        }

        internal static RopeSegment CreateSegment(RopePathNetwork network)
        {
            if (network == null)
            {
                return null;
            }

            GameObject gameObject = new GameObject("RopeSegment");
            gameObject.transform.SetParent(network.transform, false);
            Undo.RegisterCreatedObjectUndo(gameObject, "创建绳子段");
            RopeSegment segment = gameObject.AddComponent<RopeSegment>();
            Selection.activeGameObject = gameObject;
            MarkDirty(network, gameObject);
            return segment;
        }

        internal static RopePlatform CreatePlatform(RopePathNetwork network)
        {
            if (network == null)
            {
                return null;
            }

            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = "RopePlatform";
            gameObject.transform.SetParent(network.transform, false);
            gameObject.transform.localScale = new Vector3(0.8f, 0.25f, 0.8f);
            Undo.RegisterCreatedObjectUndo(gameObject, "创建绳索平台");
            RopePlatform platform = gameObject.AddComponent<RopePlatform>();
            Selection.activeGameObject = gameObject;
            MarkDirty(network, gameObject);
            return platform;
        }

        internal static bool SnapSelectedEndpoint(
            RopePathNetwork network,
            RopeProjectionDirection direction,
            RopeEndpoint endpoint)
        {
            return SnapSelectedEndpoint(network, direction, endpoint, true);
        }

        internal static bool SnapSelectedEndpointWorld(
            RopePathNetwork network,
            RopeProjectionDirection direction,
            RopeEndpoint endpoint)
        {
            return SnapSelectedEndpoint(network, direction, endpoint, false);
        }

        private static bool SnapSelectedEndpoint(
            RopePathNetwork network,
            RopeProjectionDirection direction,
            RopeEndpoint endpoint,
            bool preserveDepth)
        {
            RopeSegment selected = FindSelectedSegment();
            if (network == null || selected == null)
            {
                return false;
            }

            Vector3 selectedWorld = selected.GetWorldEndpoint(endpoint);
            Vector2 selectedProjected = RopeProjectionUtility.Project(selectedWorld, direction);
            RopeSegment bestSegment = null;
            RopeEndpoint bestEndpoint = RopeEndpoint.A;
            float bestDistance = float.MaxValue;
            foreach (RopeSegment segment in network.Segments)
            {
                if (segment == null || segment == selected)
                {
                    continue;
                }

                for (int index = 0; index < 2; index++)
                {
                    RopeEndpoint candidateEndpoint = (RopeEndpoint)index;
                    Vector2 candidate = RopeProjectionUtility.Project(
                        segment.GetWorldEndpoint(candidateEndpoint),
                        direction);
                    float distance = Vector2.Distance(selectedProjected, candidate);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestSegment = segment;
                        bestEndpoint = candidateEndpoint;
                    }
                }
            }

            if (bestSegment == null || bestDistance > network.ConnectionTolerance * 4f)
            {
                return false;
            }

            Undo.RecordObject(selected, "投影吸附绳子端点");
            Vector2 targetProjected = RopeProjectionUtility.Project(
                bestSegment.GetWorldEndpoint(bestEndpoint),
                direction);
            float depth = preserveDepth
                ? RopeProjectionUtility.Depth(selectedWorld, direction)
                : RopeProjectionUtility.Depth(
                    bestSegment.GetWorldEndpoint(bestEndpoint),
                    direction);
            selected.SetWorldEndpoint(
                endpoint,
                RopeProjectionUtility.Unproject(targetProjected, depth, direction));
            network.InvalidateCache();
            MarkDirty(network, selected.gameObject);
            SceneView.RepaintAll();
            return true;
        }

        internal static bool BindSelectedPlatform(
            RopePathNetwork network,
            RopeProjectionDirection direction)
        {
            RopePlatform platform = FindSelectedPlatform();
            if (network == null || platform == null)
            {
                return false;
            }

            Vector2 platformProjected = RopeProjectionUtility.Project(
                platform.transform.position,
                direction);
            RopeSegment bestSegment = null;
            RopeEndpoint bestEndpoint = RopeEndpoint.A;
            float bestDistance = float.MaxValue;
            foreach (RopeSegment segment in network.Segments)
            {
                if (segment == null)
                {
                    continue;
                }

                for (int index = 0; index < 2; index++)
                {
                    RopeEndpoint endpoint = (RopeEndpoint)index;
                    float distance = Vector2.Distance(
                        platformProjected,
                        RopeProjectionUtility.Project(
                            segment.GetWorldEndpoint(endpoint),
                            direction));
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestSegment = segment;
                        bestEndpoint = endpoint;
                    }
                }
            }

            if (bestSegment == null || bestDistance > network.ConnectionTolerance * 6f)
            {
                return false;
            }

            Undo.RecordObject(platform, "绑定绳索平台停靠点");
            platform.Bind(network, bestSegment, bestEndpoint);
            platform.SnapTransformToBinding();
            MarkDirty(network, platform.gameObject);
            SceneView.RepaintAll();
            return true;
        }

        internal static void MarkDirty(RopePathNetwork network, GameObject gameObject)
        {
            if (gameObject != null)
            {
                EditorUtility.SetDirty(gameObject);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }

            if (network != null)
            {
                EditorUtility.SetDirty(network);
            }
        }
    }
}
