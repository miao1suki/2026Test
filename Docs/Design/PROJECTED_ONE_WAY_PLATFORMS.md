# 正交视角单向平台

这套功能与方块表面贴画完全独立。任何带 `BoxCollider` 的场景物体都可以成为单向平台，不需要瓦片库、贴画网格或 `SurfaceTileBlock`。

## 给策划的最短流程

1. 在 Scene 中选中需要作为平台的物体。
2. 选择 `Tools > 2026Test > 正交单向平台 > 显示 Scene 工具`。
3. 点击 `让选中物体成为单向平台`。物体没有 `BoxCollider` 时会自动补齐；没有选中物体时会创建一个测试方块。
4. 勾选 Front、Right、Back、Left 中需要生效的正交 2D 视角，并按需调整落脚容差。
5. 保存 Scene。

面板可在 Scene 窗口中拖动、折叠和关闭。需要恢复普通实体碰撞时，选中平台并点击 `移除单向平台功能`；这不会删除物体本身或表面贴画。

## 行为规则

- 匹配的稳定 2D 正交视角：角色向上移动或位于平台下方时穿过；越过平台顶面后恢复碰撞并可落脚。
- 3D 模式、摄像机过渡期间、未勾选的 2D 方向：物体保持普通实体碰撞。
- 切换出单向模式时，如果角色仍与平台相交，会延迟恢复碰撞直到脱离，避免把角色卡在平台里。

此模块不控制角色移动，也不直接控制摄像机。它只读取角色提供的当前模式、投影方向和刚体速度，并管理角色碰撞体与平台碰撞体之间的忽略状态。

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
- `IsProjectedPlatformModeActive`：仅在稳定的 2D 正交模式返回 `true`；3D 与过渡阶段返回 `false`。

当前 `PlayerController` 已实现此接口。后续替换玩家控制器时只需迁移接口实现，不需要让平台依赖具体玩家类。

主要运行时类型：

- `ProjectedOneWayPlatform`：按正交方向执行单向碰撞规则。
- `ProjectedOneWayPlatformSensor`：检测进入平台影响范围的角色碰撞体。
- `IProjectedPlatformActor`：玩家侧最小读取接口。
- `ProjectedPlatformDirections`：平台在哪些正交方向生效的标记集合。
