using System.Collections.Generic;
using Project.InputAbstraction;
using Project.LadderPaths;
using Project.PlatformPaths;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace Project.Player.Editor
{
    [CustomEditor(typeof(PlayerController))]
    public sealed class PlayerControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty script;
        private SerializedProperty motor;
        private SerializedProperty bodyCollider;
        private SerializedProperty cameraFollow;
        private SerializedProperty cameraMode;
        private SerializedProperty actionRunner;
        private SerializedProperty platformRider;
        private SerializedProperty ladderSensor;
        private SerializedProperty ropeNetworks;
        private SerializedProperty actionBindings;
        private SerializedProperty moveSpeed;
        private SerializedProperty jumpSpeed;
        private SerializedProperty climbSpeed;
        private SerializedProperty lookSensitivity;
        private SerializedProperty sprintMultiplier;
        private SerializedProperty crouchHeight;
        private SerializedProperty crouchSpeedMultiplier;
        private SerializedProperty crouchBehavior;
        private SerializedProperty groundCheckDistance;
        private SerializedProperty coyoteTime;
        private SerializedProperty jumpBufferTime;
        private SerializedProperty groundMask;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            motor = serializedObject.FindProperty("motor");
            bodyCollider = serializedObject.FindProperty("bodyCollider");
            cameraFollow = serializedObject.FindProperty("cameraFollow");
            cameraMode = serializedObject.FindProperty("cameraMode");
            actionRunner = serializedObject.FindProperty("actionRunner");
            platformRider = serializedObject.FindProperty("platformRider");
            ladderSensor = serializedObject.FindProperty("ladderSensor");
            ropeNetworks = serializedObject.FindProperty("ropeNetworks");
            actionBindings = serializedObject.FindProperty("actionBindings");
            moveSpeed = serializedObject.FindProperty("moveSpeed");
            jumpSpeed = serializedObject.FindProperty("jumpSpeed");
            climbSpeed = serializedObject.FindProperty("climbSpeed");
            lookSensitivity = serializedObject.FindProperty("lookSensitivity");
            sprintMultiplier = serializedObject.FindProperty("sprintMultiplier");
            crouchHeight = serializedObject.FindProperty("crouchHeight");
            crouchSpeedMultiplier =
                serializedObject.FindProperty("crouchSpeedMultiplier");
            crouchBehavior =
                serializedObject.FindProperty("crouchBehavior");
            groundCheckDistance =
                serializedObject.FindProperty("groundCheckDistance");
            coyoteTime = serializedObject.FindProperty("coyoteTime");
            jumpBufferTime =
                serializedObject.FindProperty("jumpBufferTime");
            groundMask = serializedObject.FindProperty("groundMask");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawScript();

            DrawSection("引用");
            DrawReference(motor, "刚体", "玩家移动和攀爬使用的 Rigidbody");
            DrawReference(bodyCollider, "身体碰撞体", "地面检测使用的 CapsuleCollider");
            DrawReference(cameraFollow, "相机跟随", "2D/3D 跟随与视角申请");
            DrawReference(cameraMode, "相机模式", "2D/3D 权威模式控制器");
            DrawReference(actionRunner, "动作运行器", "播放 ActSO 中的 Timeline");
            DrawReference(platformRider, "平台承载", "站在移动平台上时临时承载玩家");
            DrawReference(ladderSensor, "梯子传感器", "检测玩家附近的梯子");
            EditorGUILayout.PropertyField(
                ropeNetworks,
                new GUIContent(
                    "绳索网络",
                    "2D 转向时同步投影方向的绳索网络列表"),
                true);

            DrawSection("输入动作绑定");
            DrawActionBindings();

            bool jumpBound = HasBinding(InputActionId.Jump);
            if (jumpBound)
            {
                EditorGUILayout.HelpBox(
                    "Jump 已绑定 ActSO，内置物理跳跃不会执行；" +
                    "跳跃位移和表现由对应 Timeline 负责。",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Jump 未绑定 ActSO，使用内置物理跳跃。",
                    MessageType.None);
            }

            DrawSection("基础移动");
            EditorGUILayout.PropertyField(
                moveSpeed,
                new GUIContent("移动速度"));
            EditorGUILayout.PropertyField(
                sprintMultiplier,
                new GUIContent("冲刺倍率"));
            EditorGUILayout.PropertyField(
                lookSensitivity,
                new GUIContent("视角灵敏度"));
            EditorGUILayout.PropertyField(
                climbSpeed,
                new GUIContent("攀爬速度"));
            EditorGUILayout.PropertyField(
                crouchBehavior,
                new GUIContent(
                    "蹲下方式",
                    "按住蹲下，或点击切换"));
            if (crouchBehavior.enumValueIndex ==
                (int)PlayerCrouchBehavior.Toggle)
            {
                EditorGUILayout.HelpBox(
                    "点击切换蹲下状态；跳跃和主动动作会取消蹲下。",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "按住保持蹲下，松开后恢复站立。",
                    MessageType.None);
            }
            EditorGUILayout.PropertyField(
                crouchHeight,
                new GUIContent("蹲下高度"));
            EditorGUILayout.PropertyField(
                crouchSpeedMultiplier,
                new GUIContent("蹲下速度倍率"));

            DrawSection("跳跃和落地");
            if (!jumpBound)
            {
                EditorGUILayout.PropertyField(
                    jumpSpeed,
                    new GUIContent("跳跃速度"));
                EditorGUILayout.PropertyField(
                    coyoteTime,
                    new GUIContent("离地宽限", "离开地面后仍可起跳的时间"));
                EditorGUILayout.PropertyField(
                    jumpBufferTime,
                    new GUIContent("跳跃缓冲", "落地前提前按跳跃的保留时间"));
            }

            EditorGUILayout.PropertyField(
                groundCheckDistance,
                new GUIContent("地面检测距离"));
            EditorGUILayout.PropertyField(
                groundMask,
                new GUIContent("地面层"));

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                DrawRuntimeStatus();
            }

            DrawSetupTools();
        }

        private void DrawScript()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    script,
                    new GUIContent("脚本"));
            }
        }

        private void DrawActionBindings()
        {
            for (int index = 0;
                 index < actionBindings.arraySize;
                 index++)
            {
                SerializedProperty element =
                    actionBindings.GetArrayElementAtIndex(index);
                SerializedProperty inputAction =
                    element.FindPropertyRelative("inputAction");
                SerializedProperty action =
                    element.FindPropertyRelative("action");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(
                        inputAction,
                        GUIContent.none,
                        GUILayout.Width(145f));
                    EditorGUILayout.PropertyField(
                        action,
                        GUIContent.none);
                    if (GUILayout.Button(
                            "×",
                            GUILayout.Width(24f)))
                    {
                        actionBindings.DeleteArrayElementAtIndex(index);
                        break;
                    }
                }
            }

            if (GUILayout.Button("＋ 添加动作绑定"))
            {
                int index = actionBindings.arraySize;
                actionBindings.InsertArrayElementAtIndex(index);
                SerializedProperty element =
                    actionBindings.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("inputAction")
                    .enumValueIndex = 0;
                element.FindPropertyRelative("action")
                    .objectReferenceValue = null;
            }
        }

        private bool HasBinding(InputActionId inputAction)
        {
            for (int index = 0;
                 index < actionBindings.arraySize;
                 index++)
            {
                SerializedProperty element =
                    actionBindings.GetArrayElementAtIndex(index);
                SerializedProperty action =
                    element.FindPropertyRelative("action");
                SerializedProperty input =
                    element.FindPropertyRelative("inputAction");
                if (action.objectReferenceValue != null &&
                    input.enumValueIndex == (int)inputAction)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawRuntimeStatus()
        {
            PlayerController controller =
                (PlayerController)target;
            DrawSection("运行状态");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup(
                    "当前状态",
                    controller.CurrentStateId);
                EditorGUILayout.Toggle(
                    "已落地",
                    controller.IsGrounded);
                EditorGUILayout.Toggle(
                    "正在蹲下",
                    controller.IsCrouched);
                EditorGUILayout.Toggle(
                    "控制被锁定",
                    controller.IsControlLocked);
                EditorGUILayout.Toggle(
                    "正在平台承载",
                    controller.IsRidingPlatform);
                EditorGUILayout.EnumPopup(
                    "当前投影",
                    controller.ProjectionDirection);
            }
        }

        private void DrawSetupTools()
        {
            DrawSection("玩家组件检查");
            string report = BuildSetupReport();
            EditorGUILayout.HelpBox(
                report ?? "玩家核心组件和本物体引用齐全。",
                report == null
                    ? MessageType.Info
                    : MessageType.Warning);
            using (new EditorGUI.DisabledScope(
                       Application.isPlaying))
            {
                if (GUILayout.Button("一键补齐玩家组件"))
                {
                    EnsurePlayerComponents();
                }
            }
        }

        private string BuildSetupReport()
        {
            GameObject gameObject =
                ((PlayerController)target).gameObject;
            List<string> issues = new List<string>();
            if (gameObject.tag != "Player")
            {
                issues.Add("GameObject Tag 不是 Player");
            }

            if (!gameObject.TryGetComponent(
                    out Rigidbody _))
            {
                issues.Add("缺少 Rigidbody");
            }

            if (!gameObject.TryGetComponent(
                    out CapsuleCollider _))
            {
                issues.Add("缺少 CapsuleCollider");
            }

            if (!gameObject.TryGetComponent(
                    out PlayableDirector _))
            {
                issues.Add("缺少 PlayableDirector");
            }

            if (!gameObject.TryGetComponent(
                    out PlayerActionRunner _))
            {
                issues.Add("缺少 PlayerActionRunner");
            }

            if (!gameObject.TryGetComponent(
                    out TimelineActorHost _))
            {
                issues.Add("缺少 TimelineActorHost");
            }

            if (!gameObject.TryGetComponent(
                    out PlatformRider _))
            {
                issues.Add("缺少 PlatformRider");
            }

            if (motor.objectReferenceValue == null)
            {
                issues.Add("PlayerController 未引用 Rigidbody");
            }

            if (bodyCollider.objectReferenceValue == null)
            {
                issues.Add(
                    "PlayerController 未引用 CapsuleCollider");
            }

            if (actionRunner.objectReferenceValue == null)
            {
                issues.Add(
                    "PlayerController 未引用 PlayerActionRunner");
            }

            if (issues.Count == 0)
            {
                return null;
            }

            return "发现 " + issues.Count + " 项配置问题：\n· " +
                   string.Join("\n· ", issues);
        }

        private void EnsurePlayerComponents()
        {
            PlayerController controller =
                (PlayerController)target;
            GameObject gameObject = controller.gameObject;
            Undo.RecordObject(gameObject, "补齐玩家组件");
            if (gameObject.tag != "Player")
            {
                gameObject.tag = "Player";
            }

            Rigidbody motorComponent =
                GetOrAddComponent<Rigidbody>(gameObject);
            CapsuleCollider colliderComponent =
                GetOrAddComponent<CapsuleCollider>(gameObject);
            PlayableDirector director =
                GetOrAddComponent<PlayableDirector>(gameObject);
            PlayerActionRunner runner =
                GetOrAddComponent<PlayerActionRunner>(gameObject);
            TimelineActorHost actorHost =
                GetOrAddComponent<TimelineActorHost>(gameObject);
            PlatformRider riderComponent =
                GetOrAddComponent<PlatformRider>(gameObject);

            if (actorHost.attackPoint == null)
            {
                Undo.RecordObject(
                    actorHost,
                    "补齐玩家组件");
                actorHost.attackPoint =
                    controller.transform;
                EditorUtility.SetDirty(actorHost);
            }

            serializedObject.Update();
            motor.objectReferenceValue = motorComponent;
            bodyCollider.objectReferenceValue = colliderComponent;
            actionRunner.objectReferenceValue = runner;
            platformRider.objectReferenceValue = riderComponent;
            ladderSensor.objectReferenceValue =
                gameObject.GetComponent<LadderClimbSensor>();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(director);
        }

        private static T GetOrAddComponent<T>(
            GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(
                    out T component)
                ? component
                : Undo.AddComponent<T>(gameObject);
        }

        private static void DrawReference(
            SerializedProperty property,
            string label,
            string tooltip)
        {
            EditorGUILayout.PropertyField(
                property,
                new GUIContent(label, tooltip));
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
