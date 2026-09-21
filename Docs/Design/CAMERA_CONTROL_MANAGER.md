# 统一摄像机控制架构与协作接口

## 1. 必须遵守的规则

场景里的实际 `Camera` 只能由 `CameraControlManager` 写入。其他模块不得直接修改：

```text
Camera.transform.position / rotation
Camera.orthographic / orthographicSize
Camera.fieldOfView
Camera.projectionMatrix
```

跟随、2D/3D模式、技能镜头、Boss镜头和演出都只向 Manager 提供期望状态或焦点。
这样可以避免多个 `LateUpdate`、Animator、Timeline 和协程在同一帧争抢相机。

## 2. 数据流

```text
角色跟随模块 ── SetFocusPoint ──┐
                                │
2D/3D 模式 ─ ICameraControlSource ├─> CameraControlManager ─> Unity Camera
                                │       唯一写入者
演出/技能 ── ICameraControlSource ┘       优先级 + 打断 + 单过渡状态机
```

- 跟随模块负责算“看哪里”，不负责摆放 Camera；
- 2D/3D模块根据焦点算构图、旋转和镜头；
- 演出模块可提交完整绝对 `CameraState`，临时覆盖玩法视角；
- Manager 每帧只选一个完整状态源并执行一次 Camera 写入。

## 3. 控制权模型

调用 `RequestControl` 会得到 `CameraControlHandle`。请求可排队，不代表一定立即持有
控制权；使用 `handle.HasControl` 查询实际控制者。

内置优先级：

| 常量 | 值 | 建议用途 |
| --- | ---: | --- |
| `CameraControlPriorities.Gameplay` | 0 | 普通2D/3D玩法视角 |
| `GameplayAbility` | 100 | 短暂技能镜头 |
| `Cutscene` | 1000 | Timeline、过场、Boss演出 |
| `Debug` | 10000 | 仅调试工具 |

规则：

1. 更高优先级可抢占 `AllowHigherPriority` 控制者；
2. 同优先级不会自动抢占，避免加载顺序导致画面随机变化；
3. `BlockAll` 会阻止普通抢占，适合必须完整播放的关键段；
4. 不可打断段结束后必须恢复 `AllowHigherPriority` 或释放 Handle；
5. `ForceTakeControl` 绕过规则，只供死亡、重置等紧急兜底；
6. `Release` 后自动选择队列中优先级最高的剩余来源；
7. Handle 释放后失效，旧 Handle 不能误释放新请求。

`BlockAll` 不应覆盖整段长演出，否则更高优先级的死亡或场景切换镜头无法正常接管。

## 4. 为什么由 Manager 统一过渡

是的，控制权、打断动画和协程生命周期都应由 Manager 统一管理。本实现完全不使用
相机协程，而是在 Manager 内维护唯一过渡状态：

- 新请求捕获当前实际画面作为起点；
- 中途反向、抢占或重定向不会出现多个协程并行；
- 正交与透视过渡从当前实际投影矩阵继续；
- 禁用 Manager 时会清理自定义投影状态；
- 暂停时是否推进由 Manager 的 `Use Unscaled Time` 统一决定。

外部模块不得为 Camera 写入启动自己的过渡协程。外部可以用协程安排剧情时序，但每
帧只能更新自己的期望 `CameraState`，最终写入仍交给 Manager。

## 5. 角色跟随模块接入

跟随程序负责阻尼、死区、房间边界和传送处理，最后只提交焦点：

```csharp
using Project.CameraModes;
using UnityEngine;

public sealed class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private CameraControlManager cameraManager;
    [SerializeField] private Transform player;
    [SerializeField] private float damping = 0.12f;

    private Vector3 focus;
    private Vector3 velocity;

    private void LateUpdate()
    {
        focus = Vector3.SmoothDamp(
            focus,
            player.position,
            ref velocity,
            damping);
        cameraManager.SetFocusPoint(focus);
    }
}
```

Manager 的执行顺序是 `1000`。跟随模块应使用小于 `1000` 的默认执行顺序，确保焦点
在 Manager 最终写入前更新。

简单测试场景也可调用 `SetFocusTarget(transform)`；它只做直接采样，不包含正式跟随
算法。

## 6. 演出/技能模块接入

演出适配器实现 `ICameraControlSource`，不要持有并写入实际 Camera：

```csharp
using Project.CameraModes;
using UnityEngine;

public sealed class CutsceneCameraSource : MonoBehaviour, ICameraControlSource
{
    [SerializeField] private CameraControlManager cameraManager;
    [SerializeField] private Transform shotCameraAnchor;

    private CameraControlHandle handle;

    public string CameraControlName => "Boss Intro";

    public bool TryGetCameraState(
        in CameraControlContext context,
        out CameraState state)
    {
        state = new CameraState
        {
            position = shotCameraAnchor.position,
            rotation = shotCameraAnchor.rotation,
            projection = CameraProjectionMode.Perspective,
            fieldOfView = 45f,
            orthographicSize = 5f,
        };
        return true;
    }

    public void BeginShot()
    {
        handle = cameraManager.RequestControl(
            this,
            CameraControlPriorities.Cutscene,
            CameraInterruptionPolicy.AllowHigherPriority,
            CameraTransition.Ease(0.35f));
    }

    public void RetargetShot()
    {
        cameraManager.Retarget(handle, CameraTransition.Ease(0.2f));
    }

    public void EndShot()
    {
        handle.Release(CameraTransition.Ease(0.3f));
    }

    private void OnDisable()
    {
        handle.Release(CameraTransition.Immediate);
    }
}
```

未来接入 Timeline 时，只需由 Timeline Clip 更新这个 Source 的锚点或状态，并在片段
开始/结束时申请、释放 Handle；不需要让 Timeline 直接绑定 Main Camera。目前项目未
添加 Timeline 相机控制脚本。

## 7. 主要 API

| API | 说明 |
| --- | --- |
| `RequestControl(source, priority, policy, transition)` | 注册状态源并参与控制权仲裁 |
| `Retarget(handle, transition)` | 同一来源目标变化，从当前画面重新过渡 |
| `ReleaseControl` / `handle.Release` | 释放并回退到下一个来源 |
| `SetPriority` | 动态调整请求优先级 |
| `SetInterruptionPolicy` | 开始/结束不可打断段 |
| `ForceTakeControl` | 紧急强制接管 |
| `SetFocusPoint` | 提交正式跟随系统计算后的焦点 |
| `SetFocusTarget` | 简单场景直接采样 Transform |
| `ActiveControlChanged` | 当前控制者发生变化 |
| `TransitionCompleted` | 某 Handle 的统一过渡完成 |
| `ActiveControlName` / `ActivePriority` | 调试当前控制权 |

## 8. 模块交付约定

负责跟随或演出的程序提交代码前应确认：

1. 搜索模块代码，不存在对实际 Camera Transform/镜头字段的写入；
2. 每次 `RequestControl` 都有对应 `Release`，并在 `OnDisable` 兜底；
3. 不可打断状态不会无限期保留；
4. 进入和退出演出均测试过渡中再次打断；
5. 模式切换发生在演出期间时，释放后能回到最新模式；
6. 不修改 `ProjectVersion.txt`，保持项目 Unity 版本不变。
