using System;
using UnityEditor;
using UnityEngine;

namespace Project.PlatformPaths.Editor
{
    [CustomEditor(typeof(PlatformRiderZone))]
    public sealed class PlatformRiderZoneEditor : UnityEditor.Editor
    {
        private SerializedProperty script;
        private SerializedProperty platform;
        private SerializedProperty playerTag;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            platform = serializedObject.FindProperty("platform");
            playerTag = serializedObject.FindProperty("playerTag");
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
                "平台乘客判定区。PlatformMove 运行时会自动创建并配置，" +
                "只有当玩家进入这个区域并接近平台表面时才会被承载。",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                platform,
                new GUIContent("所属平台"));
            DrawTagPopup(
                playerTag,
                new GUIContent("玩家标签"));

            if (platform.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "所属平台未绑定。运行时会由 PlatformMove 自动写入。",
                    MessageType.None);
            }

            BoxCollider zoneCollider =
                ((PlatformRiderZone)target)
                .GetComponent<BoxCollider>();
            if (zoneCollider == null)
            {
                EditorGUILayout.HelpBox(
                    "缺少 BoxCollider，无法检测乘客。",
                    MessageType.Error);
            }
            else
            {
                DrawSection("判定形状");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Toggle(
                        "启用",
                        zoneCollider.enabled);
                    EditorGUILayout.Toggle(
                        "触发器",
                        zoneCollider.isTrigger);
                    EditorGUILayout.Vector3Field(
                        "尺寸",
                        zoneCollider.size);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawTagPopup(
            SerializedProperty property,
            GUIContent label)
        {
            string[] tags =
                UnityEditorInternal.InternalEditorUtility.tags;
            int currentIndex =
                Array.IndexOf(tags, property.stringValue);
            if (currentIndex < 0)
            {
                EditorGUILayout.PropertyField(
                    property,
                    label);
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

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }
    }
}
