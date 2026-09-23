using System.Collections.Generic;
using Project.CubeMapEditing;
using UnityEngine;

namespace Project.LevelAuthoring
{
    [CreateAssetMenu(
        fileName = "LevelAuthoringChunk",
        menuName = "2026Test/关卡创作/空间内容块")]
    public sealed class LevelAuthoringChunk : ScriptableObject
    {
        [SerializeField] private string levelId = "LV001";
        [SerializeField, Min(1)] private int pieceIndex = 1;
        [SerializeField] private CubeMapFace face;
        [SerializeField] private string regionId = "FullFace";
        [SerializeField] private LevelContentKind contentKind;
        [SerializeField] private List<LevelMapItemRecord> mapItems = new();
        [SerializeField] private List<LevelRopeRecord> ropes = new();
        [SerializeField] private List<LevelLadderRecord> ladders = new();
        [SerializeField] private List<LevelPlatformRecord> platforms = new();

        public string LevelId => levelId;
        public int PieceIndex => pieceIndex;
        public CubeMapFace Face => face;
        public string RegionId => regionId;
        public LevelContentKind ContentKind => contentKind;
        public IReadOnlyList<LevelMapItemRecord> MapItems => mapItems;
        public IReadOnlyList<LevelRopeRecord> Ropes => ropes;
        public IReadOnlyList<LevelLadderRecord> Ladders => ladders;
        public IReadOnlyList<LevelPlatformRecord> Platforms => platforms;

        public void Configure(
            string id,
            int piece,
            CubeMapFace cubeFace,
            string region,
            LevelContentKind kind)
        {
            levelId = string.IsNullOrWhiteSpace(id) ? "LV001" : id;
            pieceIndex = Mathf.Max(1, piece);
            face = cubeFace;
            regionId = string.IsNullOrWhiteSpace(region) ? "FullFace" : region;
            contentKind = kind;
        }

        public void Add(LevelMapItemRecord record) => AddUnique(mapItems, record);
        public void Add(LevelRopeRecord record) => AddUnique(ropes, record);
        public void Add(LevelLadderRecord record) => AddUnique(ladders, record);
        public void Add(LevelPlatformRecord record) => AddUnique(platforms, record);

        public LevelEntityRecord Find(string entityId)
        {
            LevelEntityRecord record = FindIn(mapItems, entityId);
            record ??= FindIn(ropes, entityId);
            record ??= FindIn(ladders, entityId);
            return record ?? FindIn(platforms, entityId);
        }

        public IEnumerable<LevelEntityRecord> EnumerateAll()
        {
            foreach (LevelMapItemRecord record in mapItems) yield return record;
            foreach (LevelRopeRecord record in ropes) yield return record;
            foreach (LevelLadderRecord record in ladders) yield return record;
            foreach (LevelPlatformRecord record in platforms) yield return record;
        }

        public void Clear()
        {
            mapItems.Clear();
            ropes.Clear();
            ladders.Clear();
            platforms.Clear();
        }

        public bool Remove(string entityId)
        {
            return RemoveFrom(mapItems, entityId) ||
                   RemoveFrom(ropes, entityId) ||
                   RemoveFrom(ladders, entityId) ||
                   RemoveFrom(platforms, entityId);
        }

        private void OnValidate()
        {
            pieceIndex = Mathf.Max(1, pieceIndex);
            regionId = string.IsNullOrWhiteSpace(regionId) ? "FullFace" : regionId;
            foreach (LevelEntityRecord record in EnumerateAll())
            {
                record?.EnsureId();
            }
        }

        private static void AddUnique<T>(List<T> records, T record)
            where T : LevelEntityRecord
        {
            if (record == null)
            {
                return;
            }

            record.EnsureId();
            if (FindIn(records, record.EntityId) == null)
            {
                records.Add(record);
            }
        }

        private static T FindIn<T>(IReadOnlyList<T> records, string entityId)
            where T : LevelEntityRecord
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                return null;
            }

            for (int index = 0; index < records.Count; index++)
            {
                if (records[index] != null && records[index].EntityId == entityId)
                {
                    return records[index];
                }
            }

            return null;
        }

        private static bool RemoveFrom<T>(List<T> records, string entityId)
            where T : LevelEntityRecord
        {
            T record = FindIn(records, entityId);
            return record != null && records.Remove(record);
        }
    }
}
