using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.PlatformPaths;

namespace Project.RopePaths.Editor
{
    internal static class RopePathEditorService
    {
        private const string RopeMaterialFolder = "Assets/_Project/Content/Materials/RopePaths";
        private const string RopeMaterialPath = RopeMaterialFolder + "/Rope_White.mat";

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
            EnsureSegmentVisuals(segment);
            Selection.activeGameObject = gameObject;
            MarkDirty(network, gameObject);
            return segment;
        }

        internal static void EnsureSegmentVisuals(RopeSegment segment)
        {
            if (segment == null)
            {
                return;
            }

            Material material = GetOrCreateRopeMaterial();
            if (segment.EnsureVisuals(material))
            {
                RopePathNetwork network = segment.GetComponentInParent<RopePathNetwork>();
                MarkDirty(network, segment.gameObject);
            }
        }

        [MenuItem("Tools/2026Test/绳子路径/创建白膜材质")]
        public static void CreateDefaultMaterialAsset()
        {
            GetOrCreateRopeMaterial();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Material GetOrCreateRopeMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RopeMaterialPath);
            if (material != null)
            {
                return material;
            }

            EnsureFolder("Assets/_Project/Content");
            EnsureFolder("Assets/_Project/Content/Materials");
            EnsureFolder(RopeMaterialFolder);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            material = new Material(shader)
            {
                name = "Rope_White"
            };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            AssetDatabase.CreateAsset(material, RopeMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
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
