using UnityEngine;

namespace Project.CubeMapEditing
{
    [CreateAssetMenu(
        fileName = "GridMapItem",
        menuName = "2026Test/网格地图物品")]
    public sealed class GridMapItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "新地图物品";
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(1)] private int widthInCells = 1;
        [SerializeField, Min(1)] private int heightInCells = 1;
        [SerializeField] private Vector2 pivotOffset;
        [SerializeField] private bool allowOverlapByDefault = true;
        [SerializeField] private bool snapToGridByDefault = true;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? name
            : displayName;
        public GameObject Prefab => prefab;
        public Vector2Int SizeInCells => new Vector2Int(widthInCells, heightInCells);
        public Vector2 PivotOffset => pivotOffset;
        public bool AllowOverlapByDefault => allowOverlapByDefault;
        public bool SnapToGridByDefault => snapToGridByDefault;

        public void Configure(
            string itemName,
            GameObject sourcePrefab,
            Vector2Int size,
            Vector2 pivot,
            bool allowOverlap,
            bool snapToGrid)
        {
            displayName = string.IsNullOrWhiteSpace(itemName) ? "新地图物品" : itemName;
            prefab = sourcePrefab;
            widthInCells = Mathf.Max(1, size.x);
            heightInCells = Mathf.Max(1, size.y);
            pivotOffset = pivot;
            allowOverlapByDefault = allowOverlap;
            snapToGridByDefault = snapToGrid;
        }

        private void OnValidate()
        {
            widthInCells = Mathf.Max(1, widthInCells);
            heightInCells = Mathf.Max(1, heightInCells);
        }
    }
}
