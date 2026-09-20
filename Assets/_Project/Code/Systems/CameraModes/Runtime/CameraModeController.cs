using System;
using UnityEngine;
using UnityEngine.Events;

namespace Project.CameraModes
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class CameraModeController : MonoBehaviour
    {
        [Serializable]
        public sealed class Side2DSettings
        {
            [Tooltip("相机沿世界 Z 轴后退的距离。")]
            [Min(0.01f)]
            public float distance = 10f;

            [Tooltip("正交相机的可视高度参数。")]
            [Min(0.01f)]
            public float orthographicSize = 5f;

            [Tooltip("相对跟随目标的竖直观察偏移。不会造成左右偏移。")]
            public float verticalOffset = 1f;

            [Tooltip("相对跟随目标的纵深观察偏移。")]
            public float depthOffset;
        }

        [Serializable]
        public sealed class Perspective3DSettings
        {
            [Tooltip("相机到观察中心的直线距离。")]
            [Min(0.01f)]
            public float distance = 12f;

            [Tooltip("斜上方俯视角度。Y 轴旋转固定为 0，保证不往左右偏。")]
            [Range(1f, 80f)]
            public float pitch = 35f;

            [Tooltip("透视相机垂直视野角。")]
            [Range(1f, 179f)]
            public float fieldOfView = 50f;

            [Tooltip("相对跟随目标的竖直观察偏移。不会造成左右偏移。")]
            public float verticalOffset = 1f;

            [Tooltip("相对跟随目标的纵深观察偏移。")]
            public float depthOffset;
        }

        [Serializable]
        public sealed class TransitionSettings
        {
            [Tooltip("2D/3D 模式切换时间。设为 0 时立即完成。")]
            [Min(0f)]
            public float duration = 0.8f;

            [Tooltip("切换缓动曲线。输入和输出均建议保持在 0 到 1。")]
            public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            [Tooltip("暂停游戏时间时是否仍然完成相机切换。")]
            public bool useUnscaledTime = true;
        }

        [Serializable]
        public sealed class CameraViewModeEvent : UnityEvent<CameraViewMode>
        {
        }

        private readonly struct CameraPose
        {
            public CameraPose(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
        }

        [Header("References")]
        [SerializeField]
        [Tooltip("被控制的相机。留空时优先使用同物体上的 Camera。")]
        private Camera controlledCamera;

        [SerializeField]
        [Tooltip("玩家尚未接入时可使用场景中的空物体作为跟随点。")]
        private Transform followTarget;

        [SerializeField]
        [Tooltip("没有跟随目标时使用的世界坐标观察点。")]
        private Vector3 fallbackFocusPoint = Vector3.zero;

        [Header("Modes")]
        [SerializeField]
        private CameraViewMode initialMode = CameraViewMode.Side2D;

        [SerializeField]
        private Side2DSettings side2D = new Side2DSettings();

        [SerializeField]
        private Perspective3DSettings perspective3D = new Perspective3DSettings();

        [Header("Motion")]
        [SerializeField]
        private TransitionSettings transition = new TransitionSettings();

        [SerializeField]
        [Tooltip("跟随目标移动的平滑时间。设为 0 时直接跟随。")]
        [Min(0f)]
        private float followSmoothTime = 0.08f;

        [Header("Events")]
        [SerializeField]
        private CameraViewModeEvent onTransitionStarted = new CameraViewModeEvent();

        [SerializeField]
        private CameraViewModeEvent onModeChanged = new CameraViewModeEvent();

        private CameraViewMode currentMode;
        private CameraViewMode targetMode;
        private bool initialized;
        private bool isTransitioning;
        private float transitionElapsed;
        private Vector3 transitionStartPosition;
        private Quaternion transitionStartRotation;
        private Matrix4x4 transitionStartProjection;
        private float transitionStartOrthographicSize;
        private float transitionStartFieldOfView;
        private Vector3 smoothedFocus;
        private Vector3 focusVelocity;

        public event Action<CameraViewMode> TransitionStarted;
        public event Action<CameraViewMode> ModeChanged;

        public Camera ControlledCamera => controlledCamera;
        public Transform FollowTarget => followTarget;
        public CameraViewMode CurrentMode => currentMode;
        public CameraViewMode TargetMode => targetMode;
        public bool IsTransitioning => isTransitioning;
        public float NormalizedTransitionTime => !isTransitioning || transition.duration <= 0f
            ? (isTransitioning ? 0f : 1f)
            : Mathf.Clamp01(transitionElapsed / transition.duration);

        private void Reset()
        {
            controlledCamera = GetComponent<Camera>();
        }

        private void Awake()
        {
            Initialize(initialMode, true);
        }

        private void LateUpdate()
        {
            float deltaTime = transition.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Tick(deltaTime);
        }

        private void OnDisable()
        {
            SettleTransitionForDisable();
        }

        internal void SettleTransitionForDisable()
        {
            if (!initialized || controlledCamera == null || !isTransitioning)
            {
                return;
            }

            CameraViewMode settleMode = NormalizedTransitionTime >= 0.5f ? targetMode : currentMode;
            ApplyStableLens(settleMode);
            currentMode = settleMode;
            targetMode = settleMode;
            isTransitioning = false;
        }

        private void OnValidate()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }

            side2D.distance = Mathf.Max(0.01f, side2D.distance);
            side2D.orthographicSize = Mathf.Max(0.01f, side2D.orthographicSize);
            perspective3D.distance = Mathf.Max(0.01f, perspective3D.distance);
            perspective3D.pitch = Mathf.Clamp(perspective3D.pitch, 1f, 80f);
            perspective3D.fieldOfView = Mathf.Clamp(perspective3D.fieldOfView, 1f, 179f);
            transition.duration = Mathf.Max(0f, transition.duration);
            followSmoothTime = Mathf.Max(0f, followSmoothTime);
        }

        public void Configure(Camera cameraToControl, Transform target, bool snapToInitialMode = true)
        {
            controlledCamera = cameraToControl;
            followTarget = target;
            initialized = false;
            Initialize(initialMode, snapToInitialMode);
        }

        public void SetFollowTarget(Transform target, bool snapFollowPosition = false)
        {
            followTarget = target;
            if (snapFollowPosition)
            {
                SnapFollowPosition();
            }
        }

        public void SetFollowTarget(ICameraFollowTarget targetProvider, bool snapFollowPosition = false)
        {
            SetFollowTarget(targetProvider?.CameraFollowTransform, snapFollowPosition);
        }

        public void ClearFollowTarget(bool keepCurrentFocus = true)
        {
            if (keepCurrentFocus)
            {
                fallbackFocusPoint = smoothedFocus;
            }

            followTarget = null;
            focusVelocity = Vector3.zero;
        }

        public void SwitchMode(CameraViewMode mode, bool immediate = false)
        {
            EnsureInitialized();
            if (controlledCamera == null)
            {
                return;
            }

            if (immediate || transition.duration <= 0f)
            {
                SnapToMode(mode);
                return;
            }

            if (isTransitioning && targetMode == mode)
            {
                return;
            }

            if (!isTransitioning && currentMode == mode)
            {
                return;
            }

            targetMode = mode;
            transitionElapsed = 0f;
            transitionStartPosition = controlledCamera.transform.position;
            transitionStartRotation = controlledCamera.transform.rotation;
            transitionStartProjection = controlledCamera.projectionMatrix;
            transitionStartOrthographicSize = controlledCamera.orthographicSize;
            transitionStartFieldOfView = controlledCamera.fieldOfView;
            isTransitioning = true;

            // A custom projection matrix provides a continuous ortho/perspective
            // visual blend. The stable endpoint restores the native camera lens.
            controlledCamera.orthographic = false;
            controlledCamera.projectionMatrix = transitionStartProjection;

            onTransitionStarted.Invoke(mode);
            TransitionStarted?.Invoke(mode);
        }

        public void ToggleMode(bool immediate = false)
        {
            CameraViewMode referenceMode = isTransitioning ? targetMode : currentMode;
            CameraViewMode nextMode = referenceMode == CameraViewMode.Side2D
                ? CameraViewMode.Perspective3D
                : CameraViewMode.Side2D;
            SwitchMode(nextMode, immediate);
        }

        public void SnapToMode(CameraViewMode mode, bool notifyListeners = true)
        {
            EnsureInitialized();
            if (controlledCamera == null)
            {
                return;
            }

            smoothedFocus = ResolveRawFocus();
            focusVelocity = Vector3.zero;
            CameraPose pose = CalculatePose(mode, smoothedFocus);
            controlledCamera.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            ApplyStableLens(mode);

            bool changed = currentMode != mode || targetMode != mode || isTransitioning;
            currentMode = mode;
            targetMode = mode;
            isTransitioning = false;
            transitionElapsed = 0f;

            if (changed && notifyListeners)
            {
                onModeChanged.Invoke(mode);
                ModeChanged?.Invoke(mode);
            }
        }

        public void SnapFollowPosition()
        {
            EnsureInitialized();
            smoothedFocus = ResolveRawFocus();
            focusVelocity = Vector3.zero;
            if (!isTransitioning && controlledCamera != null)
            {
                CameraPose pose = CalculatePose(currentMode, smoothedFocus);
                controlledCamera.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            }
        }

        internal void Tick(float deltaTime)
        {
            EnsureInitialized();
            if (controlledCamera == null)
            {
                return;
            }

            UpdateSmoothedFocus(Mathf.Max(0f, deltaTime));
            if (!isTransitioning)
            {
                CameraPose stablePose = CalculatePose(currentMode, smoothedFocus);
                controlledCamera.transform.SetPositionAndRotation(
                    stablePose.Position,
                    stablePose.Rotation);
                return;
            }

            transitionElapsed += Mathf.Max(0f, deltaTime);
            float normalizedTime = transition.duration <= 0f
                ? 1f
                : Mathf.Clamp01(transitionElapsed / transition.duration);
            float easedTime = transition.easing == null
                ? normalizedTime
                : Mathf.Clamp01(transition.easing.Evaluate(normalizedTime));

            CameraPose targetPose = CalculatePose(targetMode, smoothedFocus);
            controlledCamera.transform.SetPositionAndRotation(
                Vector3.LerpUnclamped(transitionStartPosition, targetPose.Position, easedTime),
                Quaternion.SlerpUnclamped(transitionStartRotation, targetPose.Rotation, easedTime));

            Matrix4x4 targetProjection = BuildProjection(targetMode);
            controlledCamera.projectionMatrix = LerpMatrix(
                transitionStartProjection,
                targetProjection,
                easedTime);
            controlledCamera.orthographicSize = Mathf.Lerp(
                transitionStartOrthographicSize,
                side2D.orthographicSize,
                easedTime);
            controlledCamera.fieldOfView = Mathf.Lerp(
                transitionStartFieldOfView,
                perspective3D.fieldOfView,
                easedTime);

            if (normalizedTime >= 1f)
            {
                CompleteTransition();
            }
        }

        private void Initialize(CameraViewMode mode, bool applyPose)
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }

            currentMode = mode;
            targetMode = mode;
            smoothedFocus = ResolveRawFocus();
            focusVelocity = Vector3.zero;
            initialized = true;

            if (controlledCamera == null)
            {
                return;
            }

            ApplyStableLens(mode);
            if (applyPose)
            {
                CameraPose pose = CalculatePose(mode, smoothedFocus);
                controlledCamera.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            }
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                Initialize(initialMode, false);
            }
        }

        private void UpdateSmoothedFocus(float deltaTime)
        {
            Vector3 desiredFocus = ResolveRawFocus();
            if (followSmoothTime <= 0f || deltaTime <= 0f)
            {
                if (followSmoothTime <= 0f)
                {
                    smoothedFocus = desiredFocus;
                    focusVelocity = Vector3.zero;
                }

                return;
            }

            smoothedFocus = Vector3.SmoothDamp(
                smoothedFocus,
                desiredFocus,
                ref focusVelocity,
                followSmoothTime,
                Mathf.Infinity,
                deltaTime);
        }

        private Vector3 ResolveRawFocus()
        {
            return followTarget != null ? followTarget.position : fallbackFocusPoint;
        }

        private CameraPose CalculatePose(CameraViewMode mode, Vector3 focus)
        {
            if (mode == CameraViewMode.Side2D)
            {
                Vector3 center = focus + new Vector3(0f, side2D.verticalOffset, side2D.depthOffset);
                return new CameraPose(
                    center + Vector3.back * side2D.distance,
                    Quaternion.identity);
            }

            Vector3 perspectiveCenter = focus + new Vector3(
                0f,
                perspective3D.verticalOffset,
                perspective3D.depthOffset);
            Quaternion rotation = Quaternion.Euler(perspective3D.pitch, 0f, 0f);
            Vector3 position = perspectiveCenter - rotation * Vector3.forward * perspective3D.distance;
            return new CameraPose(position, rotation);
        }

        private Matrix4x4 BuildProjection(CameraViewMode mode)
        {
            float aspect = controlledCamera.aspect > 0f ? controlledCamera.aspect : 16f / 9f;
            if (mode == CameraViewMode.Side2D)
            {
                float halfHeight = side2D.orthographicSize;
                float halfWidth = halfHeight * aspect;
                return Matrix4x4.Ortho(
                    -halfWidth,
                    halfWidth,
                    -halfHeight,
                    halfHeight,
                    controlledCamera.nearClipPlane,
                    controlledCamera.farClipPlane);
            }

            return Matrix4x4.Perspective(
                perspective3D.fieldOfView,
                aspect,
                controlledCamera.nearClipPlane,
                controlledCamera.farClipPlane);
        }

        private static Matrix4x4 LerpMatrix(Matrix4x4 from, Matrix4x4 to, float amount)
        {
            Matrix4x4 result = default;
            for (int index = 0; index < 16; index++)
            {
                result[index] = Mathf.LerpUnclamped(from[index], to[index], amount);
            }

            return result;
        }

        private void ApplyStableLens(CameraViewMode mode)
        {
            controlledCamera.ResetProjectionMatrix();
            controlledCamera.orthographic = mode == CameraViewMode.Side2D;
            if (mode == CameraViewMode.Side2D)
            {
                controlledCamera.orthographicSize = side2D.orthographicSize;
            }
            else
            {
                controlledCamera.fieldOfView = perspective3D.fieldOfView;
            }

            controlledCamera.ResetProjectionMatrix();
        }

        private void CompleteTransition()
        {
            currentMode = targetMode;
            isTransitioning = false;
            transitionElapsed = transition.duration;
            CameraPose pose = CalculatePose(currentMode, smoothedFocus);
            controlledCamera.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            ApplyStableLens(currentMode);
            onModeChanged.Invoke(currentMode);
            ModeChanged?.Invoke(currentMode);
        }
    }
}
