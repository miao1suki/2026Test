using System.Collections.Generic;
using Project.LadderPaths;
using Project.PlatformPaths;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.RopePaths.Editor
{
    /// <summary>
    /// Designer-facing creation helpers for ropes, ladders and rope platforms.
    /// All commands operate directly in the active scene and are Undo-safe.
    /// </summary>
    internal static class PathMechanismAuthoringService
    {
        private const string WhiteMaterialPath =
            "Assets/_Project/Content/Materials/RopePaths/Rope_White.mat";

        internal static Vector3 GetCreationPosition()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null && selected.scene == SceneManager.GetActiveScene())
            {
                return selected.transform.position;
            }

            return SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.pivot
                : Vector3.zero;
        }

        internal static RopeSegment CreateRope()
        {
            Vector3 position = GetCreationPosition();
            RopePathNetwork network = FindOrCreateRopeNetwork();
            return CreateRopeAt(network, position);
        }

        private static RopeSegment CreateRopeAt(
            RopePathNetwork network,
            Vector3 position)
        {
            RopeSegment segment = RopePathEditorService.CreateSegment(network);
            if (segment == null)
            {
                return null;
            }

            Undo.RecordObject(segment.transform, "放置绳子");
            segment.transform.position = position;
            segment.name = GetUniqueChildName(network.transform, "Rope");
            network.InvalidateCache();
            MarkSceneDirty(segment.gameObject);
            Frame(segment.gameObject);
            return segment;
        }

        internal static RopeSegment ContinueRope(RopeEndpoint sourceEndpoint)
        {
            RopeSegment source = FindSelectedRope();
            if (source == null)
            {
                return CreateRope();
            }

            RopePathNetwork network = source.GetComponentInParent<RopePathNetwork>() ??
                                      FindOrCreateRopeNetwork();
            RopeSegment segment = RopePathEditorService.CreateSegment(network);
            Vector3 start = source.GetWorldEndpoint(sourceEndpoint);
            Vector3 direction = source.GetWorldDirection(sourceEndpoint);
            float length = Vector3.Distance(
                source.GetWorldEndpoint(RopeEndpoint.A),
                source.GetWorldEndpoint(RopeEndpoint.B));
            if (direction.sqrMagnitude < 0.000001f)
            {
                direction = Vector3.right;
            }

            Undo.RecordObject(segment.transform, "续接绳子");
            Undo.RecordObject(segment, "续接绳子");
            segment.transform.position = start;
            segment.transform.rotation = Quaternion.identity;
            segment.Configure(
                Vector3.zero,
                direction.normalized * Mathf.Max(0.5f, length),
                source.RopeWidth,
                source.EndpointRadius);
            segment.name = GetUniqueChildName(network.transform, "Rope");
            RopePathEditorState.ActiveEndpoint = RopeEndpoint.B;
            network.InvalidateCache();
            MarkSceneDirty(segment.gameObject);
            Frame(segment.gameObject);
            return segment;
        }

        internal static LadderSegment CreateLadder()
        {
            Vector3 position = GetCreationPosition();
            LadderPathNetwork network = FindOrCreateLadderNetwork();
            LadderSegment segment = CreateLadderInternal(
                network,
                position,
                Quaternion.identity,
                2f,
                2f,
                0.25f,
                5);
            Frame(segment.gameObject);
            return segment;
        }

        internal static LadderSegment ContinueLadder(bool above)
        {
            LadderSegment source = FindSelectedLadder();
            if (source == null)
            {
                return CreateLadder();
            }

            LadderPathNetwork network = source.GetComponentInParent<LadderPathNetwork>() ??
                                        FindOrCreateLadderNetwork();
            Vector3 sourceEndpoint = source.GetWorldEndpoint(
                above ? LadderEndpoint.Top : LadderEndpoint.Bottom);
            float direction = above ? 1f : -1f;
            Vector3 center = sourceEndpoint +
                             source.transform.up * (source.Height * 0.5f * direction);
            LadderSegment segment = CreateLadderInternal(
                network,
                center,
                source.transform.rotation,
                source.Width,
                source.Height,
                source.Depth,
                source.RungCount);
            Frame(segment.gameObject);
            return segment;
        }

        internal static RopePlatform CreateBoundPlatform()
        {
            Vector3 position = GetCreationPosition();
            RopePathNetwork network = FindOrCreateRopeNetwork();
            RopeSegment selectedRope = FindSelectedRope();
            RopeEndpoint endpoint = RopePathEditorState.ActiveEndpoint;
            if (selectedRope == null)
            {
                selectedRope = FindNearestRopeEndpoint(
                    network,
                    position,
                    out endpoint);
            }

            if (selectedRope == null)
            {
                selectedRope = CreateRopeAt(network, position);
                endpoint = RopeEndpoint.B;
            }

            RopePlatform platform = RopePathEditorService.CreatePlatform(network);
            platform.name = GetUniqueChildName(network.transform, "RopePlatform");
            Undo.RecordObject(platform, "创建并绑定绳索平台");
            platform.Bind(network, selectedRope, endpoint);
            platform.SnapTransformToBinding();
            EnsurePlatformCollider(platform.gameObject);
            MarkSceneDirty(platform.gameObject);
            Frame(platform.gameObject);
            return platform;
        }

        internal static bool BindSelectedPlatformToNearestEndpoint()
        {
            RopePlatform platform = FindSelectedPlatform();
            RopePathNetwork network = FindOrCreateRopeNetwork();
            if (platform == null)
            {
                return false;
            }

            RopeSegment segment = FindNearestRopeEndpoint(
                network,
                platform.transform.position,
                out RopeEndpoint endpoint);
            if (segment == null)
            {
                return false;
            }

            Undo.RecordObject(platform, "绑定平台到最近绳端");
            platform.Bind(network, segment, endpoint);
            platform.SnapTransformToBinding();
            EnsurePlatformCollider(platform.gameObject);
            MarkSceneDirty(platform.gameObject);
            return true;
        }

        internal static void SetPreviewDirection(RopeProjectionDirection direction)
        {
            RopePathNetwork ropeNetwork = RopePathEditorService.FindActiveNetwork();
            if (ropeNetwork != null)
            {
                Undo.RecordObject(ropeNetwork, "修改路径预览方向");
                ropeNetwork.EditorDirection = direction;
                EditorUtility.SetDirty(ropeNetwork);
            }

            LadderPathNetwork ladderNetwork = FindActiveLadderNetwork();
            if (ladderNetwork != null)
            {
                Undo.RecordObject(ladderNetwork, "修改路径预览方向");
                ladderNetwork.EditorDirection = (LadderProjectionDirection)direction;
                EditorUtility.SetDirty(ladderNetwork);
            }

            SceneView.RepaintAll();
        }

        internal static RepairReport RepairActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            List<RopeSegment> ropes = FindInScene<RopeSegment>(scene);
            List<LadderSegment> ladders = FindInScene<LadderSegment>(scene);
            List<RopePlatform> platforms = FindInScene<RopePlatform>(scene);
            RopePathNetwork ropeNetwork = ropes.Count > 0 || platforms.Count > 0
                ? FindOrCreateRopeNetwork()
                : RopePathEditorService.FindActiveNetwork();
            LadderPathNetwork ladderNetwork = ladders.Count > 0
                ? FindOrCreateLadderNetwork()
                : FindActiveLadderNetwork();
            int repaired = 0;
            int unboundPlatforms = 0;

            Material material = AssetDatabase.LoadAssetAtPath<Material>(WhiteMaterialPath);
            for (int index = 0; index < ropes.Count; index++)
            {
                RopeSegment rope = ropes[index];
                if (rope.GetComponentInParent<RopePathNetwork>() == null)
                {
                    Undo.SetTransformParent(rope.transform, ropeNetwork.transform, "收纳绳子");
                    repaired++;
                }

                if (rope.EnsureVisuals(material))
                {
                    repaired++;
                }
            }

            for (int index = 0; index < ladders.Count; index++)
            {
                LadderSegment ladder = ladders[index];
                if (ladder.GetComponentInParent<LadderPathNetwork>() == null)
                {
                    Undo.SetTransformParent(
                        ladder.transform,
                        ladderNetwork.transform,
                        "收纳梯子");
                    repaired++;
                }

                if (ladder.EnsureVisuals(material))
                {
                    repaired++;
                }
            }

            for (int index = 0; index < platforms.Count; index++)
            {
                RopePlatform platform = platforms[index];
                if (platform.GetComponentInParent<RopePathNetwork>() == null &&
                    ropeNetwork != null)
                {
                    Undo.SetTransformParent(
                        platform.transform,
                        ropeNetwork.transform,
                        "收纳绳索平台");
                    repaired++;
                }

                if (EnsurePlatformCollider(platform.gameObject))
                {
                    repaired++;
                }

                if (!platform.IsBound)
                {
                    unboundPlatforms++;
                }
            }

            ropeNetwork?.RebuildPaths();
            ladderNetwork?.RefreshSegments();
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            return new RepairReport(
                ropes.Count,
                ladders.Count,
                platforms.Count,
                repaired,
                unboundPlatforms);
        }

        internal static RopeSegment FindSelectedRope()
        {
            return Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<RopeSegment>()
                : null;
        }

        internal static LadderSegment FindSelectedLadder()
        {
            return Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<LadderSegment>()
                : null;
        }

        internal static RopePlatform FindSelectedPlatform()
        {
            return Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<RopePlatform>()
                : null;
        }

        internal static LadderPathNetwork FindActiveLadderNetwork()
        {
            List<LadderPathNetwork> values =
                FindInScene<LadderPathNetwork>(SceneManager.GetActiveScene());
            return values.Count > 0 ? values[0] : null;
        }

        private static RopePathNetwork FindOrCreateRopeNetwork()
        {
            RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
            return network != null ? network : RopePathEditorService.CreateNetwork();
        }

        private static LadderPathNetwork FindOrCreateLadderNetwork()
        {
            LadderPathNetwork network = FindActiveLadderNetwork();
            if (network != null)
            {
                return network;
            }

            GameObject gameObject = new GameObject("LadderPathNetwork");
            Undo.RegisterCreatedObjectUndo(gameObject, "创建梯子路径网络");
            network = gameObject.AddComponent<LadderPathNetwork>();
            MarkSceneDirty(gameObject);
            return network;
        }

        private static LadderSegment CreateLadderInternal(
            LadderPathNetwork network,
            Vector3 position,
            Quaternion rotation,
            float width,
            float height,
            float depth,
            int rungCount)
        {
            GameObject gameObject = new GameObject(
                GetUniqueChildName(network.transform, "Ladder"));
            gameObject.transform.SetParent(network.transform, false);
            gameObject.transform.SetPositionAndRotation(position, rotation);
            Undo.RegisterCreatedObjectUndo(gameObject, "创建梯子");
            LadderSegment segment = gameObject.AddComponent<LadderSegment>();
            segment.Configure(width, height, depth, rungCount);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(WhiteMaterialPath);
            segment.EnsureVisuals(material);
            network.RefreshSegments();
            Selection.activeGameObject = gameObject;
            MarkSceneDirty(gameObject);
            return segment;
        }

        private static RopeSegment FindNearestRopeEndpoint(
            RopePathNetwork network,
            Vector3 worldPosition,
            out RopeEndpoint endpoint)
        {
            endpoint = RopeEndpoint.A;
            RopeSegment best = null;
            float bestDistance = float.MaxValue;
            if (network == null)
            {
                return null;
            }

            foreach (RopeSegment segment in network.Segments)
            {
                if (segment == null)
                {
                    continue;
                }

                for (int index = 0; index < 2; index++)
                {
                    RopeEndpoint candidate = (RopeEndpoint)index;
                    float distance = Vector3.SqrMagnitude(
                        segment.GetWorldEndpoint(candidate) - worldPosition);
                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    bestDistance = distance;
                    best = segment;
                    endpoint = candidate;
                }
            }

            return best;
        }

        private static bool EnsurePlatformCollider(GameObject gameObject)
        {
            if (gameObject.GetComponent<Collider>() != null)
            {
                return false;
            }

            Undo.AddComponent<BoxCollider>(gameObject);
            return true;
        }

        private static List<T> FindInScene<T>(Scene scene) where T : Component
        {
            List<T> result = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                result.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return result;
        }

        private static string GetUniqueChildName(Transform parent, string prefix)
        {
            int index = 1;
            string candidate;
            do
            {
                candidate = $"{prefix}_{index:00}";
                index++;
            }
            while (parent.Find(candidate) != null);

            return candidate;
        }

        private static void MarkSceneDirty(GameObject gameObject)
        {
            EditorUtility.SetDirty(gameObject);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        private static void Frame(GameObject gameObject)
        {
            Selection.activeGameObject = gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            SceneView.RepaintAll();
        }

        internal readonly struct RepairReport
        {
            internal RepairReport(
                int ropes,
                int ladders,
                int platforms,
                int repaired,
                int unboundPlatforms)
            {
                Ropes = ropes;
                Ladders = ladders;
                Platforms = platforms;
                Repaired = repaired;
                UnboundPlatforms = unboundPlatforms;
            }

            internal int Ropes { get; }
            internal int Ladders { get; }
            internal int Platforms { get; }
            internal int Repaired { get; }
            internal int UnboundPlatforms { get; }
        }
    }
}
