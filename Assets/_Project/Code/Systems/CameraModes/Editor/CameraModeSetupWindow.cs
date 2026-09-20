using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.CameraModes.Editor
{
    public sealed class CameraModeSetupWindow : EditorWindow
    {
        private Camera selectedCamera;
        private Transform selectedFollowTarget;

        [MenuItem("Tools/2026Test/2D-3D 相机配置")]
        public static void OpenWindow()
        {
            CameraModeSetupWindow window = GetWindow<CameraModeSetupWindow>();
            window.titleContent = new GUIContent("2D-3D 相机");
            window.minSize = new Vector2(380f, 310f);
            window.Show();
        }

        private void OnEnable()
        {
            if (selectedCamera == null)
            {
                selectedCamera = Camera.main;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("平台跳跃相机配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "选择场景相机与跟随点，然后创建或更新控制器。该工具不会修改 URP Renderer 或 Render Pipeline Asset。",
                MessageType.Info);

            selectedCamera = (Camera)EditorGUILayout.ObjectField(
                "受控相机",
                selectedCamera,
                typeof(Camera),
                true);
            selectedFollowTarget = (Transform)EditorGUILayout.ObjectField(
                "跟随目标",
                selectedFollowTarget,
                typeof(Transform),
                true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("使用 Main Camera"))
                {
                    selectedCamera = Camera.main;
                }

                if (GUILayout.Button("创建临时跟随点"))
                {
                    CreateFollowTarget();
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(selectedCamera == null))
            {
                if (GUILayout.Button("创建 / 更新 CameraModeController", GUILayout.Height(32f)))
                {
                    ConfigureController();
                }
            }

            CameraModeController controller = selectedCamera != null
                ? selectedCamera.GetComponent<CameraModeController>()
                : null;

            if (controller == null)
            {
                EditorGUILayout.HelpBox("当前相机尚未配置 CameraModeController。", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("快速控制", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Application.isPlaying ? "切到 2D" : "预览 2D"))
                {
                    ChangeMode(controller, CameraViewMode.Side2D);
                }

                if (GUILayout.Button(Application.isPlaying ? "切到 3D" : "预览 3D"))
                {
                    ChangeMode(controller, CameraViewMode.Perspective3D);
                }
            }

            EditorGUILayout.LabelField("当前模式", controller.CurrentMode.ToString());
            EditorGUILayout.LabelField("目标模式", controller.TargetMode.ToString());
            EditorGUILayout.LabelField("切换状态", controller.IsTransitioning ? "切换中" : "稳定");

            if (GUILayout.Button("在 Inspector 中定位控制器"))
            {
                Selection.activeObject = controller;
                EditorGUIUtility.PingObject(controller);
            }
        }

        private void CreateFollowTarget()
        {
            GameObject targetObject = new GameObject("CameraFollowTarget");
            Undo.RegisterCreatedObjectUndo(targetObject, "Create Camera Follow Target");
            if (Selection.activeTransform != null)
            {
                targetObject.transform.position = Selection.activeTransform.position;
            }

            selectedFollowTarget = targetObject.transform;
            Selection.activeObject = targetObject;
            MarkSceneDirty(targetObject);
        }

        private void ConfigureController()
        {
            CameraModeController controller = selectedCamera.GetComponent<CameraModeController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<CameraModeController>(selectedCamera.gameObject);
            }

            Undo.RecordObject(controller, "Configure Camera Mode Controller");
            Undo.RecordObject(selectedCamera, "Configure Camera Mode Controller");
            Undo.RecordObject(selectedCamera.transform, "Configure Camera Mode Controller");
            controller.Configure(selectedCamera, selectedFollowTarget, true);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(selectedCamera);
            EditorUtility.SetDirty(selectedCamera.transform);
            MarkSceneDirty(selectedCamera.gameObject);
            Selection.activeObject = controller;
        }

        private static void ChangeMode(CameraModeController controller, CameraViewMode mode)
        {
            if (Application.isPlaying)
            {
                controller.SwitchMode(mode);
                return;
            }

            Camera camera = controller.ControlledCamera;
            Undo.RecordObject(controller, "Preview Camera View Mode");
            if (camera != null)
            {
                Undo.RecordObject(camera, "Preview Camera View Mode");
                Undo.RecordObject(camera.transform, "Preview Camera View Mode");
            }

            controller.SnapToMode(mode, false);
            EditorUtility.SetDirty(controller);
            if (camera != null)
            {
                EditorUtility.SetDirty(camera);
                EditorUtility.SetDirty(camera.transform);
            }

            MarkSceneDirty(controller.gameObject);
            SceneView.RepaintAll();
        }

        private static void MarkSceneDirty(GameObject gameObject)
        {
            if (gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
    }
}
