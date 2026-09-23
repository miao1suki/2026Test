using Project.CubeMapEditing;
using UnityEngine;

namespace Project.LevelAuthoring
{
    [DisallowMultipleComponent]
    public sealed class LevelGeneratedEntity : MonoBehaviour
    {
        [SerializeField] private string entityId;
        [SerializeField] private LevelEntityKind entityKind;
        [SerializeField] private string levelId;
        [SerializeField] private int pieceIndex;
        [SerializeField] private CubeMapFace face;

        public string EntityId => entityId;
        public LevelEntityKind EntityKind => entityKind;
        public string LevelId => levelId;
        public int PieceIndex => pieceIndex;
        public CubeMapFace Face => face;

        public void Configure(
            string id,
            LevelEntityKind kind,
            string sourceLevelId,
            int piece,
            CubeMapFace sourceFace)
        {
            entityId = id;
            entityKind = kind;
            levelId = sourceLevelId;
            pieceIndex = piece;
            face = sourceFace;
        }
    }

    [DisallowMultipleComponent]
    public sealed class LevelAuthoringProxy : MonoBehaviour
    {
        [SerializeField] private LevelAuthoringChunk sourceChunk;
        [SerializeField] private string entityId;
        [SerializeField] private LevelViewMode viewMode;

        public LevelAuthoringChunk SourceChunk => sourceChunk;
        public string EntityId => entityId;
        public LevelViewMode ViewMode => viewMode;

        public void Configure(
            LevelAuthoringChunk chunk,
            string sourceEntityId,
            LevelViewMode mode)
        {
            sourceChunk = chunk;
            entityId = sourceEntityId;
            viewMode = mode;
        }
    }

    [DisallowMultipleComponent]
    public sealed class LevelGeneratedSceneInfo : MonoBehaviour
    {
        [SerializeField] private string levelId;
        [SerializeField] private LevelViewMode viewMode;
        [SerializeField] private string sourceHash;
        [SerializeField] private int pieceIndex;

        public string LevelId => levelId;
        public LevelViewMode ViewMode => viewMode;
        public string SourceHash => sourceHash;
        public int PieceIndex => pieceIndex;

        public void Configure(
            string id,
            LevelViewMode mode,
            string hash,
            int sourcePieceIndex = 0)
        {
            levelId = id;
            viewMode = mode;
            sourceHash = hash;
            pieceIndex = Mathf.Max(0, sourcePieceIndex);
        }
    }
}
