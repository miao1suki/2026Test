# 2D / 3D 相机模式模块

## 1. 职责边界

`CameraModeController` 只负责生成两种玩法视角：

- `Side2D`：平视、正交投影；
- `Perspective3D`：斜上方俯视、透视投影，Y 轴旋转固定为 0；
- 两种模式之间的切换请求、缓动参数和完成事件。

它不再直接修改 `Camera`，也不负责角色跟随策略或 Timeline 演出。
所有实际位置、旋转、正交/透视和投影矩阵写入均由同物体上的
`CameraControlManager` 完成。完整协作规则见
[统一摄像机控制架构](CAMERA_CONTROL_MANAGER.md)。

运行时代码：

```text
Assets/_Project/Code/Systems/CameraModes/Runtime
```

策划工具：

```text
Assets/_Project/Code/Systems/CameraModes/Editor
```

## 2. 策划使用

打开测试场景：

```text
Assets/_Project/Scenes/Sandbox/Programmer/LevelEditorSandbox.unity
```

首选入口是 Scene View 右上角：

```text
Overlays > 2D / 3D 相机
```

备用入口：

```text
Tools > 2026Test > 2D-3D 相机配置
```

操作流程：

1. 选择场景相机；
2. 可选一个临时焦点或玩家锚点；
3. 点击“创建 / 应用相机控制器”；
4. 点击“2D 平台”或“3D 俯视”检查构图；
5. “切换预览时启用过渡”开启时，可在编辑态验证反向打断。

工具会同时配置：

- `CameraControlManager`：唯一 Camera 写入者；
- `CameraModeController`：2D/3D 状态提供者。

## 3. 玩法调用 API

切换模式时只调用 `CameraModeController`：

```csharp
using Project.CameraModes;
using UnityEngine;

public sealed class DimensionSwitch : MonoBehaviour
{
    [SerializeField] private CameraModeController cameraModes;

    public void Enter3D()
    {
        cameraModes.SwitchMode(CameraViewMode.Perspective3D);
    }

    public void ReturnTo2D()
    {
        cameraModes.SwitchMode(CameraViewMode.Side2D);
    }
}
```

| API | 用途 |
| --- | --- |
| `SwitchMode(mode, immediate)` | 平滑或立即切换指定模式 |
| `ToggleMode(immediate)` | 在 2D 与 3D 间切换 |
| `SnapToMode(mode)` | 初始化或编辑器预览时立即到达端点 |
| `CurrentMode` | 已完成的稳定模式 |
| `TargetMode` | 当前请求的目标模式 |
| `HasControl` | 模式模块当前是否持有 Manager 控制权 |
| `IsTransitioning` | 模式模块持有控制权且正在过渡 |
| `TransitionStarted` | 模式切换请求开始 |
| `ModeChanged` | Manager 实际到达稳定端点 |

角色跟随程序不要调用 `Camera.transform`，应把算好的最终焦点交给：

```csharp
cameraModes.Manager.SetFocusPoint(finalFocusPoint);
```

`SetFollowTarget` / `ICameraFollowTarget` 仍保留给简单原型和旧代码兼容；正式的阻尼、
边界、死亡锁定等跟随策略应由独立跟随模块计算，再调用 Manager 的
`SetFocusPoint`。

## 4. 切换和打断

2D/3D 模块没有协程。每次切换都调用 Manager 的 `Retarget`：

1. Manager 捕获当前实际位置、旋转和投影矩阵；
2. 新动画从当前画面继续，而不是从上一次起点重开；
3. 重复请求同一目标不会重启动画；
4. 反向请求立即替换唯一过渡状态；
5. 动画完成后恢复 Unity 原生正交或透视投影矩阵。

若高优先级演出暂时取得控制权，模式切换请求仍可更新目标，但不会写 Camera；演出
释放后 Manager 会回到当前目标模式。

## 5. 参数

| 参数 | 作用 |
| --- | --- |
| 2D Distance | 相机沿世界 Z 轴后退距离 |
| Orthographic Size | 2D 正交视野大小 |
| 3D Distance | 透视相机到观察中心的距离 |
| Pitch | 俯视角；不会引入左右旋转 |
| Field Of View | 3D 透视视野角 |
| Vertical / Depth Offset | 相对焦点的上下、纵深构图偏移 |
| Duration / Easing | 模式切换时长与缓动曲线 |

## 6. 验收

1. 2D 为平视正交相机；
2. 3D 为无左右偏移的斜上方透视相机；
3. 快速交替切换时画面连续；
4. `CameraModeController` 中不存在 Camera Transform 或镜头参数写入；
5. 演出接管并释放后能回到最新玩法模式；
6. Sandbox 不加入 Build Settings；
7. Unity Console 无新增错误。
