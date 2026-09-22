using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.LadderPaths.Editor
{
    internal static class LadderPathEditorService
    {
        private const string WhiteMaterialPath =
            "Assets/_Project/Content/Materials/RopePaths/Rope_White.mat";

        internal static LadderPathNetwork FindActiveNetwork()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                LadderPathNetwork network =
                    root.GetComponentInChildren<LadderPathNetwork>(true);
                if (network != null)
                {
                    return network;
                }
            }

            return null;
        }

        internal static LadderSegment FindSelectedSegment()
        {
            return Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<LadderSegment>()
                : null;
        }

        internal static LadderPathNetwork CreateNetwork()
        {
            GameObject gameObject = new GameObject("LadderPathNetwork");
            Undo.RegisterCreatedObjectUndo(gameObject, "创建梯子路径网络");
            LadderPathNetwork network = gameObject.AddComponent<LadderPathNetwork>();
            Selection.activeGameObject = gameObject;
            MarkDirty(network, gameObject);
            return network;
        }

        internal static LadderSegment CreateSegment(LadderPathNetwork network)
        {
            if (network == null)
            {
                return null;
            }

            GameObject gameObject = new GameObject("LadderSegment");
            gameObject.transform.SetParent(network.transform, false);
            Undo.RegisterCreatedObjectUndo(gameObject, "创建梯子段");
            LadderSegment segment = gameObject.AddComponent<LadderSegment>();
            EnsureSegmentVisuals(segment);
            network.RefreshSegments();
            Selection.activeGameObject = gameObject;
            MarkDirty(network, gameObject);
            return segment;
        }

        internal static void EnsureSegmentVisuals(LadderSegment segment)
        {
            if (segment == null)
            {
                return;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(WhiteMaterialPath);
            if (segment.EnsureVisuals(material))
            {
                MarkDirty(segment.GetComponentInParent<LadderPathNetwork>(), segment.gameObject);
            }
        }

        internal static bool SnapSelectedEndpoint(
            LadderPathNetwork network,
            LadderProjectionDirection direction,
            LadderEndpoint endpoint,
            bool preserveDepth)
        {
            LadderSegment selected = FindSelectedSegment();
            if (network == null || selected == null)
            {
                return false;
            }

            Vector3 selectedWorld = selected.GetWorldEndpoint(endpoint);
            Vector2 selectedProjected = LadderProjectionUtility.Project(
                selectedWorld,
                direction);
            LadderFaceFamily selectedFamily =
                selected.GetVisibleFaceFamily(direction);
            LadderEndpoint requiredEndpoint = endpoint == LadderEndpoint.Bottom
                ? LadderEndpoint.Top
                : LadderEndpoint.Bottom;
            LadderSegment bestSegment = null;
            float bestDistance = float.MaxValue;
            foreach (LadderSegment segment in network.Segments)
            {
                if (segment == null ||
                    segment == selected ||
                    segment.GetVisibleFaceFamily(direction) != selectedFamily)
                {
                    continue;
                }

                Vector2 candidate = LadderProjectionUtility.Project(
                    segment.GetWorldEndpoint(requiredEndpoint),
                    direction);
                float distance = Vector2.Distance(selectedProjected, candidate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSegment = segment;
                }
            }

            if (bestSegment == null ||
                bestDistance > network.ConnectionTolerance * 4f)
            {
                return false;
            }

            Vector3 targetWorld = bestSegment.GetWorldEndpoint(requiredEndpoint);
            Vector2 targetProjected = LadderProjectionUtility.Project(targetWorld, direction);
            float depth = preserveDepth
                ? LadderProjectionUtility.Depth(selectedWorld, direction)
                : LadderProjectionUtility.Depth(targetWorld, direction);
            Vector3 snappedWorld = LadderProjectionUtility.Unproject(
                targetProjected,
                depth,
                direction);
            Undo.RecordObject(selected.transform, "吸附梯子接续点");
            selected.transform.position += snappedWorld - selectedWorld;
            network.InvalidateCache();
            MarkDirty(network, selected.gameObject);
            SceneView.RepaintAll();
            return true;
        }

        internal static void MarkDirty(
            LadderPathNetwork network,
            GameObject gameObject)
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
