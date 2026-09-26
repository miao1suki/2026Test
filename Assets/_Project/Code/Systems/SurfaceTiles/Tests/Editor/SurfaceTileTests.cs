using NUnit.Framework;
using Project.SurfaceTiles.Editor;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.SurfaceTiles.Tests
{
    public sealed class SurfaceTileTests
    {
        [Test]
        public void GridSize_UsesWorldDimensionsForEveryFace()
        {
            GameObject target = new GameObject("Block");
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.size = new Vector3(2f, 3f, 4f);
            target.transform.localScale = new Vector3(2f, 1f, 0.5f);

            Assert.That(
                SurfaceTileGeometry.GetGridSize(
                    target.transform,
                    collider,
                    SurfaceTileFace.Front,
                    1f),
                Is.EqualTo(new Vector2Int(4, 3)));
            Assert.That(
                SurfaceTileGeometry.GetGridSize(
                    target.transform,
                    collider,
                    SurfaceTileFace.Right,
                    1f),
                Is.EqualTo(new Vector2Int(2, 3)));
            Assert.That(
                SurfaceTileGeometry.GetGridSize(
                    target.transform,
                    collider,
                    SurfaceTileFace.Top,
                    1f),
                Is.EqualTo(new Vector2Int(4, 2)));

            Object.DestroyImmediate(target);
        }

        [Test]
        public void CellLookup_MapsFaceCornersAndClampsFarEdge()
        {
            GameObject target = new GameObject("Block");
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 2f, 1f);
            SurfaceTileFaceBasis basis = SurfaceTileGeometry.GetLocalBasis(
                collider,
                SurfaceTileFace.Front);

            Assert.That(
                SurfaceTileGeometry.TryGetCell(
                    collider,
                    SurfaceTileFace.Front,
                    basis.Point(0.01f, 0.01f),
                    new Vector2Int(4, 2),
                    out Vector2Int first),
                Is.True);
            Assert.That(first, Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(
                SurfaceTileGeometry.TryGetCell(
                    collider,
                    SurfaceTileFace.Front,
                    basis.Point(1f, 1f),
                    new Vector2Int(4, 2),
                    out Vector2Int last),
                Is.True);
            Assert.That(last, Is.EqualTo(new Vector2Int(3, 1)));

            Object.DestroyImmediate(target);
        }

        [Test]
        public void Placement_ReplacesSameCellAndBuildsOneQuad()
        {
            Texture2D texture = new Texture2D(2, 2);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f),
                2f);
            SurfaceTilePalette palette =
                ScriptableObject.CreateInstance<SurfaceTilePalette>();
            palette.ReplaceTiles(new[] { sprite });
            GameObject target = new GameObject("Block");
            target.AddComponent<BoxCollider>();
            SurfaceTileBlock block = target.AddComponent<SurfaceTileBlock>();
            block.Configure(palette, 1f);
            string tileId = palette.Tiles[0].Id;

            block.SetTile(
                SurfaceTileFace.Front,
                Vector2Int.zero,
                tileId,
                0,
                false,
                false);
            block.SetTile(
                SurfaceTileFace.Front,
                Vector2Int.zero,
                tileId,
                1,
                true,
                false);
            Mesh mesh = SurfaceTileMeshBuilder.BuildCellMesh(block);

            Assert.That(block.Placements.Count, Is.EqualTo(1));
            Assert.That(block.Placements[0].QuarterTurns, Is.EqualTo(1));
            Assert.That(block.Placements[0].FlipX, Is.True);
            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(mesh.triangles.Length, Is.EqualTo(6));

            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(palette);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void UvTransform_RotatesAndFlipsDeterministically()
        {
            Vector2 rotated = SurfaceTileMeshBuilder.TransformTileUv(
                new Vector2(0f, 0f),
                1,
                false,
                false);
            Vector2 flipped = SurfaceTileMeshBuilder.TransformTileUv(
                new Vector2(0.25f, 0.75f),
                0,
                true,
                true);

            Assert.That(rotated, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(flipped, Is.EqualTo(new Vector2(0.75f, 0.25f)));
        }

        [Test]
        public void OneWayPlatform_IgnoresBelowAndAscendingButLandsFromAbove()
        {
            Bounds platform = new Bounds(Vector3.zero, new Vector3(4f, 1f, 2f));
            Bounds below = new Bounds(
                new Vector3(0f, -1f, 0f),
                new Vector3(1f, 1f, 1f));
            Bounds above = new Bounds(
                new Vector3(0f, 1.1f, 0f),
                new Vector3(1f, 1f, 1f));

            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                true, true, false, platform, below, -1f, 0.08f), Is.True);
            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                true, true, false, platform, above, -1f, 0.08f), Is.False);
            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                true, true, false, platform, above, 2f, 0.08f), Is.True);
        }

        [Test]
        public void OneWayPlatform_ReleasesSafelyAfterLeavingTwoDMode()
        {
            Bounds platform = new Bounds(Vector3.zero, new Vector3(4f, 1f, 2f));
            Bounds overlapping = new Bounds(
                new Vector3(0f, 0.25f, 0f),
                Vector3.one);
            Bounds clear = new Bounds(
                new Vector3(0f, 3f, 0f),
                Vector3.one);

            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                false, true, true, platform, overlapping, 0f, 0.08f), Is.True);
            Assert.That(ProjectedOneWayPlatform.ShouldIgnoreCollision(
                false, true, true, platform, clear, 0f, 0.08f), Is.False);
        }

        [Test]
        public void Bake_CreatesPersistentTextureMaterialAndMesh()
        {
            const string temporaryRoot = "Assets/__SurfaceTileBakeTest";
            const string generatedParent = "Assets/_Project/Generated";
            const string generatedRoot = generatedParent + "/SurfaceTiles";
            const string generatedFolder =
                generatedRoot + "/BakeTest";
            bool generatedParentExisted = AssetDatabase.IsValidFolder(generatedParent);
            bool generatedRootExisted = AssetDatabase.IsValidFolder(generatedRoot);
            try
            {
                if (!AssetDatabase.IsValidFolder(temporaryRoot))
                {
                    AssetDatabase.CreateFolder("Assets", "__SurfaceTileBakeTest");
                }

                Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                source.SetPixels(new[]
                {
                    Color.red, Color.green, Color.blue, Color.white
                });
                source.Apply();
                string texturePath = temporaryRoot + "/Tile.png";
                File.WriteAllBytes(Path.GetFullPath(texturePath), source.EncodeToPNG());
                Object.DestroyImmediate(source);
                AssetDatabase.ImportAsset(
                    texturePath,
                    ImportAssetOptions.ForceSynchronousImport);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(
                    texturePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);

                SurfaceTilePalette palette =
                    ScriptableObject.CreateInstance<SurfaceTilePalette>();
                palette.ReplaceTiles(new[] { sprite });
                string palettePath = temporaryRoot + "/Palette.asset";
                AssetDatabase.CreateAsset(palette, palettePath);
                AssetDatabase.SaveAssets();
                Scene scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
                Assert.That(EditorSceneManager.SaveScene(
                    scene,
                    temporaryRoot + "/BakeTest.unity"), Is.True);
                palette = AssetDatabase.LoadAssetAtPath<SurfaceTilePalette>(
                    palettePath);

                GameObject target = new GameObject("BakeBlock");
                target.AddComponent<BoxCollider>();
                SurfaceTileBlock block = target.AddComponent<SurfaceTileBlock>();
                block.Configure(palette, 1f);
                block.SetTile(
                    SurfaceTileFace.Front,
                    Vector2Int.zero,
                    palette.Tiles[0].Id,
                    0,
                    false,
                    false);

                Assert.That(
                    SurfaceTileAssetBaker.Bake(block, out string message),
                    Is.True,
                    message);
                Assert.That(block.BakeUpToDate, Is.True);
                Assert.That(AssetDatabase.Contains(block.BakedMesh), Is.True);
                Assert.That(AssetDatabase.Contains(block.BakedMaterial), Is.True);
                Assert.That(block.BakedMaterial.mainTexture, Is.Not.Null);
                Assert.That(
                    AssetDatabase.IsValidFolder(generatedFolder),
                    Is.True);
                target.transform.localScale = new Vector3(2f, 1f, 1f);
                Assert.That(block.BakeUpToDate, Is.False);
            }
            finally
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
                AssetDatabase.DeleteAsset(temporaryRoot);
                AssetDatabase.DeleteAsset(generatedFolder);
                if (!generatedRootExisted)
                {
                    AssetDatabase.DeleteAsset(generatedRoot);
                }

                if (!generatedParentExisted)
                {
                    AssetDatabase.DeleteAsset(generatedParent);
                }

                AssetDatabase.Refresh();
            }
        }
    }
}
