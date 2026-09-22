using System.Collections.Generic;
using UnityEngine;

namespace Project.CubeMapEditing
{
    [CreateAssetMenu(
        fileName = "CubeMapWorkspace",
        menuName = "2026Test/立方体地图工作区")]
    public sealed class CubeMapWorkspaceDefinition : ScriptableObject
    {
        [SerializeField]
        private string levelId = "LV001";

        [SerializeField, Min(0.1f)]
        private float faceWidth = 12f;

        [SerializeField, Min(0.1f)]
        private float pieceHeight = 4f;

        [SerializeField, Min(1)]
        private int columnsPerFace = 6;

        [SerializeField, Min(1)]
        private int rowsPerPiece = 2;

        [SerializeField, Range(1, 16)]
        private int placementGridSubdivisions = 4;

        [SerializeField]
        private string total2DScenePath;

        [SerializeField]
        private string main3DScenePath;

        [SerializeField]
        private List<string> pieceScenePaths = new List<string>();

        public string LevelId => levelId;
        public float FaceWidth => faceWidth;
        public float PieceHeight => pieceHeight;
        public int ColumnsPerFace => columnsPerFace;
        public int RowsPerPiece => rowsPerPiece;
        public int PlacementGridSubdivisions => placementGridSubdivisions;
        public string Total2DScenePath => total2DScenePath;
        public string Main3DScenePath => main3DScenePath;
        public IReadOnlyList<string> PieceScenePaths => pieceScenePaths;
        public int PieceCount => pieceScenePaths.Count;
        public float TotalHeight => PieceCount * pieceHeight;
        public Vector2 CellSize => new Vector2(
            faceWidth / Mathf.Max(1, columnsPerFace),
            pieceHeight / Mathf.Max(1, rowsPerPiece));
        public Vector2 PlacementCellSize => CellSize / Mathf.Max(1, placementGridSubdivisions);
        public int PlacementColumnsPerFace => ColumnsPerFace * Mathf.Max(1, placementGridSubdivisions);
        public int PlacementRowsPerPiece => RowsPerPiece * Mathf.Max(1, placementGridSubdivisions);

        public void Configure(
            string id,
            float width,
            float sectionHeight,
            int columns,
            int rows,
            string flatScenePath,
            string foldedScenePath)
        {
            levelId = string.IsNullOrWhiteSpace(id) ? "LV001" : id;
            faceWidth = Mathf.Max(0.1f, width);
            pieceHeight = Mathf.Max(0.1f, sectionHeight);
            columnsPerFace = Mathf.Max(1, columns);
            rowsPerPiece = Mathf.Max(1, rows);
            total2DScenePath = flatScenePath;
            main3DScenePath = foldedScenePath;
        }

        public void SetPlacementGridSubdivisions(int subdivisions)
        {
            placementGridSubdivisions = Mathf.Clamp(subdivisions, 1, 16);
        }

        public bool AddPieceScene(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || pieceScenePaths.Contains(scenePath))
            {
                return false;
            }

            pieceScenePaths.Add(scenePath);
            return true;
        }

        private void OnValidate()
        {
            faceWidth = Mathf.Max(0.1f, faceWidth);
            pieceHeight = Mathf.Max(0.1f, pieceHeight);
            columnsPerFace = Mathf.Max(1, columnsPerFace);
            rowsPerPiece = Mathf.Max(1, rowsPerPiece);
            placementGridSubdivisions = Mathf.Clamp(placementGridSubdivisions, 1, 16);
            pieceScenePaths.RemoveAll(string.IsNullOrWhiteSpace);
        }
    }
}
