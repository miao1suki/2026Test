using System;
using Project.PlatformPaths;
using UnityEditor;
using UnityEngine;

namespace Project.PlatformPaths.Editor
{
    [CustomEditor(typeof(PressurePlate))]
    public sealed class PressurePlateEditor : UnityEditor.Editor
    {
        private SerializedProperty script;
        private SerializedProperty commandedPlatforms;
        private SerializedProperty playerTag;
        private SerializedProperty checkHeight;
        private SerializedProperty checkWidth;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            commandedPlatforms =
                serializedObject.FindProperty("commandedPlatforms");
            playerTag = serializedObject.FindProperty("playerTag");
            checkHeight = serializedObject.FindProperty("checkHeight");
            checkWidth = serializedObject.FindProperty("checkWidth");
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
                "压力板检测到指定标签的玩家站上来后，会向所有绑定平台发送一次按钮信号。" +
                "压力板离开后不会取消信号，恢复游荡由平台自己的倒计时或玩家踩上平台决定。",
                MessageType.Info);

            DrawSection("指挥平台");
            EditorGUILayout.PropertyField(
                commandedPlatforms,
                new GUIContent("绑定平台列表", "可以同时绑定多个 PlatformMove"),
                true);
            WarnAboutDisabledButtonPlatforms(
                commandedPlatforms);

            DrawSection("玩家检测");
            DrawTagPopup(
                playerTag,
                new GUIContent("玩家标签", "用于识别压力板上的玩家"));

            PressurePlate plate = (PressurePlate)target;
            if (plate.GetComponent<Collider>() == null)
            {
                EditorGUILayout.HelpBox(
                    "压力板缺少 Collider，无法检测玩家。",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.PropertyField(
                    checkHeight,
                    new GUIContent("检测高度", "压力板上方的检测高度"));
                EditorGUILayout.PropertyField(
                    checkWidth,
                    new GUIContent("检测宽度", "压力板顶面的横向检测范围"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void DrawTagPopup(
            SerializedProperty property,
            GUIContent label)
        {
            string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
            int currentIndex = Array.IndexOf(tags, property.stringValue);
            if (currentIndex < 0)
            {
                EditorGUILayout.PropertyField(property, label);
                return;
            }

            int selectedIndex = EditorGUILayout.Popup(
                label,
                currentIndex,
                tags);
            if (selectedIndex != currentIndex)
            {
                property.stringValue = tags[selectedIndex];
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
                    $"平台 {platform.name} 尚未开启“按钮联动”，压力板信号不会生效。",
                    MessageType.Warning);
            }
        }
    }
}
