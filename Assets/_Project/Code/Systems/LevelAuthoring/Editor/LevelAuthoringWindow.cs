using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.LevelAuthoring.Editor
{
    internal sealed class LevelAuthoringWindow : EditorWindow
    {
        private const string DefinitionPreference =
            "2026Test.LevelAuthoring.DefinitionGuid";

        private LevelAuthoringDefinition definition;
        private LevelAuthoringChunk activeChunk;
        private Vector2 scroll;

        [MenuItem("Tools/2026Test/关卡创作管线/打开管线窗口")]
        private static void Open()
        {
            GetWindow<LevelAuthoringWindow>("关卡创作管线").minSize =
                new Vector2(430f, 520f);
        }

        private void OnEnable()
        {
            string guid = EditorPrefs.GetString(DefinitionPreference, string.Empty);
            string path = AssetDatabase.GUIDToAssetPath(guid);
            definition = AssetDatabase.LoadAssetAtPath<LevelAuthoringDefinition>(path);
            if (definition == null)
            {
                definition = AssetDatabase.LoadAssetAtPath<LevelAuthoringDefinition>(
                    LevelProjectPaths.GetDefinitionPath(
                        LevelProjectStructureService.DefaultLevelId));
            }
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("开发 / 发布隔离", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Development 保存高频创作数据和可写预览；Release 只由发布按钮生成。目录按关卡、Piece、Face 与内容类型组织，不按人员或职位划分。",
                MessageType.Info);

            LevelAuthoringDefinition selected =
                (LevelAuthoringDefinition)EditorGUILayout.ObjectField(
                    "关卡管线定义",
                    definition,
                    typeof(LevelAuthoringDefinition),
                    false);
            if (selected != definition)
            {
                definition = selected;
                SaveDefinitionPreference();
            }

            if (definition == null)
            {
                if (GUILayout.Button("初始化 LV001 标准结构"))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        definition = LevelProjectStructureService.Initialize("LV001", true);
                        SaveDefinitionPreference();
                    }
                }

                EditorGUILayout.EndScrollView();
                return;
            }

            activeChunk = (LevelAuthoringChunk)EditorGUILayout.ObjectField(
                "当前创作数据块",
                activeChunk,
                typeof(LevelAuthoringChunk),
                false);
            EditorGUILayout.HelpBox(
                "在 2D/3D 预览中用现有工具创建绳子、梯子、平台后，选中它并收编到当前数据块。数据块必须按它实际所属的 Piece、Face 与内容类型选择。",
                MessageType.None);
            using (new EditorGUI.DisabledScope(activeChunk == null || Selection.activeGameObject == null))
            {
                if (GUILayout.Button("把选中对象收编到当前数据块"))
                {
                    LevelGeneratedSceneInfo info =
                        Object.FindFirstObjectByType<LevelGeneratedSceneInfo>();
                    LevelViewMode mode = info != null
                        ? info.ViewMode
                        : LevelViewMode.Total2D;
                    if (LevelAuthoringAdoptionService.AdoptSelected(activeChunk) && info != null)
                    {
                        LevelAuthoringComposer.BuildPreview(definition, mode, true);
                    }
                }
            }

            EditorGUILayout.Space();
            DrawPath("创作数据", definition.AuthoringRoot);
            DrawPath("2D 开发预览", definition.Preview2DScenePath);
            DrawPath("3D 开发预览", definition.Preview3DScenePath);
            DrawPath("发布主场景", definition.ReleaseMainScenePath);
            DrawPath("发布地图场景", definition.ReleaseMapScenePath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("迁移旧关卡", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "非破坏迁移会读取旧小拼图和旧 3D 主场景，但不会修改它们。它会清空并重新填充当前关卡的数据块，包含网格物品以及未位于生成根节点下的绳子、梯子和平台。",
                MessageType.Warning);
            if (GUILayout.Button("从旧场景重新收编到创作数据"))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() &&
                    EditorUtility.DisplayDialog(
                        "重新收编关卡",
                        "这会替换 Development 中现有的 LV001 创作记录，但不会修改旧场景。继续吗？",
                        "收编",
                        "取消"))
                {
                    LegacyLevelImportService.ImportAll(definition);
                    ShowNotification(new GUIContent("旧关卡已收编"));
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("开发预览", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("生成并打开 2D 总拼"))
                {
                    BuildPreview(LevelViewMode.Total2D);
                }

                if (GUILayout.Button("生成并打开 3D 折叠"))
                {
                    BuildPreview(LevelViewMode.Folded3D);
                }
            }

            if (GUILayout.Button("保存当前预览代理修改"))
            {
                EditorApplication.ExecuteMenuItem(
                    "Tools/2026Test/关卡创作管线/保存当前预览修改");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("验证与发布", EditorStyles.boldLabel);
            if (GUILayout.Button("验证关卡创作数据"))
            {
                LevelValidationReport report = LevelAuthoringValidation.Validate(definition);
                EditorUtility.DisplayDialog(
                    report.IsValid ? "验证通过" : "验证失败",
                    report.Format(),
                    "确定");
            }

            GUI.backgroundColor = new Color(0.45f, 0.85f, 0.55f);
            if (GUILayout.Button("验证并发布到 Release", GUILayout.Height(36f)))
            {
                Publish();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
        }

        private void BuildPreview(LevelViewMode mode)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            LevelAuthoringComposer.BuildPreview(definition, mode, true);
        }

        private void Publish()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            LevelValidationReport report = LevelAuthoringValidation.Validate(definition);
            if (!report.IsValid)
            {
                EditorUtility.DisplayDialog("不能发布", report.Format(), "确定");
                return;
            }

            LevelAuthoringComposer.PublishRelease(definition);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "发布完成",
                $"已生成：\n{definition.ReleaseMapScenePath}\n\n" +
                "发布主场景与生成地图场景应同时加入 Build Settings。",
                "确定");
        }

        private void SaveDefinitionPreference()
        {
            string path = definition != null ? AssetDatabase.GetAssetPath(definition) : null;
            string guid = !string.IsNullOrWhiteSpace(path)
                ? AssetDatabase.AssetPathToGUID(path)
                : string.Empty;
            EditorPrefs.SetString(DefinitionPreference, guid);
        }

        private static void DrawPath(string label, string path)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.SelectableLabel(
                    path ?? string.Empty,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }
    }
}
