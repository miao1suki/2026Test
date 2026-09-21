using System.Linq;
using NUnit.Framework;
using Project.CubeMapEditing.Editor;
using UnityEditor;
using UnityEngine;

namespace Project.CubeMapEditing.Tests
{
    public sealed class GridMapEditorTests
    {
        [Test]
        public void Workspace_CellSizeFollowsConfiguredFaceDimensions()
        {
            CubeMapWorkspaceDefinition workspace =
                ScriptableObject.CreateInstance<CubeMapWorkspaceDefinition>();
            workspace.Configure("TEST", 12f, 4f, 6, 2, string.Empty, string.Empty);

            Assert.That(workspace.CellSize.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(workspace.CellSize.y, Is.EqualTo(2f).Within(0.0001f));

            Object.DestroyImmediate(workspace);
        }

        [Test]
        public void ItemDefinition_StoresCellFootprintAndDefaults()
        {
            GridMapItemDefinition definition =
                ScriptableObject.CreateInstance<GridMapItemDefinition>();
            definition.Configure(
                "桥",
                new GameObject("Bridge Prefab"),
                new Vector2Int(3, 2),
                new Vector2(0.25f, -0.5f),
                true,
                false);

            Assert.That(definition.DisplayName, Is.EqualTo("桥"));
            Assert.That(definition.SizeInCells, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(definition.PivotOffset, Is.EqualTo(new Vector2(0.25f, -0.5f)));
            Assert.That(definition.AllowOverlapByDefault, Is.True);
            Assert.That(definition.SnapToGridByDefault, Is.False);

            Object.DestroyImmediate(definition.Prefab);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Placement_RotationSwapsFootprintAndPreservesTemporaryFlags()
        {
            GameObject objectToPlace = new GameObject("Placement");
            GridMapPlacement placement = objectToPlace.AddComponent<GridMapPlacement>();
            GridMapItemDefinition definition =
                ScriptableObject.CreateInstance<GridMapItemDefinition>();
            definition.Configure(
                "长物体",
                objectToPlace,
                new Vector2Int(3, 1),
                Vector2.zero,
                false,
                true);

            placement.Configure(
                definition,
                new Vector2Int(2, 4),
                1,
                false,
                true,
                new Vector2(2.5f, 1.5f));

            Assert.That(placement.AnchorCell, Is.EqualTo(new Vector2Int(2, 4)));
            Assert.That(placement.RotationSteps, Is.EqualTo(1));
            Assert.That(placement.RotatedSizeInCells, Is.EqualTo(new Vector2Int(1, 3)));
            Assert.That(placement.SnappedToGrid, Is.False);
            Assert.That(placement.AllowOverlap, Is.True);

            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(objectToPlace);
        }

        [Test]
        public void EditorGridMath_AlignsFootprintToWorkspaceCells()
        {
            CubeMapWorkspaceDefinition workspace =
                ScriptableObject.CreateInstance<CubeMapWorkspaceDefinition>();
            workspace.Configure("TEST", 12f, 4f, 6, 2, string.Empty, string.Empty);
            GameObject pieceObject = new GameObject("Piece");
            CubeMapPieceAuthoring piece = pieceObject.AddComponent<CubeMapPieceAuthoring>();
            GameObject faceObject = new GameObject("Face");
            faceObject.transform.SetParent(pieceObject.transform);
            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(faceObject.transform);
            GridMapPieceContext context = new GridMapPieceContext(
                piece,
                workspace,
                CubeMapFace.Front,
                faceObject.transform,
                contentObject.transform);

            Vector3 position = GridMapEditorService.GetPlacementLocalPosition(
                context,
                new Vector2Int(0, 0),
                new Vector2Int(1, 1),
                true,
                default);

            Assert.That(position.x, Is.EqualTo(-5f).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(-1f).Within(0.0001f));
            Object.DestroyImmediate(pieceObject);
            Object.DestroyImmediate(workspace);
        }

        [Test]
        public void SamplePalette_ContainsUsableTestPrefabs()
        {
            const string palettePath =
                "Assets/_Project/Content/Data/MapItems/LV001_GridMapPalette.asset";
            GridMapPalette palette =
                AssetDatabase.LoadAssetAtPath<GridMapPalette>(palettePath);

            Assert.That(palette, Is.Not.Null);
            Assert.That(palette.Items.Count, Is.GreaterThanOrEqualTo(5));

            string[] expectedNames =
            {
                "单格地板",
                "双格地板",
                "双格墙",
                "方块障碍",
                "菱形障碍",
            };

            foreach (string expectedName in expectedNames)
            {
                GridMapItemDefinition definition = palette.Items.FirstOrDefault(
                    item => item != null && item.DisplayName == expectedName);
                Assert.That(definition, Is.Not.Null, $"缺少测试物品：{expectedName}");
                Assert.That(definition.Prefab, Is.Not.Null, $"{expectedName} 未绑定预制体");
                Assert.That(
                    AssetDatabase.GetAssetPath(definition.Prefab),
                    Does.StartWith("Assets/_Project/Content/Prefabs/MapItems/"));
            }
        }
    }
}
