using System;
using UnityEngine;

namespace Project.SurfaceTiles
{
    public enum SurfaceTileFace
    {
        Front = 0,
        Right = 1,
        Back = 2,
        Left = 3,
        Top = 4,
        Bottom = 5
    }

    [Flags]
    public enum ProjectedPlatformDirections
    {
        None = 0,
        Front = 1 << 0,
        Right = 1 << 1,
        Back = 1 << 2,
        Left = 1 << 3,
        All = Front | Right | Back | Left
    }

    [Serializable]
    public sealed class SurfaceTilePlacement
    {
        [SerializeField] private SurfaceTileFace face;
        [SerializeField] private Vector2Int cell;
        [SerializeField] private string tileId;
        [SerializeField, Range(0, 3)] private int quarterTurns;
        [SerializeField] private bool flipX;
        [SerializeField] private bool flipY;

        public SurfaceTileFace Face => face;
        public Vector2Int Cell => cell;
        public string TileId => tileId;
        public int QuarterTurns => quarterTurns;
        public bool FlipX => flipX;
        public bool FlipY => flipY;

        public SurfaceTilePlacement(
            SurfaceTileFace valueFace,
            Vector2Int valueCell,
            string valueTileId,
            int valueQuarterTurns,
            bool valueFlipX,
            bool valueFlipY)
        {
            face = valueFace;
            cell = valueCell;
            tileId = valueTileId;
            quarterTurns = Mathf.Abs(valueQuarterTurns) % 4;
            flipX = valueFlipX;
            flipY = valueFlipY;
        }

        public void SetTile(
            string valueTileId,
            int valueQuarterTurns,
            bool valueFlipX,
            bool valueFlipY)
        {
            tileId = valueTileId;
            quarterTurns = Mathf.Abs(valueQuarterTurns) % 4;
            flipX = valueFlipX;
            flipY = valueFlipY;
        }
    }

    public readonly struct SurfaceTileFaceBasis
    {
        public SurfaceTileFaceBasis(
            Vector3 center,
            Vector3 normal,
            Vector3 axisU,
            Vector3 axisV,
            float width,
            float height)
        {
            Center = center;
            Normal = normal;
            AxisU = axisU;
            AxisV = axisV;
            Width = width;
            Height = height;
        }

        public Vector3 Center { get; }
        public Vector3 Normal { get; }
        public Vector3 AxisU { get; }
        public Vector3 AxisV { get; }
        public float Width { get; }
        public float Height { get; }

        public Vector3 Point(float normalizedU, float normalizedV, float bias = 0f)
        {
            return Center +
                   AxisU * ((normalizedU - 0.5f) * Width) +
                   AxisV * ((normalizedV - 0.5f) * Height) +
                   Normal * bias;
        }
    }

    public static class SurfaceTileGeometry
    {
        public static SurfaceTileFace FaceFromLocalNormal(Vector3 normal)
        {
            Vector3 absolute = new Vector3(
                Mathf.Abs(normal.x),
                Mathf.Abs(normal.y),
                Mathf.Abs(normal.z));
            if (absolute.y >= absolute.x && absolute.y >= absolute.z)
            {
                return normal.y >= 0f
                    ? SurfaceTileFace.Top
                    : SurfaceTileFace.Bottom;
            }

            if (absolute.x >= absolute.z)
            {
                return normal.x >= 0f
                    ? SurfaceTileFace.Left
                    : SurfaceTileFace.Right;
            }

            return normal.z >= 0f
                ? SurfaceTileFace.Back
                : SurfaceTileFace.Front;
        }

        public static SurfaceTileFaceBasis GetLocalBasis(
            BoxCollider collider,
            SurfaceTileFace face)
        {
            Vector3 center = collider != null ? collider.center : Vector3.zero;
            Vector3 size = collider != null ? collider.size : Vector3.one;
            switch (face)
            {
                case SurfaceTileFace.Right:
                    return new SurfaceTileFaceBasis(
                        center + Vector3.left * size.x * 0.5f,
                        Vector3.left,
                        Vector3.back,
                        Vector3.up,
                        size.z,
                        size.y);
                case SurfaceTileFace.Back:
                    return new SurfaceTileFaceBasis(
                        center + Vector3.forward * size.z * 0.5f,
                        Vector3.forward,
                        Vector3.left,
                        Vector3.up,
                        size.x,
                        size.y);
                case SurfaceTileFace.Left:
                    return new SurfaceTileFaceBasis(
                        center + Vector3.right * size.x * 0.5f,
                        Vector3.right,
                        Vector3.forward,
                        Vector3.up,
                        size.z,
                        size.y);
                case SurfaceTileFace.Top:
                    return new SurfaceTileFaceBasis(
                        center + Vector3.up * size.y * 0.5f,
                        Vector3.up,
                        Vector3.right,
                        Vector3.forward,
                        size.x,
                        size.z);
                case SurfaceTileFace.Bottom:
                    return new SurfaceTileFaceBasis(
                        center + Vector3.down * size.y * 0.5f,
                        Vector3.down,
                        Vector3.right,
                        Vector3.back,
                        size.x,
                        size.z);
                default:
                    return new SurfaceTileFaceBasis(
                        center + Vector3.back * size.z * 0.5f,
                        Vector3.back,
                        Vector3.right,
                        Vector3.up,
                        size.x,
                        size.y);
            }
        }

        public static Vector2Int GetGridSize(
            Transform transform,
            BoxCollider collider,
            SurfaceTileFace face,
            float worldCellSize)
        {
            SurfaceTileFaceBasis basis = GetLocalBasis(collider, face);
            Vector3 scale = transform != null
                ? transform.lossyScale
                : Vector3.one;
            float scaleU = AxisScale(basis.AxisU, scale);
            float scaleV = AxisScale(basis.AxisV, scale);
            float safeCellSize = Mathf.Max(0.01f, worldCellSize);
            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(basis.Width * scaleU / safeCellSize)),
                Mathf.Max(1, Mathf.RoundToInt(basis.Height * scaleV / safeCellSize)));
        }

        public static bool TryGetCell(
            BoxCollider collider,
            SurfaceTileFace face,
            Vector3 localPoint,
            Vector2Int gridSize,
            out Vector2Int cell)
        {
            SurfaceTileFaceBasis basis = GetLocalBasis(collider, face);
            Vector3 origin = basis.Point(0f, 0f);
            float normalizedU = Vector3.Dot(localPoint - origin, basis.AxisU) /
                                Mathf.Max(0.0001f, basis.Width);
            float normalizedV = Vector3.Dot(localPoint - origin, basis.AxisV) /
                                Mathf.Max(0.0001f, basis.Height);
            if (normalizedU < -0.001f || normalizedU > 1.001f ||
                normalizedV < -0.001f || normalizedV > 1.001f)
            {
                cell = default;
                return false;
            }

            cell = new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(normalizedU * gridSize.x), 0, gridSize.x - 1),
                Mathf.Clamp(Mathf.FloorToInt(normalizedV * gridSize.y), 0, gridSize.y - 1));
            return true;
        }

        private static float AxisScale(Vector3 axis, Vector3 scale)
        {
            return Mathf.Abs(axis.x) * Mathf.Abs(scale.x) +
                   Mathf.Abs(axis.y) * Mathf.Abs(scale.y) +
                   Mathf.Abs(axis.z) * Mathf.Abs(scale.z);
        }
    }
}
