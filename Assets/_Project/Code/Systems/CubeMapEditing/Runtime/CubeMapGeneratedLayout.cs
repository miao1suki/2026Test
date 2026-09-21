using UnityEngine;

namespace Project.CubeMapEditing
{
    [DisallowMultipleComponent]
    public sealed class CubeMapGeneratedLayout : MonoBehaviour
    {
        [SerializeField]
        private CubeMapWorkspaceDefinition workspace;

        [SerializeField]
        private bool folded;

        public CubeMapWorkspaceDefinition Workspace => workspace;
        public bool Folded => folded;

        public void Configure(CubeMapWorkspaceDefinition definition, bool isFolded)
        {
            workspace = definition;
            folded = isFolded;
        }
    }
}
