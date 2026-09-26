using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.SurfaceTiles
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SurfaceTileBlock : MonoBehaviour
    {
        [SerializeField] private SurfaceTilePalette palette;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;
        [SerializeField] private List<SurfaceTilePlacement> placements =
            new List<SurfaceTilePlacement>();
        [SerializeField, HideInInspector] private string blockId;
        [SerializeField, HideInInspector] private MeshFilter outputFilter;
        [SerializeField, HideInInspector] private MeshRenderer outputRenderer;
        [SerializeField, HideInInspector] private Mesh bakedMesh;
        [SerializeField, HideInInspector] private Material bakedMaterial;
        [SerializeField, HideInInspector] private bool bakeUpToDate;
        [SerializeField, HideInInspector] private Vector3 bakedColliderCenter;
        [SerializeField, HideInInspector] private Vector3 bakedColliderSize;
        [SerializeField, HideInInspector] private Vector3 bakedLossyScale;
        [SerializeField, HideInInspector] private float bakedCellSize;

        public SurfaceTilePalette Palette => palette;
        public float CellSize => cellSize;
        public IReadOnlyList<SurfaceTilePlacement> Placements => placements;
        public string BlockId => blockId;
        public MeshFilter OutputFilter => outputFilter;
        public MeshRenderer OutputRenderer => outputRenderer;
        public Mesh BakedMesh => bakedMesh;
        public Material BakedMaterial => bakedMaterial;
        public bool BakeUpToDate =>
            bakeUpToDate &&
            bakedMesh != null &&
            bakedMaterial != null &&
            GeometryMatchesBake();
        public BoxCollider SurfaceCollider => GetComponent<BoxCollider>();

        public Vector2Int GetGridSize(SurfaceTileFace face)
        {
            return SurfaceTileGeometry.GetGridSize(
                transform,
                SurfaceCollider,
                face,
                cellSize);
        }

        public bool TryGetPlacement(
            SurfaceTileFace face,
            Vector2Int cell,
            out SurfaceTilePlacement placement)
        {
            for (int index = 0; index < placements.Count; index++)
            {
                SurfaceTilePlacement candidate = placements[index];
                if (candidate.Face == face && candidate.Cell == cell)
                {
                    placement = candidate;
                    return true;
                }
            }

            placement = null;
            return false;
        }

        public void SetTile(
            SurfaceTileFace face,
            Vector2Int cell,
            string tileId,
            int quarterTurns,
            bool flipX,
            bool flipY)
        {
            if (string.IsNullOrWhiteSpace(tileId))
            {
                RemoveTile(face, cell);
                return;
            }

            if (TryGetPlacement(face, cell, out SurfaceTilePlacement placement))
            {
                placement.SetTile(tileId, quarterTurns, flipX, flipY);
            }
            else
            {
                placements.Add(new SurfaceTilePlacement(
                    face,
                    cell,
                    tileId,
                    quarterTurns,
                    flipX,
                    flipY));
            }

            bakeUpToDate = false;
        }

        public bool RemoveTile(SurfaceTileFace face, Vector2Int cell)
        {
            for (int index = placements.Count - 1; index >= 0; index--)
            {
                SurfaceTilePlacement placement = placements[index];
                if (placement.Face == face && placement.Cell == cell)
                {
                    placements.RemoveAt(index);
                    bakeUpToDate = false;
                    return true;
                }
            }

            return false;
        }

        public int ClearFace(SurfaceTileFace face)
        {
            int removed = 0;
            for (int index = placements.Count - 1; index >= 0; index--)
            {
                if (placements[index].Face == face)
                {
                    placements.RemoveAt(index);
                    removed++;
                }
            }

            if (removed > 0)
            {
                bakeUpToDate = false;
            }

            return removed;
        }

        public int RemoveOutOfBoundsTiles()
        {
            int removed = 0;
            for (int index = placements.Count - 1; index >= 0; index--)
            {
                SurfaceTilePlacement placement = placements[index];
                Vector2Int size = GetGridSize(placement.Face);
                if (placement.Cell.x < 0 || placement.Cell.y < 0 ||
                    placement.Cell.x >= size.x || placement.Cell.y >= size.y ||
                    palette == null || !palette.TryGet(placement.TileId, out _))
                {
                    placements.RemoveAt(index);
                    removed++;
                }
            }

            if (removed > 0)
            {
                bakeUpToDate = false;
            }

            return removed;
        }

        public void Configure(SurfaceTilePalette valuePalette, float valueCellSize)
        {
            palette = valuePalette;
            cellSize = Mathf.Max(0.01f, valueCellSize);
            bakeUpToDate = false;
        }

        public void BindOutput(MeshFilter filter, MeshRenderer renderer)
        {
            outputFilter = filter;
            outputRenderer = renderer;
        }

        public void BindBake(Mesh mesh, Material material)
        {
            bakedMesh = mesh;
            bakedMaterial = material;
            bakeUpToDate = mesh != null && material != null;
            BoxCollider collider = SurfaceCollider;
            bakedColliderCenter = collider != null ? collider.center : Vector3.zero;
            bakedColliderSize = collider != null ? collider.size : Vector3.one;
            bakedLossyScale = transform.lossyScale;
            bakedCellSize = cellSize;
            if (outputFilter != null)
            {
                outputFilter.sharedMesh = mesh;
            }

            if (outputRenderer != null)
            {
                outputRenderer.sharedMaterial = material;
            }
        }

        public void InvalidateBake()
        {
            bakeUpToDate = false;
        }

        public void EnsureBlockId()
        {
            if (string.IsNullOrWhiteSpace(blockId))
            {
                blockId = Guid.NewGuid().ToString("N");
            }
        }

        public void RegenerateBlockId()
        {
            blockId = Guid.NewGuid().ToString("N");
            bakeUpToDate = false;
        }

        private void Reset()
        {
            EnsureBlockId();
        }

        private void OnValidate()
        {
            cellSize = Mathf.Max(0.01f, cellSize);
            EnsureBlockId();
        }

        private bool GeometryMatchesBake()
        {
            BoxCollider collider = SurfaceCollider;
            return collider != null &&
                   Approximately(collider.center, bakedColliderCenter) &&
                   Approximately(collider.size, bakedColliderSize) &&
                   Approximately(transform.lossyScale, bakedLossyScale) &&
                   Mathf.Approximately(cellSize, bakedCellSize);
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude <= 0.000001f;
        }
    }
}
