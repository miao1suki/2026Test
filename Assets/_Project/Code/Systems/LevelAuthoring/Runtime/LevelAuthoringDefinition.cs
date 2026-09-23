using Project.CubeMapEditing;
using UnityEngine;

namespace Project.LevelAuthoring
{
    [CreateAssetMenu(
        fileName = "LevelAuthoringDefinition",
        menuName = "2026Test/关卡创作/关卡管线定义")]
    public sealed class LevelAuthoringDefinition : ScriptableObject
    {
        [SerializeField] private string levelId = "LV001";
        [SerializeField, Min(1)] private int pieceCount = 4;
        [SerializeField] private CubeMapWorkspaceDefinition workspace;
        [SerializeField] private string developmentRoot;
        [SerializeField] private string authoringRoot;
        [SerializeField] private string preview2DScenePath;
        [SerializeField] private string preview3DScenePath;
        [SerializeField] private string releaseMainScenePath;
        [SerializeField] private string releaseMapScenePath;

        public string LevelId => levelId;
        public int PieceCount => Mathf.Max(1, pieceCount);
        public CubeMapWorkspaceDefinition Workspace => workspace;
        public string DevelopmentRoot => developmentRoot;
        public string AuthoringRoot => authoringRoot;
        public string Preview2DScenePath => preview2DScenePath;
        public string Preview3DScenePath => preview3DScenePath;
        public string ReleaseMainScenePath => releaseMainScenePath;
        public string ReleaseMapScenePath => releaseMapScenePath;

        public void Configure(
            string id,
            int pieces,
            CubeMapWorkspaceDefinition sourceWorkspace,
            string devRoot,
            string sourceRoot,
            string flatPreview,
            string foldedPreview,
            string releaseMain,
            string releaseMap)
        {
            levelId = string.IsNullOrWhiteSpace(id) ? "LV001" : id;
            pieceCount = Mathf.Max(1, pieces);
            workspace = sourceWorkspace;
            developmentRoot = devRoot;
            authoringRoot = sourceRoot;
            preview2DScenePath = flatPreview;
            preview3DScenePath = foldedPreview;
            releaseMainScenePath = releaseMain;
            releaseMapScenePath = releaseMap;
        }
    }
}
