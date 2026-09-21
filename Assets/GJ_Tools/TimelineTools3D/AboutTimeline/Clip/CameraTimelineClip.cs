using UnityEngine;
using UnityEngine.Playables;

public enum CamMoveMode
{
    [InspectorName("平滑运镜")]
    SmoothLerp,
    [InspectorName("瞬移运镜")]
    Teleport,
    [InspectorName("归位")]
    ResetOrigin
}

public enum ResetCamSubMode
{
    [InspectorName("瞬移")]
    Teleport,
    [InspectorName("平滑")]
    SmoothLerp
}

public enum TimelineCameraProjection
{
    [InspectorName("正交 2D")]
    Orthographic,
    [InspectorName("透视 3D")]
    Perspective
}

public class CameraTimelineClip : PlayableAsset
{
    [Header("镜头移动总模式")]
    [Tooltip("运镜模式：平滑跟到目标机位 / 瞬移切镜 / 归位回播放前机位")]
    public CamMoveMode cameraMoveMode;

    [Header("归位模式专用参数")]
    [Tooltip("归位方式：瞬移 或 平滑拉回")]
    public ResetCamSubMode resetSubMode;
    [Tooltip("归位平滑速度(越大越快)")]
    public float resetLerpFactor = 10f;

    [Header("环绕圆弧运镜")]
    [Tooltip("沿圆弧环绕角色(忽略目标坐标的距离部分，只取方位)")]
    public bool useSurroundMode = false;
    [Tooltip("环绕半径")]
    public float surroundRadius = 3.2f;
    [Tooltip("环绕扫过的角度(180 = 绕到侧面/脑后，负值反向)")]
    public float surroundTotalAngle = 180f;
    [Tooltip("环绕圆心的高度(相对角色脚底)")]
    public float surroundFixedHeight = 1.2f;

    [Header("目标机位参数")]
    [Tooltip("目标机位坐标(相对角色，角色转身目标会跟着走)")]
    public Vector3 cameraTargetLocalPos;
    [Tooltip("目标机位朝向(相对角色的欧拉角)")]
    public Vector3 cameraTargetEuler;

    [Header("朝向设置")]
    [Tooltip("片段内持续看向角色(否则使用目标机位朝向)")]
    public bool lockLookAtPlayer = true;

    [Header("2D / 3D 投影")]
    [Tooltip("覆盖 Project 当前投影，可在 Timeline 中主动切换正交 2D 与透视 3D")]
    public bool overrideProjection;
    [Tooltip("覆盖后的投影方式")]
    public TimelineCameraProjection projection = TimelineCameraProjection.Perspective;
    [Tooltip("正交模式的可视高度")]
    [Min(0.01f)] public float orthographicSize = 5f;
    [Tooltip("透视模式的垂直视野角")]
    [Range(1f, 179f)] public float fieldOfView = 50f;
    [Tooltip("投影模式切换时交给 Project CameraControlManager 的过渡时间")]
    [Min(0f)] public float projectionTransitionDuration = 0.4f;

    [Header("2D 正交轴约束")]
    [Tooltip("仅在目标投影为正交时限制相机位置；适合 2D 侧视或固定纵深关卡")]
    public bool constrainOrthographicAxes;
    [Tooltip("允许相机沿世界 X 轴移动")]
    public bool allowPositionX = true;
    [Tooltip("允许相机沿世界 Y 轴移动")]
    public bool allowPositionY = true;
    [Tooltip("允许相机沿世界 Z 轴移动")]
    public bool allowPositionZ;
    [Tooltip("限制世界 X 坐标范围")]
    public bool clampPositionX;
    public Vector2 positionXRange = new Vector2(-10f, 10f);
    [Tooltip("限制世界 Y 坐标范围")]
    public bool clampPositionY;
    public Vector2 positionYRange = new Vector2(-10f, 10f);
    [Tooltip("限制世界 Z 坐标范围")]
    public bool clampPositionZ;
    public Vector2 positionZRange = new Vector2(-10f, 10f);

    [Header("平滑插值系数")]
    [Tooltip("平滑跟手的插值系数(越大越跟手)")]
    public float smoothLerpFactor = 10f;

    [Header("变速插值设置")]
    [Tooltip("启用变速(起/终点速度线性过渡)")]
    public bool useVariableSpeed = false;
    [Tooltip("变速起始速度(0=静止起步)")]
    public float startSpeed = 1f;
    [Tooltip("变速结束速度")]
    public float endSpeed = 3f;

    [Header("以上一帧相机位置为本段起点")]
    [Tooltip("多段运镜衔接：勾选后以片段开始时的相机位置为插值起点，不做归位；连续运镜建议开启")]
    public bool useLastFrameAsOrigin;

    [Header("片段结束与手动接管")]
    [Tooltip("轨道结束后平滑交还 Project 玩法相机；关闭时立即交还")]
    public bool restoreOriginOnEnd = true;
    [Tooltip("本段播放期间允许手动拖动视角(拖动时本段暂停写入，停止输入后从当前机位继续；建议配合“以上一帧为起点”使用)")]
    public bool allowManualCamera = false;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        CameraTimelineBehaviour behaviour = new CameraTimelineBehaviour();
        behaviour.clip = this;
        return ScriptPlayable<CameraTimelineBehaviour>.Create(graph, behaviour);
    }
}
