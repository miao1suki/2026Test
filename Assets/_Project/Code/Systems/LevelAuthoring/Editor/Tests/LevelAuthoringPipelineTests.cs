using NUnit.Framework;
using Project.CubeMapEditing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.LevelAuthoring.Editor.Tests
{
    public sealed class LevelAuthoringPipelineTests
    {
        [TestCase(LevelViewMode.Piece2D)]
        [TestCase(LevelViewMode.Total2D)]
        [TestCase(LevelViewMode.Folded3D)]
        [TestCase(LevelViewMode.Release3D)]
        public void CoordinateConversion_RoundTripsEveryFace(LevelViewMode mode)
        {
            CubeMapWorkspaceDefinition workspace =
                ScriptableObject.CreateInstance<CubeMapWorkspaceDefinition>();
            workspace.Configure("TEST", 12f, 4f, 6, 2, string.Empty, string.Empty);
            LevelPose source = new(
                new Vector3(2.25f, -0.75f, 0.4f),
                Quaternion.Euler(15f, 25f, 35f),
                new Vector3(1.2f, 0.8f, 1.5f));

            for (int index = 0; index < CubeMapLayoutMath.FaceCount; index++)
            {
                LevelSectionFrame frame = LevelCoordinateUtility.GetFrame(
                    workspace,
                    3,
                    (CubeMapFace)index,
                    mode);
                LevelPose view = LevelCoordinateUtility.ToViewPose(source, frame);
                GameObject gameObject = new("RoundTrip");
                gameObject.transform.position = view.LocalPosition;
                gameObject.transform.rotation = view.LocalRotation;
                gameObject.transform.localScale = view.LocalScale;
                LevelPose restored = LevelCoordinateUtility.FromViewPose(
                    gameObject.transform,
                    frame);

                Assert.That(Vector3.Distance(restored.LocalPosition, source.LocalPosition),
                    Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(restored.LocalRotation, source.LocalRotation),
                    Is.LessThan(0.001f));
                Assert.That(Vector3.Distance(restored.LocalScale, source.LocalScale),
                    Is.LessThan(0.0001f));
                Object.DestroyImmediate(gameObject);
            }

            Object.DestroyImmediate(workspace);
        }

        [Test]
        public void Chunk_RejectsDuplicateIds_AndRemovesByStableId()
        {
            LevelAuthoringChunk chunk =
                ScriptableObject.CreateInstance<LevelAuthoringChunk>();
            LevelRopeRecord first = CreateRope("rope-a");
            LevelRopeRecord duplicate = CreateRope("rope-a");

            chunk.Add(first);
            chunk.Add(duplicate);

            Assert.That(chunk.Ropes, Has.Count.EqualTo(1));
            Assert.That(chunk.Find("rope-a"), Is.SameAs(first));
            Assert.That(chunk.Remove("rope-a"), Is.True);
            Assert.That(chunk.Find("rope-a"), Is.Null);
            Assert.That(chunk.Remove("rope-a"), Is.False);
            Object.DestroyImmediate(chunk);
        }

        [Test]
        public void StandardPaths_AreLevelPieceFaceAndContentBased()
        {
            string path = LevelProjectPaths.GetChunkPath(
                "LV777",
                2,
                CubeMapFace.Left,
                LevelContentKind.Traversal);

            Assert.That(
                path,
                Is.EqualTo(
                    "Assets/_Project/Development/Levels/LV777/Authoring/Pieces/" +
                    "Piece_02/Left/Traversal.asset"));
            Assert.That(path, Does.Not.Contain("Programmer"));
            Assert.That(path, Does.Not.Contain("Designer"));
            Assert.That(LevelProjectPaths.GetReleaseMapScenePath("LV777"),
                Does.StartWith("Assets/_Project/Release/Levels/LV777/"));
            Assert.That(
                LevelProjectPaths.GetPiecePreviewScenePath("LV777", 2),
                Is.EqualTo(
                    "Assets/_Project/Development/Levels/LV777/Preview/Scenes/" +
                    "Pieces/LV777_Piece_02_Preview.unity"));
        }

        [Test]
        public void MapRecord_PreservesGridEditingState()
        {
            LevelMapItemRecord record = new();
            record.Initialize("map-a", "Map", LevelPose.Identity);
            record.ConfigureGrid(
                new Vector2Int(7, 3),
                -1,
                false,
                true,
                new Vector2(1.25f, -0.5f));

            Assert.That(record.AnchorCell, Is.EqualTo(new Vector2Int(7, 3)));
            Assert.That(record.RotationSteps, Is.EqualTo(3));
            Assert.That(record.SnappedToGrid, Is.False);
            Assert.That(record.AllowOverlap, Is.True);
            Assert.That(record.UnsnappedLocalPosition, Is.EqualTo(new Vector2(1.25f, -0.5f)));
        }

        [Test]
        public void PiecePreview_ProvidesGridEditorAuthoringContext()
        {
            const string levelId = "LV001";
            string definitionPath = LevelProjectPaths.GetDefinitionPath(levelId);
            LevelAuthoringDefinition definition =
                AssetDatabase.LoadAssetAtPath<LevelAuthoringDefinition>(definitionPath);
            Assert.That(definition, Is.Not.Null);
            string previewPath = LevelProjectPaths.GetPiecePreviewScenePath(levelId, 1);

            LevelAuthoringComposer.BuildPiecePreview(definition, 1, false);
            Scene scene = EditorSceneManager.OpenScene(previewPath, OpenSceneMode.Additive);
            try
            {
                CubeMapPieceAuthoring authoring = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    authoring = root.GetComponentInChildren<CubeMapPieceAuthoring>(true);
                    if (authoring != null) break;
                }

                Assert.That(authoring, Is.Not.Null);
                Assert.That(authoring.PieceIndex, Is.EqualTo(1));
                for (int index = 0; index < CubeMapLayoutMath.FaceCount; index++)
                {
                    Transform faceRoot = authoring.GetFaceRoot((CubeMapFace)index);
                    Assert.That(faceRoot, Is.Not.Null);
                    Assert.That(faceRoot.Find("Content"), Is.Not.Null);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                AssetDatabase.DeleteAsset(
                    $"{LevelProjectPaths.GetPreviewSceneFolder(levelId)}/Pieces");
            }
        }

        private static LevelRopeRecord CreateRope(string id)
        {
            LevelRopeRecord record = new();
            record.Initialize(id, "Rope", LevelPose.Identity);
            return record;
        }
    }
}
