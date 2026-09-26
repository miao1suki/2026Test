# 方块表面瓦片与正交单向平台

## 给策划的最短流程

1. 在 Scene 中选中一个带 `BoxCollider` 的方块。
2. 如果面板没有显示，选择 `Tools > 2026Test > 方块贴画 > 显示 Scene 绘制工具`。
3. 点击 `让选中方块可贴画`。没有选中物体时，工具会直接创建测试方块。
4. 第一次使用时，在 Project 窗口选中同一张瓦片图中的 Sprite 切片，然后点击 `从 Project 选中切片创建瓦片库`。
5. 选择瓦片并点击 `开始绘制`，鼠标直接在方块六个面上绘制。
6. 场景定稿后点击 `合成贴图并持久化保存`，再保存 Scene。

快捷操作：

- 左键：连续绘制。
- `Shift + 左键`：临时擦除。
- `Ctrl + 左键`：吸取已有瓦片及旋转/翻转设置。
- `Esc`：退出绘制，不影响已画内容。
- 面板可以拖动、折叠和关闭；关闭后可从 `Tools/2026Test/方块贴画` 再次打开。

## 素材准备

瓦片图使用 Unity Sprite 导入方式：

- `Texture Type = Sprite (2D and UI)`。
- 多瓦片图片使用 `Sprite Mode = Multiple` 并先完成切片。
- 同一个 `SurfaceTilePalette` 中的 Sprite 必须来自同一张源图片。
- 建议使用 Point Filter、关闭 Mipmap；最终合成图会自动使用 Point、Clamp、无压缩设置。

每个方块可以独立设置 `每格世界尺寸`。网格按方块当前世界尺寸自动计算，所以不同大小和缩放的正方体仍会落在相同世界单位网格上。方块互相局部重叠不会影响贴画数据；被遮住的面只是看不见，数据仍保留。

## 一键合成与持久化

编辑阶段的每格瓦片记录是源数据；点击合成后，工具会生成：

- 一张只包含该方块六面结果的 PNG 图集；
- 一个引用图集的持久化材质；
- 一个每个有内容的面只有一张四边形的持久化 Mesh。

输出路径为：

`Assets/_Project/Generated/SurfaceTiles/<SceneName>/`

运行时不会逐格重新拼图，也不会动态创建材质。修改方块尺寸、网格或绘制内容后，面板会显示“有未合成改动”，发布前再次点击合成即可。不要手工编辑生成目录中的文件；需要改画面时回到方块上修改源数据并重新合成。

## 正交视角单向平台

在面板启用 `允许从下方穿过、从上方站立` 后，可以勾选 Front、Right、Back、Left 中哪些正交 2D 视角启用平台跳跃规则。

- 匹配的 2D 正交视角：角色向上移动或位于平台下方时穿过；越过平台顶面后恢复碰撞并可落脚。
- 3D 模式、摄像机过渡期间、未勾选的 2D 方向：方块保持普通实体碰撞。
- 切换出单向模式时，如果角色仍与方块相交，会延迟恢复碰撞直到脱离，避免把角色卡在方块里。

此功能不控制角色移动，也不直接控制摄像机。它只读取角色提供的当前模式、投影方向、刚体速度，并只管理角色碰撞体与该方块之间的碰撞忽略状态。

## 程序接入 API

需要使用此规则的角色实现 `Project.SurfaceTiles.IProjectedPlatformActor`：

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

当前 `PlayerController` 已实现此接口。后续替换玩家控制器时，只需迁移接口实现，不需要让平台依赖具体玩家类。

主要运行时类型：

- `SurfaceTileBlock`：方块的网格、瓦片源数据和合成资源引用。
- `SurfaceTilePalette`：瓦片库。
- `ProjectedOneWayPlatform`：按正交方向处理单向碰撞。
- `IProjectedPlatformActor`：玩家侧最小读取接口。

## 协作约定

- 每个人在自己的 Scene 中创建方块并绘制，避免多人同时修改同一个 `.unity` 文件。
- 瓦片库可以复用；若需改库，单独提交 `.asset` 与其 `.meta`。
- 合成产物应和 Scene 一起提交，确保其他人拉取后无需本地重算即可看到最终效果。
- 不要把测试用临时贴图或未使用的合成文件留在正式目录。
