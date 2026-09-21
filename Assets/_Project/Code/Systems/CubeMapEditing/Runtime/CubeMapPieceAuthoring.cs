using UnityEngine;

namespace Project.CubeMapEditing
{
    [DisallowMultipleComponent]
    public sealed class CubeMapPieceAuthoring : MonoBehaviour
    {
        [SerializeField]
        private CubeMapWorkspaceDefinition workspace;

        [SerializeField, Min(1)]
        private int pieceIndex = 1;

        [SerializeField]
        private Transform[] faceRoots = new Transform[CubeMapLayoutMath.FaceCount];

        public CubeMapWorkspaceDefinition Workspace => workspace;
        public int PieceIndex => pieceIndex;

        public Transform GetFaceRoot(CubeMapFace face)
        {
            int index = (int)face;
            return index >= 0 && index < faceRoots.Length ? faceRoots[index] : null;
        }

        public void Configure(
            CubeMapWorkspaceDefinition definition,
            int index,
            Transform[] roots)
        {
            workspace = definition;
            pieceIndex = Mathf.Max(1, index);
            faceRoots = roots != null && roots.Length == CubeMapLayoutMath.FaceCount
                ? roots
                : new Transform[CubeMapLayoutMath.FaceCount];
        }

        private void OnValidate()
        {
            pieceIndex = Mathf.Max(1, pieceIndex);
            if (faceRoots == null || faceRoots.Length != CubeMapLayoutMath.FaceCount)
            {
                System.Array.Resize(ref faceRoots, CubeMapLayoutMath.FaceCount);
            }
        }
    }
}
