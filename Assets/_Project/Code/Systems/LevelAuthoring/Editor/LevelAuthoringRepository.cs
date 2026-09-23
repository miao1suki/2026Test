using System;
using System.Collections.Generic;
using Project.CubeMapEditing;
using UnityEditor;
using UnityEngine;

namespace Project.LevelAuthoring.Editor
{
    internal static class LevelAuthoringRepository
    {
        internal static List<LevelAuthoringChunk> LoadChunks(
            LevelAuthoringDefinition definition)
        {
            List<LevelAuthoringChunk> chunks = new();
            if (definition == null || string.IsNullOrWhiteSpace(definition.AuthoringRoot))
            {
                return chunks;
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:LevelAuthoringChunk",
                new[] { definition.AuthoringRoot });
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                LevelAuthoringChunk chunk =
                    AssetDatabase.LoadAssetAtPath<LevelAuthoringChunk>(path);
                if (chunk != null && chunk.LevelId == definition.LevelId)
                {
                    chunks.Add(chunk);
                }
            }

            chunks.Sort(CompareChunks);
            return chunks;
        }

        internal static LevelAuthoringChunk FindChunk(
            LevelAuthoringDefinition definition,
            int pieceIndex,
            CubeMapFace face,
            LevelContentKind kind)
        {
            string path = LevelProjectPaths.GetChunkPath(
                definition.LevelId,
                pieceIndex,
                face,
                kind);
            return AssetDatabase.LoadAssetAtPath<LevelAuthoringChunk>(path);
        }

        internal static string ComputeSourceHash(IReadOnlyList<LevelAuthoringChunk> chunks)
        {
            Hash128 hash = new Hash128();
            for (int index = 0; index < chunks.Count; index++)
            {
                string path = AssetDatabase.GetAssetPath(chunks[index]);
                Hash128 dependencyHash = AssetDatabase.GetAssetDependencyHash(path);
                hash.Append(dependencyHash.ToString());
            }

            return hash.ToString();
        }

        private static int CompareChunks(
            LevelAuthoringChunk first,
            LevelAuthoringChunk second)
        {
            int piece = first.PieceIndex.CompareTo(second.PieceIndex);
            if (piece != 0) return piece;
            int face = first.Face.CompareTo(second.Face);
            if (face != 0) return face;
            int kind = first.ContentKind.CompareTo(second.ContentKind);
            if (kind != 0) return kind;
            return string.Compare(first.RegionId, second.RegionId, StringComparison.Ordinal);
        }
    }
}
