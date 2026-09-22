using System;
using Project.PlatformPaths;
using UnityEditor;
using UnityEngine;

namespace Project.PlatformPaths.Editor
{
    [CustomEditor(typeof(PlatformMove))]
    public sealed class PlatformMoveEditor : UnityEditor.Editor
    {
        private SerializedProperty ropePlatform;
        private SerializedProperty script;
        private SerializedProperty network;
        private SerializedProperty cameraModeController;
        private SerializedProperty projectionDirection;
        private SerializedProperty moveMode;
        private SerializedProperty moveSpeed;
        private SerializedProperty playerTag;
        private SerializedProperty passengerCheckHeight;
        private SerializedProperty passengerCheckWidth;
        private SerializedProperty useButtonFeature;
        private SerializedProperty buttonWaitDuration;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            ropePlatform = serializedObject.FindProperty("ropePlatform");
            network = serializedObject.FindProperty("network");
            cameraModeController =
                serializedObject.FindProperty(
                    "cameraModeController");
            projectionDirection =
                serializedObject.FindProperty("projectionDirection");
            moveMode = serializedObject.FindProperty("moveMode");
            moveSpeed = serializedObject.FindProperty("moveSpeed");
            playerTag = serializedObject.FindProperty("playerTag");
            passengerCheckHeight =
                serializedObject.FindProperty("passengerCheckHeight");
            passengerCheckWidth =
                serializedObject.FindProperty("passengerCheckWidth");
            useButtonFeature =
                serializedObject.FindProperty("useButtonFeature");
            buttonWaitDuration =
                serializedObject.FindProperty("buttonWaitDuration");
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
                "平台移动组件。按压触发模式需要玩家通过标签识别并站在平台上；" +
                "游荡模式沿当前投影方向的绳子路径往返移动。",
                MessageType.Info);

            DrawSection("引用");
            EditorGUILayout.PropertyField(
                ropePlatform,
                new GUIContent("平台绑定", "平台当前绑定的绳子和端点数据"));
            EditorGUILayout.PropertyField(
                network,
                new GUIContent("绳子网络", "提供四方向路径查询的 RopePathNetwork"));
            EditorGUILayout.PropertyField(
                cameraModeController,
                new GUIContent(
                    "相机模式",
                    "3D 视角下平台会停在下一个瞬移点前"));

            DrawSection("通用设置");
            EditorGUILayout.PropertyField(
                projectionDirection,
                new GUIContent(
                    "备用视角",
                    "没有绳子网络时使用的方向；有网络时自动跟随当前投影方向"));
            EditorGUILayout.PropertyField(
                moveMode,
                new GUIContent("运行模式", "游荡模式或按压触发模式"));
            EditorGUILayout.PropertyField(
                moveSpeed,
                new GUIContent("移动速度", "平台沿绳子移动的速度"));

            DrawSection("玩家检测");
            DrawTagPopup(
                playerTag,
                new GUIContent("玩家标签", "用于识别玩家是否站在平台上"));

            PlatformMove controller = (PlatformMove)target;
            if (controller.GetComponent<Collider>() == null)
            {
                EditorGUILayout.HelpBox(
                    "平台缺少 Collider，无法检测玩家。",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.PropertyField(
                    passengerCheckHeight,
                    new GUIContent("检测高度", "平台顶面上方的乘客检测高度"));
                EditorGUILayout.PropertyField(
                    passengerCheckWidth,
                    new GUIContent("检测宽度", "平台顶面横向检测范围"));
            }

            PlatformMoveMode selectedMode =
                (PlatformMoveMode)moveMode.enumValueIndex;
            if (selectedMode == PlatformMoveMode.PressTrigger)
            {
                EditorGUILayout.HelpBox(
                    "初始保持静止，玩家站上平台后开始移动；玩家离开后立即回到初始位置。",
                    MessageType.None);
            }
            else
            {
                DrawSection("游荡模式");
                EditorGUILayout.PropertyField(
                    useButtonFeature,
                    new GUIContent("启用按钮联动", "开启后允许外部按钮把平台叫到最近端点"));

                if (useButtonFeature.boolValue)
                {
                    EditorGUILayout.PropertyField(
                        buttonWaitDuration,
                        new GUIContent("按钮等待时间", "平台到达按钮端点后等待的秒数"));
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "外部按钮接口：ReceiveButtonSignal(Transform / Vector3) 和 CancelButtonSignal()",
                MessageType.None);

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
    }
}
