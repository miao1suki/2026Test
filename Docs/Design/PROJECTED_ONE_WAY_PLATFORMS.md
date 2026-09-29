# 正交视角单向平台

这套功能与方块表面贴画完全独立。任何带 `BoxCollider` 的场景物体都可以成为单向平台，不需要瓦片库、贴画网格或 `SurfaceTileBlock`。

## 给策划的最短流程

1. 在 Scene 中选中需要作为平台的物体。
2. 选择 `Tools > 2026Test > 正交单向平台 > 显示 Scene 工具`。
3. 点击 `让选中物体成为单向平台`。物体没有 `BoxCollider` 时会自动补齐；没有选中物体时会创建一个测试方块。
4. 勾选 Front、Right、Back、Left 中需要生效的正交 2D 视角，并按需调整落脚容差和“投影纵深”。
5. 保存 Scene。

面板可在 Scene 窗口中拖动、折叠和关闭。需要恢复普通实体碰撞时，选中平台并点击 `移除单向平台功能`；这不会删除物体本身或表面贴画。

## 行为规则

- 匹配的 2D 正交视角：系统按当前 Front/Right/Back/Left 方向建立一个不可见碰撞代理。代理只沿视角深度延伸，所以纵深错开的方块会按 Game 画面中的投影成为平台。
- 玩家从上方落到代理表面时，会在当前画面不可见的纵深轴上自动对齐到实体平台中心。画面中的横向与高度不变，但玩家的真实世界坐标已经位于方块顶面，因此继续切换正交方向或返回 3D 时不会失去支撑。
- 角色向上移动或位于平台下方时穿过代理；越过平台顶面后恢复碰撞并可落脚。
- 2D 方向变化时代理立即按相机目标角度重建，不等待相机缓动结束。3D 模式和未勾选方向使用物体原始碰撞。
- 切换出单向模式时，如果角色仍与平台相交，会延迟恢复碰撞直到脱离，避免把角色卡在平台里。
- 绳索移动平台会自动使用同一代理作为承载面；乘客检测和跟随也读取代理，因此有纵深视差时仍能跳上并随平台移动。

此模块不接管角色日常移动，也不直接控制摄像机。它读取角色提供的当前模式、投影方向和刚体速度；只有角色确认落脚时，才通过可选的落位接口请求角色自行修正纵深坐标。

## 程序接入 API

需要使用此规则的角色实现 `Project.ProjectedPlatforms.IProjectedPlatformActor`：

```csharp
public interface IProjectedPlatformActor
{
    Rigidbody ProjectedPlatformBody { get; }
    RopeProjectionDirection ProjectedPlatformDirection { get; }
    bool IsProjectedPlatformModeActive { get; }
}
```

- `ProjectedPlatformBody`：角色主刚体，用于读取竖直速度。
- `ProjectedPlatformDirection`：当前 Front/Right/Back/Left 投影方向。
- `IsProjectedPlatformModeActive`：目标模式为 2D 正交时返回 `true`。方向切换缓动期间仍保持投影承载，切向 3D 时立即退出。

当前 `PlayerController` 已实现此接口。后续替换玩家控制器时只需迁移接口实现，不需要让平台依赖具体玩家类。

为了让角色在切换视角后仍站在实体平台上，角色还可以实现可选接口：

```csharp
public interface IProjectedPlatformAlignmentReceiver
{
    bool TryAlignProjectedPlatformDepth(
        ProjectedPlatformAlignment alignment);
}
```

`alignment.WorldPosition` 只修正当前视角的纵深轴；角色实现负责把自己的物理 Motor 移到该位置并清除纵深速度。返回 `false` 可以拒绝本次落位，例如角色正处于不可传送的特殊状态。

主要运行时类型：

- `ProjectedOneWayPlatform`：按正交方向执行单向碰撞，并维护不可见的投影深度代理。
- `ProjectedOneWayPlatformSensor`：检测进入平台影响范围的角色碰撞体。
- `IProjectedPlatformActor`：玩家侧最小读取接口。
- `IProjectedPlatformAlignmentReceiver`：玩家侧可选落位接口，由玩家控制器决定如何移动自身 Motor。
- `ProjectedPlatformDirections`：平台在哪些正交方向生效的标记集合。

`ProjectionDepth` 默认 100 个世界单位。它不是平台视觉尺寸，而是碰撞代理能覆盖的关卡纵深；一般不需要调整，只有关卡纵深超过该范围时才增大。
