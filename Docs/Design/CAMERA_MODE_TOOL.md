# 2D / 3D 相机模式工具

## 1. 目标与当前范围

本工具服务于平台跳跃玩法原型：

- `Side2D`：平视、正交投影，表现为横版平台跳跃；
- `Perspective3D`：斜上方俯视、透视投影，Y 轴旋转固定为 0，不产生左右偏移；
- 两种模式之间平滑过渡，切换过程中允许随时反向打断；
- 相机可以跟随任意 `Transform`，也为后续玩家或物品系统预留接口；
- 本阶段不实现玩家移动、2D 角色朝向或玩法规则。

运行时代码位于：

```text
Assets/_Project/Code/Systems/CameraModes/Runtime
```

策划工具位于：

```text
Assets/_Project/Code/Systems/CameraModes/Editor
```

## 2. 立即体验

打开测试场景：

```text
Assets/_Project/Scenes/Sandbox/Programmer/LevelEditorSandbox.unity
```

场景中的 `Main Camera` 已挂载 `CameraModeController`，跟随对象是
`LevelEditorSandbox/SceneServices/CameraFollowTarget`。进入 Play Mode 后，在
控制器 Inspector 中点击“切换到 2D”或“切换到 3D”。切换过程中连续点击
相反模式，可以验证打断和反向过渡。

该 Sandbox 场景不会加入 Build Settings。

## 3. 策划配置界面

菜单入口：

```text
Tools > 2026Test > 2D-3D 相机配置
```

基本流程：

1. 选择场景中的受控相机；
2. 指定玩家、物品或临时空物体作为跟随目标；
3. 点击“创建 / 更新 CameraModeController”；
4. 在编辑模式使用“预览 2D / 预览 3D”调整构图；
5. 在 Play Mode 使用相同位置的按钮验证动画和打断。

常用参数：

| 参数 | 作用 |
| --- | --- |
| 2D Distance | 相机沿世界 Z 轴后退距离 |
| Orthographic Size | 2D 正交视野大小 |
| 3D Distance | 透视相机到观察中心的距离 |
| Pitch | 俯视角度；不会引入左右旋转 |
| Field Of View | 3D 透视视野角 |
| Vertical / Depth Offset | 相对跟随点的上下、纵深构图偏移 |
| Duration / Easing | 模式切换时长与缓动曲线 |
| Follow Smooth Time | 跟随目标移动的平滑时间 |

编辑器按钮支持 Undo，并只修改当前场景中的相机、控制器与跟随点，不修改
Renderer Asset、Render Pipeline Asset 或 Build Settings。

## 4. 运行时接入

直接指定 Transform：

```csharp
using Project.CameraModes;
using UnityEngine;

public sealed class CameraModeExample : MonoBehaviour
{
    [SerializeField] private CameraModeController cameraModes;
    [SerializeField] private Transform playerCameraAnchor;

    private void Start()
    {
        cameraModes.SetFollowTarget(playerCameraAnchor, true);
    }

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

需要由玩家或物品提供专用跟随点时，实现 `ICameraFollowTarget`：

```csharp
using Project.CameraModes;
using UnityEngine;

public sealed class PlayerCameraTarget : MonoBehaviour, ICameraFollowTarget
{
    [SerializeField] private Transform cameraAnchor;

    public Transform CameraFollowTransform => cameraAnchor;
}
```

随后调用：

```csharp
cameraModes.SetFollowTarget(playerCameraTarget, true);
```

主要公共接口：

| 接口 | 用途 |
| --- | --- |
| `SwitchMode(mode, immediate)` | 平滑或立即切换指定模式 |
| `ToggleMode(immediate)` | 在 2D 与 3D 之间切换 |
| `SnapToMode(mode)` | 立即定位并设置投影，适合初始化或编辑器预览 |
| `SetFollowTarget(...)` | 更换跟随对象；可立即吸附到目标 |
| `ClearFollowTarget(...)` | 清除跟随对象，可保留当前观察中心 |
| `TransitionStarted` | C# 切换开始事件 |
| `ModeChanged` | C# 稳定到目标模式后的事件 |

Inspector 中还提供对应的 UnityEvent，便于无代码连接 UI、音效或玩法提示。

## 5. 切换与冲突规则

控制器不为每次请求启动新协程，而是使用单一可打断状态：

1. 新切换从相机当前的位置、旋转和投影矩阵开始；
2. 相同目标模式的重复请求不会重启动画；
3. 相反模式请求会立即把当前画面作为新的起点；
4. 动画结束后恢复 Unity 原生正交或透视投影矩阵；
5. 组件在动画中被禁用时会清除自定义投影，避免残留状态。

投影过渡使用单相机自定义投影矩阵混合，因此无需双相机或 RenderTexture。
稳定的 2D、3D 端点仍分别使用 Unity 原生正交、透视投影。

不要让其他脚本、Animator 或 Timeline 在同一帧同时写入该 Camera 的 Transform、
`orthographic`、`fieldOfView` 或 `projectionMatrix`。如将来接入 Cinemachine，应让
其中一个系统成为唯一相机写入者，不能让两套跟随逻辑并行运行。

## 6. 验收清单

1. 2D 模式为平视正交相机；
2. 3D 模式为斜上方透视相机，X 坐标始终与跟随点一致；
3. 快速交替切换时画面连续，没有多协程争抢；
4. 移动 `CameraFollowTarget` 时两种模式均能平滑跟随；
5. 禁用再启用组件后不存在残留自定义投影；
6. Sandbox 场景不在 Build Settings；
7. Unity Console 没有新增编译错误。
