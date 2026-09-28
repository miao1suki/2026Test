using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.Achievements.Editor
{
    [CustomEditor(typeof(AchievementDetector))]
    public sealed class AchievementDetectorEditor :
        UnityEditor.Editor
    {
        private SerializedProperty bindings;

        private void OnEnable()
        {
            bindings = serializedObject.FindProperty("bindings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AchievementEditorHelp.DrawDetectorHelp();

            EditorGUILayout.HelpBox(
                "检测组件只转发信号，不判断成就是否完成。\n" +
                "官方 UI 组件可直接监听；自定义脚本需实现 IAchievementSignalSource。",
                MessageType.Info);

            for (int index = 0;
                 index < bindings.arraySize;
                 index++)
            {
                DrawBinding(index);
            }

            if (GUILayout.Button("＋ 添加检测项"))
            {
                bindings.InsertArrayElementAtIndex(
                    bindings.arraySize);
                SerializedProperty item =
                    bindings.GetArrayElementAtIndex(
                        bindings.arraySize - 1);
                item.FindPropertyRelative("enabled")
                    .boolValue = true;
                item.FindPropertyRelative("conditionId")
                    .stringValue = "0001";
                item.FindPropertyRelative("count")
                    .intValue = 1;
                item.FindPropertyRelative("progress")
                    .floatValue = 1f;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBinding(int index)
        {
            SerializedProperty binding =
                bindings.GetArrayElementAtIndex(index);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(
                binding.FindPropertyRelative("enabled"),
                GUILayout.Width(42f));
            EditorGUILayout.LabelField(
                "检测项 " + (index + 1),
                EditorStyles.boldLabel);
            if (GUILayout.Button("×", GUILayout.Width(24f)))
            {
                bindings.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(
                binding.FindPropertyRelative("source"),
                new GUIContent("源组件"));
            EditorGUILayout.PropertyField(
                binding.FindPropertyRelative("signalName"),
                new GUIContent(
                    "信号名",
                    "自定义 IAchievementSignalSource 使用；官方组件可留空"));
            SerializedProperty lookupMode =
                binding.FindPropertyRelative("lookupMode");
            string[] lookupOptions =
            {
                "按成就编号",
                "按成就名字"
            };
            int selectedLookup = EditorGUILayout.Popup(
                "查找方式",
                Mathf.Clamp(
                    lookupMode.enumValueIndex,
                    0,
                    lookupOptions.Length - 1),
                lookupOptions);
            lookupMode.enumValueIndex = selectedLookup;
            EditorGUILayout.PropertyField(
                binding.FindPropertyRelative("achievementId"),
                new GUIContent("成就编号"));
            EditorGUILayout.PropertyField(
                binding.FindPropertyRelative("achievementName"),
                new GUIContent("成就名字"));
            EditorGUILayout.PropertyField(
                binding.FindPropertyRelative("conditionId"),
                new GUIContent("条件编号"));
            EditorGUILayout.PropertyField(
                binding.FindPropertyRelative("count"),
                new GUIContent("增加计数"));
            EditorGUILayout.PropertyField(
                binding.FindPropertyRelative("progress"),
                new GUIContent(
                    "增加进度（%）",
                    "1 表示增加 1%"));

            Component source =
                binding.FindPropertyRelative("source")
                    .objectReferenceValue as Component;
            if (source == null)
            {
                EditorGUILayout.HelpBox(
                    "源组件为空。",
                    MessageType.Warning);
            }
            else if (!(source is IAchievementSignalSource) &&
                     !(source is UnityEngine.UI.Button) &&
                     !(source is UnityEngine.UI.Slider) &&
                     !(source is UnityEngine.UI.Toggle) &&
                     !(source is UnityEngine.UI.Dropdown) &&
                     !(source is UnityEngine.UI.InputField) &&
                     !(source is UnityEngine.UI.ScrollRect))
            {
                EditorGUILayout.HelpBox(
                    "该组件没有可识别的成就信号接口，也不会被检测器订阅。",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
