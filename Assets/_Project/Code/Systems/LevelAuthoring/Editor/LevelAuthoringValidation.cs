using System.Collections.Generic;
using Project.CubeMapEditing;
using UnityEditor;
using UnityEngine;

namespace Project.LevelAuthoring.Editor
{
    internal sealed class LevelValidationReport
    {
        internal readonly List<string> Errors = new();
        internal readonly List<string> Warnings = new();
        internal bool IsValid => Errors.Count == 0;

        internal string Format()
        {
            List<string> lines = new();
            lines.Add(IsValid ? "验证通过。" : $"验证失败：{Errors.Count} 个错误。");
            for (int index = 0; index < Errors.Count; index++)
            {
                lines.Add($"[错误] {Errors[index]}");
            }

            for (int index = 0; index < Warnings.Count; index++)
            {
                lines.Add($"[警告] {Warnings[index]}");
            }

            return string.Join("\n", lines);
        }
    }

    internal static class LevelAuthoringValidation
    {
        internal static LevelValidationReport Validate(LevelAuthoringDefinition definition)
        {
            LevelValidationReport report = new();
            if (definition == null)
            {
                report.Errors.Add("没有选择关卡管线定义。");
                return report;
            }

            if (definition.Workspace == null)
            {
                report.Errors.Add("关卡管线定义没有绑定 CubeMapWorkspaceDefinition。");
                return report;
            }

            List<LevelAuthoringChunk> chunks =
                LevelAuthoringRepository.LoadChunks(definition);
            Dictionary<string, LevelAuthoringChunk> ids = new();
            HashSet<string> ropeIds = new();
            List<(LevelPlatformRecord Record, LevelAuthoringChunk Chunk)> platforms = new();
            for (int index = 0; index < chunks.Count; index++)
            {
                LevelAuthoringChunk chunk = chunks[index];
                ValidateChunkLocation(definition, chunk, report);
                foreach (LevelEntityRecord record in chunk.EnumerateAll())
                {
                    if (record == null)
                    {
                        report.Errors.Add($"{AssetDatabase.GetAssetPath(chunk)} 含有空记录。");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(record.EntityId))
                    {
                        report.Errors.Add($"{chunk.name}/{record.DisplayName} 缺少稳定 ID。");
                    }
                    else if (ids.TryGetValue(record.EntityId, out LevelAuthoringChunk previous))
                    {
                        report.Errors.Add(
                            $"重复 ID {record.EntityId}：{previous.name} 与 {chunk.name}。");
                    }
                    else
                    {
                        ids.Add(record.EntityId, chunk);
                    }

                    if (record is LevelMapItemRecord map && map.Prefab == null)
                    {
                        report.Errors.Add($"{chunk.name}/{record.DisplayName} 缺少 Prefab。");
                    }

                    if (record is LevelRopeRecord rope)
                    {
                        ropeIds.Add(rope.EntityId);
                    }
                    else if (record is LevelPlatformRecord platform)
                    {
                        platforms.Add((platform, chunk));
                    }

                    ValidateBounds(definition.Workspace, chunk, record, report);
                }
            }

            for (int index = 0; index < platforms.Count; index++)
            {
                LevelPlatformRecord platform = platforms[index].Record;
                if (!string.IsNullOrWhiteSpace(platform.BoundRopeId) &&
                    !ropeIds.Contains(platform.BoundRopeId))
                {
                    report.Errors.Add(
                        $"{platforms[index].Chunk.name}/{platform.DisplayName} " +
                        $"绑定了不存在的绳子 {platform.BoundRopeId}。");
                }
            }

            if (chunks.Count == 0)
            {
                report.Errors.Add("Authoring 目录中没有任何关卡数据块。");
            }

            return report;
        }

        private static void ValidateChunkLocation(
            LevelAuthoringDefinition definition,
            LevelAuthoringChunk chunk,
            LevelValidationReport report)
        {
            string expected = LevelProjectPaths.GetChunkPath(
                definition.LevelId,
                chunk.PieceIndex,
                chunk.Face,
                chunk.ContentKind);
            string actual = AssetDatabase.GetAssetPath(chunk);
            if (actual != expected)
            {
                report.Warnings.Add(
                    $"数据块不在标准关卡目录：{actual}；期望 {expected}。");
            }

            if (chunk.LevelId != definition.LevelId)
            {
                report.Errors.Add($"{actual} 的 LevelId 与管线定义不一致。");
            }
        }

        private static void ValidateBounds(
            CubeMapWorkspaceDefinition workspace,
            LevelAuthoringChunk chunk,
            LevelEntityRecord record,
            LevelValidationReport report)
        {
            Vector3 position = record.Pose.LocalPosition;
            float halfWidth = workspace.FaceWidth * 0.5f;
            float halfHeight = workspace.PieceHeight * 0.5f;
            if (position.x < -halfWidth || position.x > halfWidth ||
                position.y < -halfHeight || position.y > halfHeight)
            {
                report.Warnings.Add(
                    $"{chunk.name}/{record.DisplayName} 的中心超出所属面的平面边界。");
            }
        }
    }
}
