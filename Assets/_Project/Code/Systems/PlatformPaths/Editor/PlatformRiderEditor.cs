using UnityEditor;
using UnityEngine;

namespace Project.PlatformPaths.Editor
{
    [CustomEditor(typeof(PlatformRider))]
    public sealed class PlatformRiderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("m_Script"),
                    new GUIContent("脚本"));
            }

            EditorGUILayout.HelpBox(
                "平台承载标记。玩家站上移动平台后由 PlatformMove 自动挂载到 " +
                "__RiderAnchor，运行时不需要手动配置。",
                MessageType.Info);

            PlatformRider rider = (PlatformRider)target;
            if (rider.GetComponent<Rigidbody>() == null)
            {
                EditorGUILayout.HelpBox(
                    "没有 Rigidbody 时仍然可以跟随平台，但不会切换运动学和重力状态。",
                    MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(
                "运行状态",
                EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    "正在承载",
                    Application.isPlaying && rider.IsRiding);
            }
        }
    }
}
