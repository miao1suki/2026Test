using System;
using UnityEngine;

namespace Project.CubeMapEditing
{
    public static class CubeMapLayoutMath
    {
        public const int FaceCount = 4;

        public static Vector3 GetPieceFaceCenter(CubeMapFace face, float faceWidth)
        {
            Validate(face, faceWidth, 1f, 1);
            return new Vector3(((int)face - 1.5f) * faceWidth, 0f, 0f);
        }

        public static Vector3 GetFlatSectionCenter(
            CubeMapFace face,
            float faceWidth,
            float pieceHeight,
            int pieceIndex)
        {
            Validate(face, faceWidth, pieceHeight, pieceIndex);
            return new Vector3(
                ((int)face - 1.5f) * faceWidth,
                (pieceIndex - 0.5f) * pieceHeight,
                0f);
        }

        public static Vector3 GetFoldedSectionCenter(
            CubeMapFace face,
            float faceWidth,
            float pieceHeight,
            int pieceIndex)
        {
            Validate(face, faceWidth, pieceHeight, pieceIndex);
            float halfWidth = faceWidth * 0.5f;
            float height = (pieceIndex - 0.5f) * pieceHeight;
            switch (face)
            {
                case CubeMapFace.Front:
                    return new Vector3(0f, height, -halfWidth);
                case CubeMapFace.Right:
                    return new Vector3(halfWidth, height, 0f);
                case CubeMapFace.Back:
                    return new Vector3(0f, height, halfWidth);
                case CubeMapFace.Left:
                    return new Vector3(-halfWidth, height, 0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        public static Quaternion GetFoldedRotation(CubeMapFace face)
        {
            switch (face)
            {
                case CubeMapFace.Front:
                    return Quaternion.identity;
                case CubeMapFace.Right:
                    return Quaternion.Euler(0f, -90f, 0f);
                case CubeMapFace.Back:
                    return Quaternion.Euler(0f, 180f, 0f);
                case CubeMapFace.Left:
                    return Quaternion.Euler(0f, 90f, 0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        public static string GetFaceLabel(CubeMapFace face)
        {
            switch (face)
            {
                case CubeMapFace.Front:
                    return "正前";
                case CubeMapFace.Right:
                    return "正右";
                case CubeMapFace.Back:
                    return "正后";
                case CubeMapFace.Left:
                    return "正左";
                default:
                    throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        private static void Validate(
            CubeMapFace face,
            float faceWidth,
            float pieceHeight,
            int pieceIndex)
        {
            if ((int)face < 0 || (int)face >= FaceCount)
            {
                throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }

            if (faceWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(faceWidth));
            }

            if (pieceHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pieceHeight));
            }

            if (pieceIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pieceIndex));
            }
        }
    }
}
