using System;
using Project.CameraModes;
using Project.PlatformPaths;
using UnityEditor;
using UnityEngine;

namespace Project.PlatformPaths.Editor
{
    [CustomEditor(typeof(ProjectedInteractButton), true)]
    public sealed class ProjectedInteractButtonEditor :
        UnityEditor.Editor
    {
        private SerializedProperty script;
        private SerializedProperty commandedPlatforms;
        private SerializedProperty playerTag;
        private SerializedProperty interactionRange;
        private SerializedProperty cameraModeController;
        private SerializedProperty require2DFaceMatch;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            commandedPlatforms =
                serializedObject.FindProperty("commandedPlatforms");
            playerTag = serializedObject.FindProperty("playerTag");
            interactionRange =
                serializedObject.FindProperty("interactionRange");
            cameraModeController =
                serializedObject.FindProperty("cameraModeController");
            require2DFaceMatch =
                serializedObject.FindProperty("require2DFaceMatch");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    script,
                    new GUIContent("脚本"));
            }

            EditorGUILayout.HelpBox(
                "玩家进入交互距离后按交互键（默认 E）触发。\n" +
                "2D 模式下会比较按钮和玩家在当前投影面上的位置；"
                + "切换到另一面后，两者在画面上不再相邻时不能触发。\n"
                + "成功触发后通过 GameplaySignalHub 发出 InteractButtonActivated。",
                MessageType.Info);

            DrawSection("指挥平台");
            EditorGUILayout.PropertyField(
                commandedPlatforms,
                new GUIContent(
                    "绑定平台列表",
                    "可以同时绑定多个平台移动组件"),
                true);
            WarnAboutDisabledButtonPlatforms(
                commandedPlatforms);

            DrawSection("玩家交互");
            DrawTagField(
                playerTag,
                new GUIContent(
                    "玩家标签",
                    "用于识别可以触发按钮的玩家"));
            EditorGUILayout.PropertyField(
                interactionRange,
                new GUIContent(
                    "交互距离",
                    "玩家与按钮在三维和二维投影上的最大交互距离"));

            DrawSection("2D 投影交互");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(
                    cameraModeController,
                    new GUIContent(
                        "相机模式",
                        "用于读取当前 2D 朝向；留空时自动查找"));
                if (GUILayout.Button(
                        "查找",
                        GUILayout.Width(48f)))
                {
                    cameraModeController.objectReferenceValue =
                        UnityEngine.Object.FindFirstObjectByType<
                            CameraModeController>();
                }
            }
            EditorGUILayout.PropertyField(
                require2DFaceMatch,
                new GUIContent(
                    "要求处于同一投影面",
                    "开启后，2D 模式下玩家和按钮在画面上不相邻就不能交互"));

            if (require2DFaceMatch.boolValue &&
                cameraModeController.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "没有找到相机模式控制器，无法执行 2D 投影校验，将按普通 3D 距离交互。",
                    MessageType.Warning);
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(
                    "交互输入由玩家交互传感器统一仲裁。",
                    MessageType.None);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }

        private static void DrawTagField(
            SerializedProperty property,
            GUIContent label)
        {
            string selected = EditorGUILayout.TagField(
                label,
                property.stringValue);
            if (!string.Equals(
                    selected,
                    property.stringValue,
                    StringComparison.Ordinal))
            {
                property.stringValue = selected;
            }
        }

        private static void WarnAboutDisabledButtonPlatforms(
            SerializedProperty platforms)
        {
            if (platforms == null || !platforms.isArray)
            {
                return;
            }

            for (int index = 0;
                 index < platforms.arraySize;
                 index++)
            {
                PlatformMove platform =
                    platforms.GetArrayElementAtIndex(index)
                        .objectReferenceValue as PlatformMove;
                if (platform == null || platform.UseButtonFeature)
                {
                    continue;
                }

                EditorGUILayout.HelpBox(
                    $"平台 {platform.name} 尚未开启“按钮联动”，按钮信号不会生效。",
                    MessageType.Warning);
            }
        }
    }
}
