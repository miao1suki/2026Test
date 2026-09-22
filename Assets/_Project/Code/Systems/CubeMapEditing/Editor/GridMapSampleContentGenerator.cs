using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.CubeMapEditing.Editor
{
    internal static class GridMapSampleContentGenerator
    {
        private const string MaterialFolder =
            "Assets/_Project/Content/Materials/MapItems";

        [MenuItem("Tools/2026Test/网格地图/创建测试物品")]
        internal static void CreateSamples()
        {
            EnsureFolder(CubeMapWorkspacePaths.MapItemDataFolder);
            EnsureFolder(CubeMapWorkspacePaths.MapItemPrefabFolder);
            EnsureFolder(MaterialFolder);

            GridMapPalette palette = GridMapEditorService.EnsurePalette();
            List<GridMapItemDefinition> definitions = new List<GridMapItemDefinition>
            {
                CreateCubeItem(
                    "测试_单格地板",
                    "单格地板",
                    new Vector3(2f, 0.35f, 0.6f),
                    new Vector3(0f, -0.825f, 0f),
                    new Color(0.18f, 0.58f, 0.92f),
                    new Vector2Int(1, 1)),
                CreateCubeItem(
                    "测试_双格地板",
                    "双格地板",
                    new Vector3(4f, 0.35f, 0.6f),
                    new Vector3(0f, -0.825f, 0f),
                    new Color(0.12f, 0.78f, 0.64f),
                    new Vector2Int(2, 1)),
                CreateCubeItem(
                    "测试_双格墙",
                    "双格墙",
                    new Vector3(0.35f, 4f, 0.6f),
                    new Vector3(-0.825f, 0f, 0f),
                    new Color(0.58f, 0.38f, 0.92f),
                    new Vector2Int(1, 2)),
                CreateCubeItem(
                    "测试_方块障碍",
                    "方块障碍",
                    new Vector3(1.55f, 1.55f, 0.7f),
                    Vector3.zero,
                    new Color(0.95f, 0.52f, 0.14f),
                    new Vector2Int(1, 1)),
                CreateDiamondItem(
                    "测试_菱形障碍",
                    "菱形障碍",
                    new Color(0.92f, 0.22f, 0.3f)),
            };

            foreach (GridMapItemDefinition definition in definitions)
            {
                palette.Add(definition);
            }

            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = palette;
            EditorGUIUtility.PingObject(palette);
            Debug.Log($"Grid map sample content ready: {definitions.Count} items.");
        }

        private static GridMapItemDefinition CreateCubeItem(
            string assetName,
            string displayName,
            Vector3 visualScale,
            Vector3 visualOffset,
            Color color,
            Vector2Int footprint)
        {
            string prefabPath =
                $"{CubeMapWorkspacePaths.MapItemPrefabFolder}/{assetName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                GameObject root = new GameObject(assetName);
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = visualOffset;
                visual.transform.localScale = visualScale;
                visual.GetComponent<MeshRenderer>().sharedMaterial =
                    GetOrCreateMaterial(assetName, color);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
            }

            return GetOrCreateDefinition(
                assetName,
                displayName,
                prefab,
                footprint);
        }

        private static GridMapItemDefinition CreateDiamondItem(
            string assetName,
            string displayName,
            Color color)
        {
            string prefabPath =
                $"{CubeMapWorkspacePaths.MapItemPrefabFolder}/{assetName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                GameObject root = new GameObject(assetName);
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Diamond Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                visual.transform.localScale = new Vector3(1.05f, 1.05f, 0.7f);
                visual.GetComponent<MeshRenderer>().sharedMaterial =
                    GetOrCreateMaterial(assetName, color);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
            }

            return GetOrCreateDefinition(
                assetName,
                displayName,
                prefab,
                Vector2Int.one);
        }

        private static GridMapItemDefinition GetOrCreateDefinition(
            string assetName,
            string displayName,
            GameObject prefab,
            Vector2Int footprint)
        {
            string path =
                $"{CubeMapWorkspacePaths.MapItemDataFolder}/{assetName}.asset";
            GridMapItemDefinition definition =
                AssetDatabase.LoadAssetAtPath<GridMapItemDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<GridMapItemDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(
                displayName,
                prefab,
                footprint,
                Vector2.zero,
                true,
                true);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static Material GetOrCreateMaterial(string assetName, Color color)
        {
            string path = $"{MaterialFolder}/{assetName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            EditorUtility.SetDirty(material);
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
    }
}
