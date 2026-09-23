using System;
using Project.CubeMapEditing;
using Project.RopePaths;
using UnityEngine;

namespace Project.LevelAuthoring
{
    public enum LevelContentKind
    {
        Geometry = 0,
        Traversal = 1,
        Gameplay = 2,
    }

    public enum LevelEntityKind
    {
        MapItem = 0,
        Rope = 1,
        Ladder = 2,
        Platform = 3,
    }

    public enum LevelViewMode
    {
        Piece2D = 0,
        Total2D = 1,
        Folded3D = 2,
        Release3D = 3,
    }

    [Serializable]
    public struct LevelPose
    {
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Quaternion localRotation;
        [SerializeField] private Vector3 localScale;

        public Vector3 LocalPosition => localPosition;
        public Quaternion LocalRotation => localRotation;
        public Vector3 LocalScale => localScale;

        public LevelPose(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            localPosition = position;
            localRotation = rotation;
            localScale = scale;
        }

        public static LevelPose Identity =>
            new LevelPose(Vector3.zero, Quaternion.identity, Vector3.one);
    }

    [Serializable]
    public abstract class LevelEntityRecord
    {
        [SerializeField, HideInInspector] private string entityId;
        [SerializeField] private string displayName;
        [SerializeField] private LevelPose pose = default;

        public string EntityId => entityId;
        public string DisplayName => displayName;
        public LevelPose Pose => pose;
        public abstract LevelEntityKind Kind { get; }

        public void Initialize(string id, string name, LevelPose initialPose)
        {
            entityId = string.IsNullOrWhiteSpace(id)
                ? Guid.NewGuid().ToString("N")
                : id;
            displayName = string.IsNullOrWhiteSpace(name) ? Kind.ToString() : name;
            pose = initialPose;
        }

        public void SetPose(LevelPose value)
        {
            pose = value;
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                entityId = Guid.NewGuid().ToString("N");
            }
        }
    }

    [Serializable]
    public sealed class LevelMapItemRecord : LevelEntityRecord
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private GridMapItemDefinition gridDefinition;
        [SerializeField] private Vector2Int anchorCell;
        [SerializeField, Range(0, 3)] private int rotationSteps;
        [SerializeField] private bool snappedToGrid = true;
        [SerializeField] private bool allowOverlap = true;
        [SerializeField] private Vector2 unsnappedLocalPosition;

        public override LevelEntityKind Kind => LevelEntityKind.MapItem;
        public GameObject Prefab => prefab != null
            ? prefab
            : gridDefinition != null ? gridDefinition.Prefab : null;
        public GridMapItemDefinition GridDefinition => gridDefinition;
        public Vector2Int AnchorCell => anchorCell;
        public int RotationSteps => rotationSteps;
        public bool SnappedToGrid => snappedToGrid;
        public bool AllowOverlap => allowOverlap;
        public Vector2 UnsnappedLocalPosition => unsnappedLocalPosition;

        public void Configure(GameObject value, GridMapItemDefinition definition)
        {
            prefab = value;
            gridDefinition = definition;
        }

        public void ConfigureGrid(
            Vector2Int cell,
            int rotation,
            bool useGrid,
            bool permitOverlap,
            Vector2 unsnappedPosition)
        {
            anchorCell = cell;
            rotationSteps = ((rotation % 4) + 4) % 4;
            snappedToGrid = useGrid;
            allowOverlap = permitOverlap;
            unsnappedLocalPosition = unsnappedPosition;
        }
    }

    [Serializable]
    public sealed class LevelRopeRecord : LevelEntityRecord
    {
        [SerializeField] private Vector3 endpointA = new Vector3(-1f, 0f, 0f);
        [SerializeField] private Vector3 endpointB = new Vector3(1f, 0f, 0f);
        [SerializeField, Min(0.01f)] private float ropeWidth = 0.08f;
        [SerializeField, Min(0.02f)] private float endpointRadius = 0.18f;

        public override LevelEntityKind Kind => LevelEntityKind.Rope;
        public Vector3 EndpointA => endpointA;
        public Vector3 EndpointB => endpointB;
        public float RopeWidth => ropeWidth;
        public float EndpointRadius => endpointRadius;

        public void Configure(
            Vector3 first,
            Vector3 second,
            float width,
            float radius)
        {
            endpointA = first;
            endpointB = second;
            ropeWidth = Mathf.Max(0.01f, width);
            endpointRadius = Mathf.Max(0.02f, radius);
        }
    }

    [Serializable]
    public sealed class LevelLadderRecord : LevelEntityRecord
    {
        [SerializeField, Min(0.1f)] private float width = 2f;
        [SerializeField, Min(0.1f)] private float height = 2f;
        [SerializeField, Min(0.05f)] private float depth = 0.25f;
        [SerializeField, Range(2, 12)] private int rungCount = 5;

        public override LevelEntityKind Kind => LevelEntityKind.Ladder;
        public float Width => width;
        public float Height => height;
        public float Depth => depth;
        public int RungCount => rungCount;

        public void Configure(float valueWidth, float valueHeight, float valueDepth, int rungs)
        {
            width = Mathf.Max(0.1f, valueWidth);
            height = Mathf.Max(0.1f, valueHeight);
            depth = Mathf.Max(0.05f, valueDepth);
            rungCount = Mathf.Clamp(rungs, 2, 12);
        }
    }

    [Serializable]
    public sealed class LevelPlatformRecord : LevelEntityRecord
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private string boundRopeId;
        [SerializeField] private RopeEndpoint boundEndpoint = RopeEndpoint.A;

        public override LevelEntityKind Kind => LevelEntityKind.Platform;
        public GameObject Prefab => prefab;
        public string BoundRopeId => boundRopeId;
        public RopeEndpoint BoundEndpoint => boundEndpoint;

        public void Configure(GameObject value, string ropeId, RopeEndpoint endpoint)
        {
            prefab = value;
            boundRopeId = ropeId;
            boundEndpoint = endpoint;
        }
    }

    public readonly struct LevelSectionFrame
    {
        public LevelSectionFrame(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public Vector3 TransformPoint(Vector3 localPoint) =>
            Position + Rotation * localPoint;

        public Vector3 InverseTransformPoint(Vector3 worldPoint) =>
            Quaternion.Inverse(Rotation) * (worldPoint - Position);

        public Quaternion TransformRotation(Quaternion localRotation) =>
            Rotation * localRotation;

        public Quaternion InverseTransformRotation(Quaternion worldRotation) =>
            Quaternion.Inverse(Rotation) * worldRotation;
    }

    public static class LevelCoordinateUtility
    {
        public static LevelSectionFrame GetFrame(
            CubeMapWorkspaceDefinition workspace,
            int pieceIndex,
            CubeMapFace face,
            LevelViewMode viewMode)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            bool folded = viewMode == LevelViewMode.Folded3D ||
                          viewMode == LevelViewMode.Release3D;
            if (viewMode == LevelViewMode.Piece2D)
            {
                return new LevelSectionFrame(
                    CubeMapLayoutMath.GetPieceFaceCenter(face, workspace.FaceWidth),
                    Quaternion.identity);
            }

            return folded
                ? new LevelSectionFrame(
                    CubeMapLayoutMath.GetFoldedSectionCenter(
                        face,
                        workspace.FaceWidth,
                        workspace.PieceHeight,
                        pieceIndex),
                    CubeMapLayoutMath.GetFoldedRotation(face))
                : new LevelSectionFrame(
                    CubeMapLayoutMath.GetFlatSectionCenter(
                        face,
                        workspace.FaceWidth,
                        workspace.PieceHeight,
                        pieceIndex),
                    Quaternion.identity);
        }

        public static LevelPose ToViewPose(LevelPose source, LevelSectionFrame frame)
        {
            return new LevelPose(
                frame.TransformPoint(source.LocalPosition),
                frame.TransformRotation(source.LocalRotation),
                source.LocalScale);
        }

        public static LevelPose FromViewPose(
            Transform viewTransform,
            LevelSectionFrame frame)
        {
            return new LevelPose(
                frame.InverseTransformPoint(viewTransform.position),
                frame.InverseTransformRotation(viewTransform.rotation),
                viewTransform.localScale);
        }
    }
}
