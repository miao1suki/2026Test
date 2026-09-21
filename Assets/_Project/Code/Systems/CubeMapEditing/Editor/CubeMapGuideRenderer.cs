using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Project.CubeMapEditing.Editor
{
    internal static class CubeMapGuideRenderer
    {
        internal static void DrawActiveScene()
        {
            if (!CubeMapSceneHud.GuidesVisible || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            CubeMapPieceAuthoring piece = FindInScene<CubeMapPieceAuthoring>(scene);
            if (piece != null && piece.Workspace != null)
            {
                DrawPiece(piece);
                return;
            }

            CubeMapGeneratedLayout generated = FindInScene<CubeMapGeneratedLayout>(scene);
            if (generated != null && generated.Workspace != null)
            {
                DrawGenerated(generated);
            }
        }

        private static void DrawPiece(CubeMapPieceAuthoring piece)
        {
            CubeMapWorkspaceDefinition workspace = piece.Workspace;
            for (int faceIndex = 0; faceIndex < CubeMapLayoutMath.FaceCount; faceIndex++)
            {
                CubeMapFace face = (CubeMapFace)faceIndex;
                Transform root = piece.GetFaceRoot(face);
                if (root == null)
                {
                    continue;
                }

                DrawFaceGrid(
                    root.localToWorldMatrix,
                    workspace.FaceWidth,
                    workspace.PieceHeight,
                    workspace.ColumnsPerFace,
                    workspace.RowsPerPiece,
                    GetFaceColor(face),
                    $"第 {piece.PieceIndex:00} 关 · {CubeMapLayoutMath.GetFaceLabel(face)}");
            }
        }

        private static void DrawGenerated(CubeMapGeneratedLayout generated)
        {
            CubeMapWorkspaceDefinition workspace = generated.Workspace;
            for (int pieceIndex = 1; pieceIndex <= workspace.PieceCount; pieceIndex++)
            {
                for (int faceIndex = 0;
                     faceIndex < CubeMapLayoutMath.FaceCount;
                     faceIndex++)
                {
                    CubeMapFace face = (CubeMapFace)faceIndex;
                    Vector3 center = generated.Folded
                        ? CubeMapLayoutMath.GetFoldedSectionCenter(
                            face,
                            workspace.FaceWidth,
                            workspace.PieceHeight,
                            pieceIndex)
                        : CubeMapLayoutMath.GetFlatSectionCenter(
                            face,
                            workspace.FaceWidth,
                            workspace.PieceHeight,
                            pieceIndex);
                    Quaternion rotation = generated.Folded
                        ? CubeMapLayoutMath.GetFoldedRotation(face)
                        : Quaternion.identity;
                    Matrix4x4 matrix = generated.transform.localToWorldMatrix *
                                       Matrix4x4.TRS(center, rotation, Vector3.one);
                    DrawFaceGrid(
                        matrix,
                        workspace.FaceWidth,
                        workspace.PieceHeight,
                        workspace.ColumnsPerFace,
                        workspace.RowsPerPiece,
                        GetFaceColor(face),
                        $"{pieceIndex:00} · {CubeMapLayoutMath.GetFaceLabel(face)}");
                }
            }
        }

        private static void DrawFaceGrid(
            Matrix4x4 matrix,
            float width,
            float height,
            int columns,
            int rows,
            Color color,
            string label)
        {
            Matrix4x4 previousMatrix = Handles.matrix;
            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;
            Handles.matrix = matrix;
            Handles.zTest = CompareFunction.LessEqual;

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            Vector3[] corners =
            {
                new Vector3(-halfWidth, -halfHeight, 0f),
                new Vector3(-halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, -halfHeight, 0f)
            };
            Color fill = new Color(color.r, color.g, color.b, 0.075f);
            Color outline = new Color(color.r, color.g, color.b, 0.82f);
            Handles.DrawSolidRectangleWithOutline(corners, fill, outline);

            Handles.color = new Color(color.r, color.g, color.b, 0.42f);
            for (int column = 1; column < columns; column++)
            {
                float x = Mathf.Lerp(-halfWidth, halfWidth, column / (float)columns);
                Handles.DrawDottedLine(
                    new Vector3(x, -halfHeight, 0f),
                    new Vector3(x, halfHeight, 0f),
                    4f);
            }

            for (int row = 1; row < rows; row++)
            {
                float y = Mathf.Lerp(-halfHeight, halfHeight, row / (float)rows);
                Handles.DrawDottedLine(
                    new Vector3(-halfWidth, y, 0f),
                    new Vector3(halfWidth, y, 0f),
                    4f);
            }

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.LowerCenter,
                normal = { textColor = outline },
                fontSize = 11
            };
            Handles.Label(
                new Vector3(0f, halfHeight + Mathf.Max(0.2f, height * 0.04f), 0f),
                label,
                labelStyle);

            Handles.matrix = previousMatrix;
            Handles.color = previousColor;
            Handles.zTest = previousZTest;
        }

        private static Color GetFaceColor(CubeMapFace face)
        {
            switch (face)
            {
                case CubeMapFace.Front:
                    return new Color(0.15f, 0.62f, 1f, 1f);
                case CubeMapFace.Right:
                    return new Color(1f, 0.55f, 0.16f, 1f);
                case CubeMapFace.Back:
                    return new Color(0.65f, 0.39f, 1f, 1f);
                case CubeMapFace.Left:
                    return new Color(0.22f, 0.78f, 0.47f, 1f);
                default:
                    return Color.white;
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
