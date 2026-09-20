using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.CameraModes.Editor
{
    [CustomEditor(typeof(CameraModeController))]
    [CanEditMultipleObjects]
    public sealed class CameraModeControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty controlledCamera;
        private SerializedProperty followTarget;
        private SerializedProperty fallbackFocusPoint;
        private SerializedProperty initialMode;
        private SerializedProperty side2D;
        private SerializedProperty perspective3D;
        private SerializedProperty transition;
        private SerializedProperty followSmoothTime;
        private SerializedProperty onTransitionStarted;
        private SerializedProperty onModeChanged;

        private void OnEnable()
        {
            controlledCamera = serializedObject.FindProperty("controlledCamera");
            followTarget = serializedObject.FindProperty("followTarget");
            fallbackFocusPoint = serializedObject.FindProperty("fallbackFocusPoint");
            initialMode = serializedObject.FindProperty("initialMode");
            side2D = serializedObject.FindProperty("side2D");
            perspective3D = serializedObject.FindProperty("perspective3D");
            transition = serializedObject.FindProperty("transition");
            followSmoothTime = serializedObject.FindProperty("followSmoothTime");
            onTransitionStarted = serializedObject.FindProperty("onTransitionStarted");
            onModeChanged = serializedObject.FindProperty("onModeChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "2D 使用平视正交相机；3D 使用无左右偏移的斜上方透视相机。运行时切换可被反向打断。",
                MessageType.Info);

            DrawSection("引用", controlledCamera, followTarget, fallbackFocusPoint);
            DrawSection("启动模式", initialMode);
            DrawSection("2D 平台视角", side2D);
            DrawSection("3D 俯视视角", perspective3D);
            DrawSection("切换与跟随", transition, followSmoothTime);
            DrawSection("事件", onTransitionStarted, onModeChanged);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawRuntimeStatus();
            DrawModeButtons();

            if (GUILayout.Button("打开 2D / 3D 相机配置工具"))
            {
                CameraModeSetupWindow.OpenWindow();
            }
        }

        private static void DrawSection(string title, params SerializedProperty[] properties)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            foreach (SerializedProperty property in properties)
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }

        private void DrawRuntimeStatus()
        {
            if (targets.Length != 1)
            {
                return;
            }

            CameraModeController controller = (CameraModeController)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("当前模式", controller.CurrentMode);
                EditorGUILayout.EnumPopup("目标模式", controller.TargetMode);
                EditorGUILayout.Toggle("正在切换", controller.IsTransitioning);
                EditorGUILayout.Slider("切换进度", controller.NormalizedTransitionTime, 0f, 1f);
            }
        }

        private void DrawModeButtons()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                Application.isPlaying ? "运行时控制" : "编辑模式预览",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Application.isPlaying ? "切换到 2D" : "预览 2D"))
                {
                    ApplyMode(CameraViewMode.Side2D);
                }

                if (GUILayout.Button(Application.isPlaying ? "切换到 3D" : "预览 3D"))
                {
                    ApplyMode(CameraViewMode.Perspective3D);
                }
            }
        }

        private void ApplyMode(CameraViewMode mode)
        {
            foreach (Object selectedTarget in targets)
            {
                CameraModeController controller = (CameraModeController)selectedTarget;
                Camera camera = controller.ControlledCamera;
                Undo.RecordObject(controller, "Change Camera View Mode");
                if (camera != null)
                {
                    Undo.RecordObject(camera, "Change Camera View Mode");
                    Undo.RecordObject(camera.transform, "Change Camera View Mode");
                }

                if (Application.isPlaying)
                {
                    controller.SwitchMode(mode);
                }
                else
                {
                    controller.SnapToMode(mode, false);
                    EditorUtility.SetDirty(controller);
                    if (camera != null)
                    {
                        EditorUtility.SetDirty(camera);
                        EditorUtility.SetDirty(camera.transform);
                    }

                    if (controller.gameObject.scene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                    }
                }
            }

            SceneView.RepaintAll();
        }
    }
}
