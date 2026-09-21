using Project.CameraModes;
using UnityEngine;

[AddComponentMenu("TimelineKit/TimelineCamRig")]
[DisallowMultipleComponent]
[RequireComponent(typeof(CameraControlManager))]
public class TimelineCamRig : MonoBehaviour, ICameraControlSource
{
    [Header("Project Camera")]
    [SerializeField]
    [Tooltip("场景中的统一相机管理器。Timeline 只能通过它修改 Camera。")]
    private CameraControlManager cameraManager;

    [SerializeField]
    [Tooltip("Timeline 运镜使用的控制优先级，默认与项目演出优先级一致。")]
    private int controlPriority = CameraControlPriorities.Cutscene;

    [SerializeField, Min(0f)]
    [Tooltip("轨道结束后交还 Project 玩法相机的平滑时间。")]
    private float returnTransitionDuration = 0.25f;

    [Header("跟随目标")]
    [Tooltip("第三人称跟随的目标，也是运镜轨的参照锚点")]
    public Transform target;

    [Header("环绕机位")]
    [Tooltip("俯仰角(度)，正数向下看")]
    public float rotX = 30f;

    [Tooltip("水平环绕角(度)")]
    public float rotY;

    [Tooltip("相机离目标水平距离")]
    public float distance = 5f;

    [Tooltip("相机高度(相对目标脚底)")]
    public float height = 1.5f;

    [Tooltip("看向目标时在目标上的抬高量")]
    public float lookHeight = 1.2f;

    [Header("状态")]
    [Tooltip("Timeline 相机轨道接管中(自动置位)")]
    public bool isPlayingAnim;

    [HideInInspector]
    public bool userManualAllowed;

    [HideInInspector]
    public bool userDriving;

    private CameraControlHandle _controlHandle;
    private CameraState _normalState;
    private CameraState _shotState;
    private bool _hasNormalState;
    private bool _hasShotState;
    private int _activeTracks;
    private float _planarYaw;

    public string CameraControlName => "Timeline Camera";

    public CameraControlManager Manager
    {
        get
        {
            ResolveManager();
            return cameraManager;
        }
    }

    public Transform AnchorTransform
    {
        get
        {
            if (target != null)
            {
                return target;
            }

            CameraControlManager manager = Manager;
            return manager != null && manager.OutputCamera != null
                ? manager.OutputCamera.transform
                : transform;
        }
    }

    public bool IsTimelineControlling => _controlHandle.IsValid && _controlHandle.HasControl;

    public float ReturnTransitionDuration => returnTransitionDuration;

    public bool SmoothReturnOnRelease { get; private set; } = true;

    public float PlanarYaw => _planarYaw;

    public bool CanManualDrive =>
        isPlayingAnim &&
        userManualAllowed &&
        IsTimelineControlling;

    public bool HasNormalState => _hasNormalState;

    public Vector3 NormalPosition => _normalState.position;

    public Quaternion NormalRotation => _normalState.rotation;

    public Vector3 CurrentPosition => ReadCurrentState().position;

    public Quaternion CurrentRotation => ReadCurrentState().rotation;

    public bool IsCurrentProjectionOrthographic =>
        ReadCurrentState().projection == CameraProjectionMode.Orthographic;

    private void Reset()
    {
        ResolveManager();
    }

    private void Awake()
    {
        ResolveManager();
    }

    private void OnDisable()
    {
        Release(true);
    }

    public Vector3 OrbitPosition(Transform anchor)
    {
        return anchor.position +
               (Quaternion.Euler(rotX, rotY, 0f) * Vector3.back) * distance +
               Vector3.up * height;
    }

    public Vector3 GetNormalOrbitPosition(Transform anchor)
    {
        if (!_hasNormalState)
        {
            return OrbitPosition(anchor);
        }

        return _normalState.position;
    }

    public void CaptureNormalState()
    {
        if (_hasNormalState)
        {
            return;
        }

        _normalState = ReadCurrentState();
        _hasNormalState = true;
    }

    public void SetReturnMode(bool smoothReturn)
    {
        SmoothReturnOnRelease = smoothReturn;
    }

    public void Acquire(float transitionDuration = 0.25f)
    {
        CameraControlManager manager = Manager;
        if (manager == null)
        {
            Debug.LogWarning("[TimelineCamRig] 缺少 CameraControlManager，Timeline 运镜不生效", this);
            return;
        }

        if (_activeTracks > 0)
        {
            Debug.LogWarning(
                "[TimelineCamRig] 多条 CameraTimelineTrack 正在同时控制同一相机，请确保同一时间只有一条相机轨道生效。",
                this);
        }

        CaptureNormalState();
        if (_activeTracks == 0)
        {
            CameraTransition transition = transitionDuration > 0f
                ? CameraTransition.Ease(transitionDuration)
                : CameraTransition.Immediate;
            _controlHandle = manager.RequestControl(
                this,
                controlPriority,
                CameraInterruptionPolicy.AllowHigherPriority,
                transition);
        }

        _activeTracks++;
        isPlayingAnim = true;
    }

    public void Release(bool restoreProjectCamera)
    {
        if (_activeTracks > 0)
        {
            _activeTracks--;
        }

        if (_activeTracks > 0)
        {
            return;
        }

        isPlayingAnim = false;
        userManualAllowed = false;
        userDriving = false;
        _hasShotState = false;

        if (_controlHandle.IsValid)
        {
            CameraTransition transition =
                restoreProjectCamera && returnTransitionDuration > 0f
                    ? CameraTransition.Ease(returnTransitionDuration)
                    : CameraTransition.Immediate;
            _controlHandle.Release(transition);
        }

        _controlHandle = default;
        _hasNormalState = false;
    }

    public void SetPlanarYaw(float yaw)
    {
        _planarYaw = Mathf.Repeat(yaw + 180f, 360f) - 180f;
    }

    public void SetShotTransform(
        Vector3 position,
        Quaternion rotation,
        bool overrideProjection,
        TimelineCameraProjection projection,
        float orthographicSize,
        float fieldOfView,
        float transitionDuration)
    {
        if (!_hasShotState)
        {
            _shotState = ReadCurrentState();
        }

        CameraProjectionMode nextProjection = overrideProjection
            ? MapProjection(projection)
            : _shotState.projection;
        float nextOrthographicSize = overrideProjection
            ? Mathf.Max(0.01f, orthographicSize)
            : _shotState.orthographicSize;
        float nextFieldOfView = overrideProjection
            ? Mathf.Clamp(fieldOfView, 1f, 179f)
            : _shotState.fieldOfView;
        bool projectionChanged = nextProjection != _shotState.projection;
        bool lensChanged =
            !Mathf.Approximately(nextOrthographicSize, _shotState.orthographicSize) ||
            !Mathf.Approximately(nextFieldOfView, _shotState.fieldOfView);

        _shotState.position = position;
        _shotState.rotation = rotation;
        _shotState.projection = nextProjection;
        if (overrideProjection)
        {
            _shotState.orthographicSize = nextOrthographicSize;
            _shotState.fieldOfView = nextFieldOfView;
        }
        _hasShotState = true;

        if ((projectionChanged || lensChanged) && _controlHandle.IsValid)
        {
            CameraTransition transition = transitionDuration > 0f
                ? CameraTransition.Ease(transitionDuration)
                : CameraTransition.Immediate;
            Manager?.Retarget(_controlHandle, transition);
        }
    }

    public bool BeginManualControl()
    {
        if (!CanManualDrive)
        {
            return false;
        }

        if (!userDriving)
        {
            SyncParamsFromCamera();
            userDriving = true;
        }

        return true;
    }

    public void EndManualControl()
    {
        userDriving = false;
    }

    public void SyncParamsFromCamera()
    {
        SyncParamsFromState(ReadCurrentState());
    }

    public bool TryGetCameraState(
        in CameraControlContext context,
        out CameraState state)
    {
        if (userDriving && CanManualDrive)
        {
            state = BuildManualState(context.FocusPoint);
            return true;
        }

        if (_hasShotState)
        {
            state = _shotState;
            return true;
        }

        state = default;
        return false;
    }

    private void LateUpdate()
    {
        if (!isPlayingAnim || userDriving || _hasShotState)
        {
            return;
        }

        CameraControlManager manager = Manager;
        if (manager == null || target == null)
        {
            return;
        }

        CameraState orbitState = BuildManualState(target.position + Vector3.up * lookHeight);
        _shotState = orbitState;
        _hasShotState = true;
    }

    private CameraState BuildManualState(Vector3 lookPoint)
    {
        Vector3 position;
        if (target != null)
        {
            position = OrbitPosition(target);
        }
        else
        {
            position = lookPoint +
                       (Quaternion.Euler(rotX, rotY, 0f) * Vector3.back) * distance +
                       Vector3.up * height;
        }

        CameraState state = _hasShotState ? _shotState : ReadCurrentState();
        state.position = position;
        state.rotation = LookRotation(position, lookPoint, state.rotation);
        return state;
    }

    private void SyncParamsFromState(CameraState state)
    {
        if (target == null)
        {
            return;
        }

        Vector3 relative = state.position - target.position;
        float flatRadius = Mathf.Sqrt(relative.x * relative.x + relative.z * relative.z);
        float heightDelta = relative.y - height;
        float totalDistance = Mathf.Sqrt(heightDelta * heightDelta + flatRadius * flatRadius);
        if (totalDistance < 0.001f)
        {
            return;
        }

        distance = totalDistance;
        rotX = Mathf.Asin(Mathf.Clamp(heightDelta / totalDistance, -1f, 1f)) * Mathf.Rad2Deg;
        rotY = Mathf.Atan2(-relative.x, -relative.z) * Mathf.Rad2Deg;
    }

    private CameraState ReadCurrentState()
    {
        CameraControlManager manager = Manager;
        if (manager != null)
        {
            return manager.CurrentState;
        }

        Camera camera = GetComponent<Camera>();
        if (camera != null)
        {
            return CameraState.FromCamera(camera);
        }

        return new CameraState
        {
            position = transform.position,
            rotation = transform.rotation,
            projection = CameraProjectionMode.Perspective,
            orthographicSize = 5f,
            fieldOfView = 60f
        };
    }

    private void ResolveManager()
    {
        if (cameraManager == null)
        {
            cameraManager = GetComponent<CameraControlManager>();
        }
    }

    private static CameraProjectionMode MapProjection(TimelineCameraProjection projection)
    {
        return projection == TimelineCameraProjection.Orthographic
            ? CameraProjectionMode.Orthographic
            : CameraProjectionMode.Perspective;
    }

    private static Quaternion LookRotation(
        Vector3 position,
        Vector3 lookPoint,
        Quaternion fallback)
    {
        Vector3 direction = lookPoint - position;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : fallback;
    }
}
