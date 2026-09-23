using Project.LadderPaths;
using Project.RopePaths;
using Project.CubeMapEditing;
using UnityEditor;
using UnityEngine;

namespace Project.LevelAuthoring.Editor
{
    [CustomEditor(typeof(LevelAuthoringProxy))]
    internal sealed class LevelAuthoringProxyEditor : UnityEditor.Editor
    {
        private static double lastAutoWriteTime;

        [InitializeOnLoadMethod]
        private static void InitializeAutoWriteBack()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;
        }

        public override void OnInspectorGUI()
        {
            LevelAuthoringProxy proxy = (LevelAuthoringProxy)target;
            EditorGUILayout.HelpBox(
                "这是开发预览代理。Transform、绳子端点和梯子尺寸会写回对应关卡数据块；正式发布场景不包含此组件。",
                MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("源数据块", proxy.SourceChunk, typeof(LevelAuthoringChunk), false);
                EditorGUILayout.TextField("稳定 ID", proxy.EntityId);
                EditorGUILayout.EnumPopup("视图", proxy.ViewMode);
            }

            if (GUILayout.Button("立即写回创作数据"))
            {
                Apply(proxy);
            }

            GUI.backgroundColor = new Color(1f, 0.58f, 0.48f);
            if (GUILayout.Button("从创作数据中删除并刷新预览"))
            {
                DeleteProxy(proxy);
            }

            GUI.backgroundColor = Color.white;
        }

        [MenuItem("Tools/2026Test/关卡创作管线/保存当前预览修改")]
        private static void SaveAllPreviewChanges()
        {
            LevelAuthoringProxy[] proxies = Object.FindObjectsByType<LevelAuthoringProxy>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int changed = 0;
            for (int index = 0; index < proxies.Length; index++)
            {
                if (Apply(proxies[index]))
                {
                    changed++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"已将 {changed} 个预览代理写回关卡创作数据。");
        }

        internal static bool Apply(LevelAuthoringProxy proxy)
        {
            if (proxy == null || proxy.SourceChunk == null)
            {
                return false;
            }

            LevelEntityRecord record = proxy.SourceChunk.Find(proxy.EntityId);
            if (record == null)
            {
                Debug.LogWarning($"找不到预览代理的源记录：{proxy.EntityId}", proxy);
                return false;
            }

            CubeMapWorkspaceDefinition workspace =
                FindDefinitionWorkspace(proxy.SourceChunk.LevelId);
            if (workspace == null)
            {
                return false;
            }

            LevelSectionFrame frame = LevelCoordinateUtility.GetFrame(
                workspace,
                proxy.SourceChunk.PieceIndex,
                proxy.SourceChunk.Face,
                proxy.ViewMode);
            Undo.RecordObject(proxy.SourceChunk, "Write level preview to authoring data");
            record.SetPose(LevelCoordinateUtility.FromViewPose(proxy.transform, frame));

            if (record is LevelRopeRecord ropeRecord &&
                proxy.TryGetComponent(out RopeSegment rope))
            {
                ropeRecord.Configure(
                    rope.LocalEndpointA,
                    rope.LocalEndpointB,
                    rope.RopeWidth,
                    rope.EndpointRadius);
            }
            else if (record is LevelLadderRecord ladderRecord &&
                     proxy.TryGetComponent(out LadderSegment ladder))
            {
                ladderRecord.Configure(
                    ladder.Width,
                    ladder.Height,
                    ladder.Depth,
                    ladder.RungCount);
            }

            EditorUtility.SetDirty(proxy.SourceChunk);
            return true;
        }

        private static void DeleteProxy(LevelAuthoringProxy proxy)
        {
            if (proxy == null || proxy.SourceChunk == null ||
                !EditorUtility.DisplayDialog(
                    "删除关卡对象",
                    $"将从源数据块中删除 {proxy.name}。此操作可通过 Undo 撤销。",
                    "删除",
                    "取消"))
            {
                return;
            }

            LevelAuthoringChunk chunk = proxy.SourceChunk;
            LevelViewMode mode = proxy.ViewMode;
            Undo.RecordObject(chunk, "Delete level authoring entity");
            if (!chunk.Remove(proxy.EntityId))
            {
                return;
            }

            EditorUtility.SetDirty(chunk);
            AssetDatabase.SaveAssets();
            LevelAuthoringDefinition definition =
                AssetDatabase.LoadAssetAtPath<LevelAuthoringDefinition>(
                    LevelProjectPaths.GetDefinitionPath(chunk.LevelId));
            if (definition != null)
            {
                LevelAuthoringComposer.BuildPreview(definition, mode, true);
            }
        }

        private static void OnSceneGui(SceneView sceneView)
        {
            Event current = Event.current;
            if (current == null || current.type != EventType.MouseUp ||
                EditorApplication.timeSinceStartup - lastAutoWriteTime < 0.05d)
            {
                return;
            }

            GameObject selected = Selection.activeGameObject;
            LevelAuthoringProxy proxy = selected != null
                ? selected.GetComponentInParent<LevelAuthoringProxy>()
                : null;
            if (proxy != null && Apply(proxy))
            {
                lastAutoWriteTime = EditorApplication.timeSinceStartup;
            }
        }

        private static CubeMapWorkspaceDefinition FindDefinitionWorkspace(
            string levelId)
        {
            string path = LevelProjectPaths.GetDefinitionPath(levelId);
            LevelAuthoringDefinition definition =
                AssetDatabase.LoadAssetAtPath<LevelAuthoringDefinition>(path);
            return definition != null ? definition.Workspace : null;
        }
    }
}
