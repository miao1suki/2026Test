using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.SurfaceTiles.Editor
{
    internal static class SurfaceTileAssetBaker
    {
        private const string GeneratedRoot =
            "Assets/_Project/Generated/SurfaceTiles";
        private const string SurfaceShaderName =
            "2026Test/Surface Tiles/Unlit Cutout";
        private const string BakeShaderName =
            "Hidden/2026Test/Surface Tile Bake";
        private const int MaximumTextureSize = 8192;

        internal static bool Bake(SurfaceTileBlock block, out string message)
        {
            message = string.Empty;
            if (block == null || block.Palette == null)
            {
                message = "请先选择一个可贴画方块并指定瓦片库。";
                return false;
            }

            if (!block.Palette.UsesSingleTexture())
            {
                message = "当前瓦片库包含多张源图片，请拆成多个瓦片库。";
                return false;
            }

            if (!TryGetTilePixelSize(block.Palette, out Vector2Int tilePixels))
            {
                message = "瓦片库为空，或瓦片像素尺寸不一致。";
                return false;
            }

            BlockLayout layout = BuildLayout(block, tilePixels);
            if (layout.Width <= 0 || layout.Height <= 0 ||
                layout.Width > MaximumTextureSize || layout.Height > MaximumTextureSize)
            {
                message = $"合成贴图尺寸 {layout.Width}×{layout.Height} 超出限制 {MaximumTextureSize}。";
                return false;
            }

            Shader bakeShader = Shader.Find(BakeShaderName);
            Shader surfaceShader = Shader.Find(SurfaceShaderName);
            if (bakeShader == null || surfaceShader == null)
            {
                message = "表面瓦片 Shader 尚未编译完成，请等待 Unity 编译后重试。";
                return false;
            }

            EnsureUniqueId(block);
            string sceneName = string.IsNullOrWhiteSpace(block.gameObject.scene.name)
                ? "UnsavedScene"
                : Sanitize(block.gameObject.scene.name);
            string folder = EnsureFolder(GeneratedRoot, sceneName);
            string prefix = Sanitize(block.name) + "_" + block.BlockId.Substring(0, 8);
            string texturePath = $"{folder}/{prefix}_Tiles.png";
            string materialPath = $"{folder}/{prefix}_Tiles.mat";
            string meshPath = $"{folder}/{prefix}_Tiles.asset";

            Texture2D atlas = RenderAtlas(block, layout, bakeShader);
            if (atlas == null)
            {
                message = "合成贴图失败。";
                return false;
            }

            byte[] png = atlas.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(atlas);
            File.WriteAllBytes(Path.GetFullPath(texturePath), png);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureTextureImporter(texturePath);
            Texture2D importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(surfaceShader)
                {
                    name = prefix + "_Tiles"
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = surfaceShader;
            }

            material.SetTexture("_BaseMap", importedTexture);
            material.mainTexture = importedTexture;
            material.SetFloat("_Cutoff", 0.1f);
            EditorUtility.SetDirty(material);

            Dictionary<SurfaceTileFace, Rect> uvRects = layout.NormalizedRects();
            Mesh generated = SurfaceTileMeshBuilder.BuildBakedMesh(block, uvRects);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = generated;
                AssetDatabase.CreateAsset(mesh, meshPath);
            }
            else
            {
                EditorUtility.CopySerialized(generated, mesh);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(mesh);
            }

            SurfaceTileMeshBuilder.EnsureOutput(block);
            Undo.RecordObject(block, "烘焙表面瓦片");
            Undo.RecordObject(block.OutputFilter, "烘焙表面瓦片");
            Undo.RecordObject(block.OutputRenderer, "烘焙表面瓦片");
            block.BindBake(mesh, material);
            EditorUtility.SetDirty(block);
            EditorSceneManager.MarkSceneDirty(block.gameObject.scene);
            AssetDatabase.SaveAssets();
            message = $"已保存：{texturePath}";
            return true;
        }

        private static Texture2D RenderAtlas(
            SurfaceTileBlock block,
            BlockLayout layout,
            Shader shader)
        {
            RenderTexture target = RenderTexture.GetTemporary(
                layout.Width,
                layout.Height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Material material = new Material(shader);
            try
            {
                RenderTexture.active = target;
                GL.Clear(true, true, Color.clear);
                GL.PushMatrix();
                GL.LoadPixelMatrix(0f, layout.Width, 0f, layout.Height);
                IReadOnlyList<SurfaceTilePlacement> placements = block.Placements;
                for (int index = 0; index < placements.Count; index++)
                {
                    SurfaceTilePlacement placement = placements[index];
                    if (!block.Palette.TryGet(
                            placement.TileId,
                            out SurfaceTilePalette.Entry entry) ||
                        entry.Sprite == null ||
                        !layout.FaceRects.TryGetValue(
                            placement.Face,
                            out RectInt faceRect))
                    {
                        continue;
                    }

                    Vector2Int grid = block.GetGridSize(placement.Face);
                    if (placement.Cell.x < 0 || placement.Cell.y < 0 ||
                        placement.Cell.x >= grid.x || placement.Cell.y >= grid.y)
                    {
                        continue;
                    }

                    Sprite sprite = entry.Sprite;
                    material.SetTexture("_MainTex", sprite.texture);
                    material.SetPass(0);
                    Rect spriteUv = SurfaceTileMeshBuilder.SpriteUv(sprite);
                    float x0 = faceRect.x + placement.Cell.x * layout.TilePixels.x;
                    float y0 = faceRect.y + placement.Cell.y * layout.TilePixels.y;
                    float x1 = x0 + layout.TilePixels.x;
                    float y1 = y0 + layout.TilePixels.y;
                    Vector2[] logical =
                    {
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f)
                    };
                    Vector2[] output =
                    {
                        new Vector2(x0, y0),
                        new Vector2(x1, y0),
                        new Vector2(x1, y1),
                        new Vector2(x0, y1)
                    };
                    GL.Begin(GL.QUADS);
                    for (int corner = 0; corner < 4; corner++)
                    {
                        Vector2 tileUv = SurfaceTileMeshBuilder.TransformTileUv(
                            logical[corner],
                            placement.QuarterTurns,
                            placement.FlipX,
                            placement.FlipY);
                        GL.TexCoord2(
                            spriteUv.x + tileUv.x * spriteUv.width,
                            spriteUv.y + tileUv.y * spriteUv.height);
                        GL.Vertex3(output[corner].x, output[corner].y, 0f);
                    }
                    GL.End();
                }

                GL.PopMatrix();
                Texture2D texture = new Texture2D(
                    layout.Width,
                    layout.Height,
                    TextureFormat.RGBA32,
                    false,
                    false);
                texture.ReadPixels(new Rect(0f, 0f, layout.Width, layout.Height), 0, 0);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        private static BlockLayout BuildLayout(
            SurfaceTileBlock block,
            Vector2Int tilePixels)
        {
            Vector2Int front = block.GetGridSize(SurfaceTileFace.Front);
            Vector2Int right = block.GetGridSize(SurfaceTileFace.Right);
            Vector2Int top = block.GetGridSize(SurfaceTileFace.Top);
            int sideWidthCells = front.x * 2 + right.x * 2;
            int topWidthCells = top.x * 2;
            int widthCells = Mathf.Max(sideWidthCells, topWidthCells);
            int sideHeightCells = front.y;
            int topHeightCells = top.y;
            BlockLayout layout = new BlockLayout(
                widthCells * tilePixels.x,
                (sideHeightCells + topHeightCells) * tilePixels.y,
                tilePixels);
            int x = 0;
            AddFace(layout, SurfaceTileFace.Front, block, ref x, 0);
            AddFace(layout, SurfaceTileFace.Right, block, ref x, 0);
            AddFace(layout, SurfaceTileFace.Back, block, ref x, 0);
            AddFace(layout, SurfaceTileFace.Left, block, ref x, 0);
            x = 0;
            int upperY = sideHeightCells * tilePixels.y;
            AddFace(layout, SurfaceTileFace.Top, block, ref x, upperY);
            AddFace(layout, SurfaceTileFace.Bottom, block, ref x, upperY);
            return layout;
        }

        private static void AddFace(
            BlockLayout layout,
            SurfaceTileFace face,
            SurfaceTileBlock block,
            ref int x,
            int y)
        {
            Vector2Int grid = block.GetGridSize(face);
            RectInt rect = new RectInt(
                x,
                y,
                grid.x * layout.TilePixels.x,
                grid.y * layout.TilePixels.y);
            layout.FaceRects[face] = rect;
            x += rect.width;
        }

        private static bool TryGetTilePixelSize(
            SurfaceTilePalette palette,
            out Vector2Int size)
        {
            size = default;
            IReadOnlyList<SurfaceTilePalette.Entry> tiles = palette.Tiles;
            for (int index = 0; index < tiles.Count; index++)
            {
                Sprite sprite = tiles[index]?.Sprite;
                if (sprite == null)
                {
                    continue;
                }

                Vector2Int candidate = new Vector2Int(
                    Mathf.RoundToInt(sprite.textureRect.width),
                    Mathf.RoundToInt(sprite.textureRect.height));
                if (size == default)
                {
                    size = candidate;
                }
                else if (size != candidate)
                {
                    return false;
                }
            }

            return size.x > 0 && size.y > 0;
        }

        private static void EnsureUniqueId(SurfaceTileBlock block)
        {
            block.EnsureBlockId();
            SurfaceTileBlock[] all = UnityEngine.Object.FindObjectsByType<SurfaceTileBlock>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < all.Length; index++)
            {
                if (all[index] != block && all[index].BlockId == block.BlockId)
                {
                    block.RegenerateBlockId();
                    return;
                }
            }
        }

        private static void ConfigureTextureImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        private static string EnsureFolder(string root, string child)
        {
            string[] rootParts = root.Split('/');
            string current = rootParts[0];
            for (int index = 1; index < rootParts.Length; index++)
            {
                string next = current + "/" + rootParts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, rootParts[index]);
                }
                current = next;
            }

            string childPath = current + "/" + child;
            if (!AssetDatabase.IsValidFolder(childPath))
            {
                AssetDatabase.CreateFolder(current, child);
            }
            return childPath;
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }
            return string.IsNullOrWhiteSpace(value) ? "SurfaceTileBlock" : value;
        }

        private sealed class BlockLayout
        {
            internal BlockLayout(int width, int height, Vector2Int tilePixels)
            {
                Width = width;
                Height = height;
                TilePixels = tilePixels;
            }

            internal int Width { get; }
            internal int Height { get; }
            internal Vector2Int TilePixels { get; }
            internal Dictionary<SurfaceTileFace, RectInt> FaceRects { get; } =
                new Dictionary<SurfaceTileFace, RectInt>();

            internal Dictionary<SurfaceTileFace, Rect> NormalizedRects()
            {
                Dictionary<SurfaceTileFace, Rect> result =
                    new Dictionary<SurfaceTileFace, Rect>();
                foreach (KeyValuePair<SurfaceTileFace, RectInt> pair in FaceRects)
                {
                    RectInt rect = pair.Value;
                    result[pair.Key] = new Rect(
                        (float)rect.x / Width,
                        (float)rect.y / Height,
                        (float)rect.width / Width,
                        (float)rect.height / Height);
                }
                return result;
            }
        }
    }
}
