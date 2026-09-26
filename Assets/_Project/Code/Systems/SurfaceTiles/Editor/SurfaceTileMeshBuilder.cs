using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.SurfaceTiles.Editor
{
    internal static class SurfaceTileMeshBuilder
    {
        private const float WorldSurfaceBias = 0.003f;

        internal static void RefreshPreview(SurfaceTileBlock block)
        {
            if (block == null)
            {
                return;
            }

            EnsureOutput(block);
            Mesh previous = block.OutputFilter.sharedMesh;
            if (previous != null && !AssetDatabase.Contains(previous) &&
                previous.name.EndsWith("_Preview"))
            {
                Object.DestroyImmediate(previous);
            }

            Mesh preview = BuildCellMesh(block);
            preview.name = block.name + "_SurfaceTiles_Preview";
            preview.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            block.OutputFilter.sharedMesh = preview;
            block.OutputRenderer.sharedMaterial =
                block.Palette != null ? block.Palette.PreviewMaterial : null;
            EditorUtility.SetDirty(block);
            EditorUtility.SetDirty(block.OutputFilter);
            EditorUtility.SetDirty(block.OutputRenderer);
        }

        internal static Mesh BuildCellMesh(SurfaceTileBlock block)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            SurfaceTilePalette palette = block.Palette;
            BoxCollider collider = block.SurfaceCollider;
            if (palette == null || collider == null || !palette.UsesSingleTexture())
            {
                return CreateMesh(vertices, normals, uvs, triangles);
            }

            IReadOnlyList<SurfaceTilePlacement> placements = block.Placements;
            for (int index = 0; index < placements.Count; index++)
            {
                SurfaceTilePlacement placement = placements[index];
                if (!palette.TryGet(placement.TileId, out SurfaceTilePalette.Entry tile) ||
                    tile.Sprite == null)
                {
                    continue;
                }

                Vector2Int grid = block.GetGridSize(placement.Face);
                if (placement.Cell.x < 0 || placement.Cell.y < 0 ||
                    placement.Cell.x >= grid.x || placement.Cell.y >= grid.y)
                {
                    continue;
                }

                Rect spriteUv = SpriteUv(tile.Sprite);
                AddCellQuad(
                    block,
                    placement.Face,
                    placement.Cell,
                    grid,
                    spriteUv,
                    placement.QuarterTurns,
                    placement.FlipX,
                    placement.FlipY,
                    vertices,
                    normals,
                    uvs,
                    triangles);
            }

            return CreateMesh(vertices, normals, uvs, triangles);
        }

        internal static Mesh BuildBakedMesh(
            SurfaceTileBlock block,
            IReadOnlyDictionary<SurfaceTileFace, Rect> faceUvs)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                SurfaceTileFace face = (SurfaceTileFace)faceIndex;
                if (!faceUvs.TryGetValue(face, out Rect uvRect) ||
                    !HasTiles(block, face))
                {
                    continue;
                }

                AddFaceQuad(
                    block,
                    face,
                    uvRect,
                    vertices,
                    normals,
                    uvs,
                    triangles);
            }

            Mesh mesh = CreateMesh(vertices, normals, uvs, triangles);
            mesh.name = block.name + "_SurfaceTiles_Baked";
            return mesh;
        }

        internal static void EnsureOutput(SurfaceTileBlock block)
        {
            if (block.OutputFilter != null && block.OutputRenderer != null)
            {
                return;
            }

            Transform child = block.transform.Find("__SurfaceTiles");
            GameObject output;
            if (child != null)
            {
                output = child.gameObject;
            }
            else
            {
                output = new GameObject("__SurfaceTiles");
                Undo.RegisterCreatedObjectUndo(output, "创建方块贴画输出");
                output.transform.SetParent(block.transform, false);
            }

            MeshFilter filter = output.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = Undo.AddComponent<MeshFilter>(output);
            }

            MeshRenderer renderer = output.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = Undo.AddComponent<MeshRenderer>(output);
            }

            Undo.RecordObject(block, "绑定方块贴画输出");
            block.BindOutput(filter, renderer);
        }

        internal static Vector2 TransformTileUv(
            Vector2 uv,
            int quarterTurns,
            bool flipX,
            bool flipY)
        {
            if (flipX)
            {
                uv.x = 1f - uv.x;
            }

            if (flipY)
            {
                uv.y = 1f - uv.y;
            }

            int turns = Mathf.Abs(quarterTurns) % 4;
            for (int index = 0; index < turns; index++)
            {
                uv = new Vector2(uv.y, 1f - uv.x);
            }

            return uv;
        }

        internal static Rect SpriteUv(Sprite sprite)
        {
            Rect rect = sprite.textureRect;
            Texture2D texture = sprite.texture;
            return new Rect(
                rect.x / texture.width,
                rect.y / texture.height,
                rect.width / texture.width,
                rect.height / texture.height);
        }

        private static void AddCellQuad(
            SurfaceTileBlock block,
            SurfaceTileFace face,
            Vector2Int cell,
            Vector2Int grid,
            Rect uvRect,
            int turns,
            bool flipX,
            bool flipY,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles)
        {
            float u0 = (float)cell.x / grid.x;
            float u1 = (float)(cell.x + 1) / grid.x;
            float v0 = (float)cell.y / grid.y;
            float v1 = (float)(cell.y + 1) / grid.y;
            AddQuad(
                block,
                face,
                u0,
                v0,
                u1,
                v1,
                uvRect,
                turns,
                flipX,
                flipY,
                vertices,
                normals,
                uvs,
                triangles);
        }

        private static void AddFaceQuad(
            SurfaceTileBlock block,
            SurfaceTileFace face,
            Rect uvRect,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles)
        {
            AddQuad(
                block,
                face,
                0f,
                0f,
                1f,
                1f,
                uvRect,
                0,
                false,
                false,
                vertices,
                normals,
                uvs,
                triangles);
        }

        private static void AddQuad(
            SurfaceTileBlock block,
            SurfaceTileFace face,
            float u0,
            float v0,
            float u1,
            float v1,
            Rect uvRect,
            int turns,
            bool flipX,
            bool flipY,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles)
        {
            SurfaceTileFaceBasis basis = SurfaceTileGeometry.GetLocalBasis(
                block.SurfaceCollider,
                face);
            float axisScale = Vector3.Magnitude(Vector3.Scale(
                basis.Normal,
                block.transform.lossyScale));
            float localBias = WorldSurfaceBias / Mathf.Max(0.0001f, axisScale);
            Vector3[] positions =
            {
                basis.Point(u0, v0, localBias),
                basis.Point(u1, v0, localBias),
                basis.Point(u1, v1, localBias),
                basis.Point(u0, v1, localBias)
            };
            Vector2[] logicalUvs =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            bool forward = Vector3.Dot(
                Vector3.Cross(basis.AxisU, basis.AxisV),
                basis.Normal) > 0f;
            int[] order = forward
                ? new[] { 0, 1, 2, 3 }
                : new[] { 0, 3, 2, 1 };
            int start = vertices.Count;
            for (int index = 0; index < 4; index++)
            {
                int source = order[index];
                vertices.Add(positions[source]);
                normals.Add(basis.Normal);
                Vector2 tileUv = TransformTileUv(
                    logicalUvs[source],
                    turns,
                    flipX,
                    flipY);
                uvs.Add(new Vector2(
                    uvRect.x + tileUv.x * uvRect.width,
                    uvRect.y + tileUv.y * uvRect.height));
            }

            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static Mesh CreateMesh(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles)
        {
            Mesh mesh = new Mesh
            {
                indexFormat = vertices.Count > 65535
                    ? IndexFormat.UInt32
                    : IndexFormat.UInt16
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static bool HasTiles(SurfaceTileBlock block, SurfaceTileFace face)
        {
            IReadOnlyList<SurfaceTilePlacement> placements = block.Placements;
            for (int index = 0; index < placements.Count; index++)
            {
                if (placements[index].Face == face)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
