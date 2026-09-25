using UnityEditor;
using UnityEngine;

namespace Project.LadderPaths.Editor
{
    [CustomEditor(typeof(LadderClimbSensor))]
    public sealed class LadderClimbSensorEditor :
        UnityEditor.Editor
    {
        private SerializedProperty script;
        private SerializedProperty network;
        private SerializedProperty stateReceiver;
        private SerializedProperty projectionDirection;
        private SerializedProperty useProjectionFallback;
        private SerializedProperty projectionQueryInterval;
        private SerializedProperty horizontalPadding;
        private SerializedProperty verticalPadding;
        private SerializedProperty obstructionMask;
        private SerializedProperty topExitGraceSeconds;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            network = serializedObject.FindProperty("network");
            stateReceiver =
                serializedObject.FindProperty("stateReceiver");
            projectionDirection =
                serializedObject.FindProperty("projectionDirection");
            useProjectionFallback =
                serializedObject.FindProperty("useProjectionFallback");
            projectionQueryInterval =
                serializedObject.FindProperty("projectionQueryInterval");
            horizontalPadding =
                serializedObject.FindProperty("horizontalPadding");
            verticalPadding =
                serializedObject.FindProperty("verticalPadding");
            obstructionMask =
                serializedObject.FindProperty("obstructionMask");
            topExitGraceSeconds =
                serializedObject.FindProperty("topExitGraceSeconds");
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
                "梯子检测组件。真实接触优先使用梯子 Trigger；" +
                "没有接触时可按当前投影方向做低频兜底检测。" +
                "玩家靠近只会记录候选，按 W/S 才会进入攀爬。",
                MessageType.Info);

            DrawSection("连接");
            EditorGUILayout.PropertyField(
                network,
                new GUIContent(
                    "梯子网络",
                    "用于投影兜底检测；留空时只响应真实 Trigger"));
            EditorGUILayout.PropertyField(
                stateReceiver,
                new GUIContent(
                    "状态接收者",
                    "留空时自动查找同物体上实现 ILadderClimbStateReceiver 的组件"));
            EditorGUILayout.PropertyField(
                projectionDirection,
                new GUIContent(
                    "投影方向",
                    "2D 方向切换时必须同步到当前视角"));

            DrawSection("投影兜底");
            EditorGUILayout.PropertyField(
                useProjectionFallback,
                new GUIContent(
                    "启用投影兜底",
                    "Trigger 未接触时按投影范围寻找梯子"));
            if (useProjectionFallback.boolValue)
            {
                EditorGUILayout.PropertyField(
                    projectionQueryInterval,
                    new GUIContent(
                        "检测间隔",
                        "投影查询的最小间隔，默认 0.05 秒"));
                EditorGUILayout.PropertyField(
                    horizontalPadding,
                    new GUIContent("水平宽容"));
                EditorGUILayout.PropertyField(
                    verticalPadding,
                    new GUIContent("垂直宽容"));
                EditorGUILayout.PropertyField(
                    obstructionMask,
                    new GUIContent(
                        "遮挡层",
                        "只勾真正会挡住玩家和梯子的碰撞层"));
            }

            DrawSection("离开规则");
            EditorGUILayout.PropertyField(
                topExitGraceSeconds,
                new GUIContent(
                    "顶部宽限时间",
                    "爬出顶部逻辑端后保持攀爬状态的时间"));

            serializedObject.ApplyModifiedProperties();

            if (!Application.isPlaying)
            {
                return;
            }

            LadderClimbSensor sensor =
                (LadderClimbSensor)target;
            DrawSection("运行状态");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    "正在攀爬",
                    sensor.IsClimbing);
                EditorGUILayout.ObjectField(
                    "当前梯子段",
                    sensor.ActiveSegment,
                    typeof(LadderSegment),
                    true);
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
