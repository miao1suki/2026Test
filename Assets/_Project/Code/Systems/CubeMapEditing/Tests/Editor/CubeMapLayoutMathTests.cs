using NUnit.Framework;
using UnityEngine;

namespace Project.CubeMapEditing.Tests
{
    public sealed class CubeMapLayoutMathTests
    {
        [Test]
        public void FlatLayout_UsesFrontRightBackLeftOrder()
        {
            Assert.AreEqual(
                new Vector3(-18f, 2f, 0f),
                CubeMapLayoutMath.GetFlatSectionCenter(CubeMapFace.Front, 12f, 4f, 1));
            Assert.AreEqual(
                new Vector3(-6f, 2f, 0f),
                CubeMapLayoutMath.GetFlatSectionCenter(CubeMapFace.Right, 12f, 4f, 1));
            Assert.AreEqual(
                new Vector3(6f, 2f, 0f),
                CubeMapLayoutMath.GetFlatSectionCenter(CubeMapFace.Back, 12f, 4f, 1));
            Assert.AreEqual(
                new Vector3(18f, 2f, 0f),
                CubeMapLayoutMath.GetFlatSectionCenter(CubeMapFace.Left, 12f, 4f, 1));
        }

        [Test]
        public void FoldedLayout_PlacesFourFacesAroundSquarePrism()
        {
            Assert.AreEqual(
                new Vector3(0f, 6f, -6f),
                CubeMapLayoutMath.GetFoldedSectionCenter(CubeMapFace.Front, 12f, 4f, 2));
            Assert.AreEqual(
                new Vector3(6f, 6f, 0f),
                CubeMapLayoutMath.GetFoldedSectionCenter(CubeMapFace.Right, 12f, 4f, 2));
            Assert.AreEqual(
                new Vector3(0f, 6f, 6f),
                CubeMapLayoutMath.GetFoldedSectionCenter(CubeMapFace.Back, 12f, 4f, 2));
            Assert.AreEqual(
                new Vector3(-6f, 6f, 0f),
                CubeMapLayoutMath.GetFoldedSectionCenter(CubeMapFace.Left, 12f, 4f, 2));
        }

        [Test]
        public void FoldedLayout_RotatesAdjacentFacesByQuarterTurns()
        {
            Assert.That(
                Quaternion.Angle(
                    Quaternion.identity,
                    CubeMapLayoutMath.GetFoldedRotation(CubeMapFace.Front)),
                Is.LessThan(0.001f));
            Assert.That(
                Quaternion.Angle(
                    Quaternion.Euler(0f, -90f, 0f),
                    CubeMapLayoutMath.GetFoldedRotation(CubeMapFace.Right)),
                Is.LessThan(0.001f));
            Assert.That(
                Quaternion.Angle(
                    Quaternion.Euler(0f, 180f, 0f),
                    CubeMapLayoutMath.GetFoldedRotation(CubeMapFace.Back)),
                Is.LessThan(0.001f));
            Assert.That(
                Quaternion.Angle(
                    Quaternion.Euler(0f, 90f, 0f),
                    CubeMapLayoutMath.GetFoldedRotation(CubeMapFace.Left)),
                Is.LessThan(0.001f));
        }
    }
}
