using System;
using UnityEngine;
using UnityEngine.Events;

namespace Project.CameraModes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CameraControlManager))]
    [DefaultExecutionOrder(100)]
    public sealed class CameraModeController :
        MonoBehaviour,
        ICameraControlSource,
        ICameraViewModeSwitcher
    {
        [Serializable]
        public sealed class Side2DSettings
        {
            [Tooltip("相机沿世界 Z 轴后退的距离。")]
            [Min(0.01f)] public float distance = 10f;

            [Tooltip("正交相机的可视高度参数。")]
            [Min(0.01f)] public float orthographicSize = 5f;

            [Tooltip("相对焦点的竖直观察偏移。不会造成左右偏移。")]
            public float verticalOffset = 1f;

            [Tooltip("相对焦点的纵深观察偏移。")]
            public float depthOffset;

            [Tooltip("2D 平面朝向。0=看向世界 +Z，90=向右转过一个面。")]
            public float yawDegrees;
        }

        [Serializable]
        public sealed class Perspective3DSettings
        {
            [Tooltip("相机到观察中心的直线距离。")]
            [Min(0.01f)] public float distance = 12f;

            [Tooltip("透视相机的俯仰角。可由 RotatePerspective 或 SetPerspectiveAngles 动态调整。")]
            [Range(1f, 80f)] public float pitch = 35f;

            [Tooltip("透视相机垂直视野角。")]
            [Range(1f, 179f)] public float fieldOfView = 50f;

            [Tooltip("相对焦点的竖直观察偏移。不会造成左右偏移。")]
            public float verticalOffset = 1f;

            [Tooltip("相对焦点的纵深观察偏移。")]
            public float depthOffset;

            [Tooltip("3D 环绕视角的水平朝向。")]
            public float yawDegrees;
        }

        [Serializable]
        public sealed class TransitionSettings
        {
            [Tooltip("2D/3D 模式切换时间。设为 0 时立即完成。")]
            [Min(0f)] public float duration = 0.8f;

            [Tooltip("切换缓动曲线。输入和输出均建议保持在 0 到 1。")]
            public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        [Serializable]
        public sealed class CameraViewModeEvent : UnityEvent<CameraViewMode>
        {
        }

        [Header("Manager")]
        [SerializeField]
        [Tooltip("唯一负责写入实际 Camera 的管理器。")]
        private CameraControlManager cameraManager;

        [SerializeField]
        [Tooltip("普通玩法视角使用 Gameplay 优先级；演出应使用更高优先级。")]
        private int controlPriority = CameraControlPriorities.Gameplay;

        [Header("Modes")]
        [SerializeField] private CameraViewMode initialMode = CameraViewMode.Side2D;
        [SerializeField] private Side2DSettings side2D = new Side2DSettings();
        [SerializeField] private Perspective3DSettings perspective3D = new Perspective3DSettings();

        [Header("Transition")]
        [SerializeField] private TransitionSettings transition = new TransitionSettings();

        [Header("Events")]
        [SerializeField] private CameraViewModeEvent onTransitionStarted = new CameraViewModeEvent();
        [SerializeField] private CameraViewModeEvent onModeChanged = new CameraViewModeEvent();

        private CameraControlHandle controlHandle;
        private CameraViewMode currentMode;
        private CameraViewMode targetMode;
        private bool initialized;
        private bool suppressCompletionNotification;

        public event Action<CameraViewMode> TransitionStarted;
        public event Action<CameraViewMode> ModeChanged;

        public string CameraControlName => "2D / 3D Camera Mode";
        public CameraControlManager Manager => cameraManager;
        public Camera ControlledCamera => cameraManager != null ? cameraManager.OutputCamera : null;
        public Transform FollowTarget => cameraManager != null ? cameraManager.FocusTarget : null;
        public CameraViewMode CurrentMode => currentMode;
        public CameraViewMode TargetMode => targetMode;
        public bool HasControl => controlHandle.HasControl;
        public float Perspective3DYaw => perspective3D.yawDegrees;
        public float Perspective3DPitch => perspective3D.pitch;
        public float Side2DYaw => side2D.yawDegrees;
        public bool IsTransitioning => HasControl && cameraManager != null && cameraManager.IsTransitioning;
        public float NormalizedTransitionTime => cameraManager != null
            ? cameraManager.GetTransitionProgress(controlHandle)
            : 0f;

        private void Reset()
        {
            cameraManager = GetComponent<CameraControlManager>();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            EnsureRegistered();
        }

        private void OnDisable()
        {
            UnsubscribeFromManager();
            controlHandle.Release(CameraTransition.Immediate);
            controlHandle = default;
        }

        private void OnValidate()
        {
            if (cameraManager == null)
            {
                cameraManager = GetComponent<CameraControlManager>();
            }

            side2D.distance = Mathf.Max(0.01f, side2D.distance);
            side2D.orthographicSize = Mathf.Max(0.01f, side2D.orthographicSize);
            perspective3D.distance = Mathf.Max(0.01f, perspective3D.distance);
            perspective3D.pitch = Mathf.Clamp(perspective3D.pitch, 1f, 80f);
            perspective3D.fieldOfView = Mathf.Clamp(perspective3D.fieldOfView, 1f, 179f);
            perspective3D.yawDegrees =
                Mathf.Repeat(perspective3D.yawDegrees + 180f, 360f) - 180f;
            side2D.yawDegrees = Mathf.Repeat(side2D.yawDegrees + 180f, 360f) - 180f;
            transition.duration = Mathf.Max(0f, transition.duration);
        }

        public void SetSide2DYaw(float yawDegrees, bool applyImmediate = false)
        {
            side2D.yawDegrees = Mathf.Repeat(yawDegrees + 180f, 360f) - 180f;
            if (applyImmediate && HasControl)
            {
                cameraManager.Retarget(controlHandle, CameraTransition.Immediate);
            }
        }

        public void RotatePerspective(
            float yawDelta,
            float pitchDelta,
            bool applyImmediate = false)
        {
            perspective3D.yawDegrees = Mathf.Repeat(
                perspective3D.yawDegrees + yawDelta + 180f,
                360f) - 180f;
            perspective3D.pitch = Mathf.Clamp(
                perspective3D.pitch + pitchDelta,
                1f,
                80f);
            if (applyImmediate && HasControl)
            {
                cameraManager.Retarget(controlHandle, CameraTransition.Immediate);
            }
        }

        public void SetPerspectiveAngles(
            float yawDegrees,
            float pitchDegrees,
            bool applyImmediate = false)
        {
            perspective3D.yawDegrees = Mathf.Repeat(
                yawDegrees + 180f,
                360f) - 180f;
            perspective3D.pitch = Mathf.Clamp(pitchDegrees, 1f, 80f);
            if (applyImmediate && HasControl)
            {
                cameraManager.Retarget(controlHandle, CameraTransition.Immediate);
            }
        }

        public void Configure(Camera cameraToControl, Transform target, bool snapToInitialMode = true)
        {
            EnsureInitialized();
            cameraManager.ConfigureOutput(cameraToControl);
            cameraManager.SetFocusTarget(target, false);
            EnsureRegistered();
            if (snapToInitialMode)
            {
                SnapToMode(initialMode, false);
            }
        }

        public void SetFollowTarget(Transform target, bool snapFollowPosition = false)
        {
            EnsureInitialized();
            cameraManager.SetFocusTarget(target, snapFollowPosition);
        }

        public void SetFollowTarget(ICameraFollowTarget targetProvider, bool snapFollowPosition = false)
        {
            SetFollowTarget(targetProvider?.CameraFollowTransform, snapFollowPosition);
        }

        public void SetFocusPoint(Vector3 worldPoint, bool snap = false)
        {
            EnsureInitialized();
            cameraManager.SetFocusPoint(worldPoint, snap);
        }

        public void ClearFollowTarget(bool keepCurrentFocus = true)
        {
            EnsureInitialized();
            cameraManager.ClearFocusTarget(keepCurrentFocus);
        }

        public void SwitchMode(CameraViewMode mode, bool immediate = false)
        {
            EnsureInitialized();
            EnsureRegistered();
            if (targetMode == mode)
            {
                if (immediate)
                {
                    cameraManager.Retarget(
                        controlHandle,
                        CameraTransition.Immediate);
                    if (HasControl)
                    {
                        CompleteModeChange();
                    }
                }
                return;
            }

            targetMode = mode;
            CameraTransition cameraTransition = immediate
                ? CameraTransition.Immediate
                : BuildTransition();
            onTransitionStarted.Invoke(mode);
            TransitionStarted?.Invoke(mode);
            cameraManager.Retarget(controlHandle, cameraTransition);
            if (immediate && HasControl)
            {
                CompleteModeChange();
            }
        }

        public void ToggleMode(bool immediate = false)
        {
            CameraViewMode nextMode = targetMode == CameraViewMode.Side2D
                ? CameraViewMode.Perspective3D
                : CameraViewMode.Side2D;
            SwitchMode(nextMode, immediate);
        }

        public void SwitchTo2D(bool immediate = false)
        {
            SwitchMode(CameraViewMode.Side2D, immediate);
        }

        public void SwitchTo3D(bool immediate = false)
        {
            SwitchMode(CameraViewMode.Perspective3D, immediate);
        }

        public void SnapToMode(CameraViewMode mode, bool notifyListeners = true)
        {
            EnsureInitialized();
            EnsureRegistered();
            bool changed = currentMode != mode || targetMode != mode || IsTransitioning;
            targetMode = mode;
            suppressCompletionNotification = !notifyListeners;
            try
            {
                cameraManager.Retarget(controlHandle, CameraTransition.Immediate);
            }
            finally
            {
                suppressCompletionNotification = false;
            }
            if (HasControl && currentMode != mode)
            {
                currentMode = mode;
                if (changed && notifyListeners)
                {
                    onModeChanged.Invoke(mode);
                    ModeChanged?.Invoke(mode);
                }
            }
        }

        public void SnapFollowPosition()
        {
            EnsureInitialized();
            cameraManager.CompleteTransition(controlHandle);
        }

        public bool TryGetCameraState(
            in CameraControlContext context,
            out CameraState state)
        {
            if (targetMode == CameraViewMode.Side2D)
            {
                Quaternion yaw = Quaternion.Euler(0f, side2D.yawDegrees, 0f);
                Vector3 center = context.FocusPoint + yaw * new Vector3(
                    0f,
                    side2D.verticalOffset,
                    side2D.depthOffset);
                state = new CameraState
                {
                    position = center + yaw * Vector3.back * side2D.distance,
                    rotation = Quaternion.LookRotation(
                        yaw * Vector3.forward,
                        Vector3.up),
                    projection = CameraProjectionMode.Orthographic,
                    orthographicSize = side2D.orthographicSize,
                    fieldOfView = perspective3D.fieldOfView,
                };
                return true;
            }

            Vector3 perspectiveCenter = context.FocusPoint + new Vector3(
                0f,
                perspective3D.verticalOffset,
                perspective3D.depthOffset);
            Quaternion rotation = Quaternion.Euler(
                perspective3D.pitch,
                perspective3D.yawDegrees,
                0f);
            state = new CameraState
            {
                position = perspectiveCenter - rotation * Vector3.forward * perspective3D.distance,
                rotation = rotation,
                projection = CameraProjectionMode.Perspective,
                orthographicSize = side2D.orthographicSize,
                fieldOfView = perspective3D.fieldOfView,
            };
            return true;
        }

        internal void Tick(float deltaTime)
        {
            EnsureInitialized();
            cameraManager.Tick(deltaTime);
        }

        internal void SettleTransitionForDisable()
        {
            if (cameraManager != null)
            {
                cameraManager.CompleteTransition(controlHandle);
            }
        }

        private void EnsureInitialized()
        {
            if (cameraManager == null)
            {
                cameraManager = GetComponent<CameraControlManager>();
            }

            if (cameraManager == null)
            {
                cameraManager = gameObject.AddComponent<CameraControlManager>();
            }

            if (!initialized)
            {
                currentMode = initialMode;
                targetMode = initialMode;
                initialized = true;
            }
        }

        private void EnsureRegistered()
        {
            if (controlHandle.IsValid || !isActiveAndEnabled)
            {
                return;
            }

            controlHandle = cameraManager.RequestControl(
                this,
                controlPriority,
                CameraInterruptionPolicy.AllowHigherPriority,
                CameraTransition.Immediate);
            SubscribeToManager();
        }

        private void SubscribeToManager()
        {
            cameraManager.TransitionCompleted -= OnManagerTransitionCompleted;
            cameraManager.TransitionCompleted += OnManagerTransitionCompleted;
        }

        private void UnsubscribeFromManager()
        {
            if (cameraManager != null)
            {
                cameraManager.TransitionCompleted -= OnManagerTransitionCompleted;
            }
        }

        private void OnManagerTransitionCompleted(CameraControlHandle handle)
        {
            if (handle == controlHandle)
            {
                CompleteModeChange(!suppressCompletionNotification);
            }
        }

        private void CompleteModeChange(bool notifyListeners = true)
        {
            if (currentMode == targetMode)
            {
                return;
            }

            currentMode = targetMode;
            if (notifyListeners)
            {
                onModeChanged.Invoke(currentMode);
                ModeChanged?.Invoke(currentMode);
            }
        }

        private CameraTransition BuildTransition()
        {
            return new CameraTransition
            {
                duration = transition.duration,
                easing = transition.easing,
            };
        }
    }
}
