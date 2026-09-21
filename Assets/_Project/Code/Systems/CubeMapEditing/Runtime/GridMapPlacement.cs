using UnityEngine;

namespace Project.CubeMapEditing
{
    [DisallowMultipleComponent]
    public sealed class GridMapPlacement : MonoBehaviour
    {
        [SerializeField] private GridMapItemDefinition definition;
        [SerializeField] private Vector2Int anchorCell;
        [SerializeField, Range(0, 3)] private int rotationSteps;
        [SerializeField] private bool snappedToGrid = true;
        [SerializeField] private bool allowOverlap;
        [SerializeField] private Vector2 unsnappedLocalPosition;

        public GridMapItemDefinition Definition => definition;
        public Vector2Int AnchorCell => anchorCell;
        public int RotationSteps => rotationSteps;
        public bool SnappedToGrid => snappedToGrid;
        public bool AllowOverlap => allowOverlap;
        public Vector2 UnsnappedLocalPosition => unsnappedLocalPosition;
        public Vector2Int RotatedSizeInCells
        {
            get
            {
                Vector2Int size = definition != null ? definition.SizeInCells : Vector2Int.one;
                return rotationSteps % 2 == 0
                    ? size
                    : new Vector2Int(size.y, size.x);
            }
        }

        public void Configure(
            GridMapItemDefinition item,
            Vector2Int cell,
            int rotation,
            bool useGrid,
            bool permitOverlap,
            Vector2 localPosition)
        {
            definition = item;
            anchorCell = cell;
            rotationSteps = ((rotation % 4) + 4) % 4;
            snappedToGrid = useGrid;
            allowOverlap = permitOverlap;
            unsnappedLocalPosition = localPosition;
        }

        private void OnValidate()
        {
            rotationSteps = ((rotationSteps % 4) + 4) % 4;
        }
    }
}
