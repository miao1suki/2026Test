# 游戏启动、多场景与 UI 结构

## 结论

项目采用“一个启动场景 + 多个常驻系统场景 + 一个当前流程场景”的 Additive
结构。UI 单独放在常驻的 `Systems_UI` 场景中，不复制到每个关卡；主菜单、关卡和
结束演出只保存各自内容。这样可以单独编辑关卡，又能保证输入、相机、音频和 UI
始终只初始化一份。

运行时的场景组合如下：

| 阶段 | 常驻场景 | 当前流程场景 |
| --- | --- | --- |
| 启动 | Bootstrap 负责加载 5 个系统场景 | 尚未加载 |
| 主菜单 | Core / Input / Audio / Camera / UI | MainMenu |
| 游戏中 | Core / Input / Audio / Camera / UI | Level_01、02、03 中的一个 |
| 结束演出 | Core / Input / Audio / Camera / UI | Ending |

`Bootstrap` 只负责装载和切换，不放游戏内容。任何时刻只保留一个流程场景，避免
多个关卡同时激活、重复光照设置或重复生成玩家。

## 标准场景位置

```text
Assets/_Project/Scenes/
├─ Bootstrap/Bootstrap.unity
├─ Systems/
│  ├─ Systems_Core.unity
│  ├─ Systems_Input.unity
│  ├─ Systems_Audio.unity
│  ├─ Systems_Camera.unity
│  └─ Systems_UI.unity
├─ Flow/
│  ├─ MainMenu.unity
│  └─ Ending.unity
└─ Levels/
   ├─ Level_01/Level_01.unity
   ├─ Level_02/Level_02.unity
   └─ Level_03/Level_03.unity
```

场景表由
`Assets/_Project/Content/GameFlow/GameSceneCatalog.asset` 统一维护。标准场景按照上面
的顺序写入 Build Settings，`Bootstrap` 必须为第 0 项。原有开发沙盒只用于功能
测试，不进入正式构建。

## 各场景职责

- `Systems_Core`：以后放存档、游戏状态、配置加载等跨关卡服务。
- `Systems_Input`：输入封装和按键绑定。这里关闭 `InputService` 自己的
  `DontDestroyOnLoad`，由常驻系统场景负责生命周期；不要在别处再放一份。
- `Systems_Audio`：全局音乐与音效通道。
- `Systems_Camera`：唯一的 Main Camera、AudioListener、`CameraControlManager` 和
  2D/3D 视角控制器。关卡脚本不得直接写 Camera 数据。
- `Systems_UI`：唯一 EventSystem、全局 Canvas、菜单 / HUD / 结束页路由。平台专属
  UI 继续交给 `PlatformUILayoutController` 管理。
- `MainMenu`：主菜单环境或演出内容；按钮本身在 `Systems_UI`。
- `Level_01` 至 `Level_03`：关卡自身的场景物体、灯光、触发器和 `PlayerSpawn`。
- `Ending`：结束演出的 Timeline、环境和演出专属对象；不重复创建相机。

## 策划日常使用

1. 直接打开要制作的 `Level_XX.unity`。
2. 编辑并保存该关卡自己的物体，不把其他关卡拖进来共同保存。
3. 点击 Play。默认的编辑器桥接会自动从 `Bootstrap` 启动，然后回到刚才打开的
   关卡，因此运行环境与正式打包一致。
4. 如果只想孤立调试当前场景，可取消菜单
   `Tools > 2026Test > 游戏流程 > Play时自动从Bootstrap启动`；调完后重新开启。

新增或修复骨架使用：

`Tools > 2026Test > 游戏流程 > 创建或修复标准场景骨架`

配置自检使用：

`Tools > 2026Test > 游戏流程 > 检查场景配置`

生成器只创建缺失场景，不覆盖已经存在的关卡内容；但会把 Build Settings 修正为
标准正式场景列表。

快速打开分类场景、创建出生点和终点，请使用
`Tools > 2026Test > 场景 > 场景导航与关卡入口`。完整试玩流程见
[可运行游戏原型](PLAYABLE_PROTOTYPE.md)。

## 程序调用 API

所有流程切换通过 `GameFlowController`，不要直接调用 `SceneManager.LoadScene`：

```csharp
using Project.GameFlow;

GameFlowController.Instance.RequestTransition(GameFlowSceneId.Level01);
GameFlowController.Instance.ReloadActiveScene();
GameFlowController.Instance.ReturnToMainMenu();
GameFlowController.Instance.RequestTransition(GameFlowSceneId.Ending);
```

接口在系统初始化和切场过程中会拒绝重复请求，并返回 `false`。需要做淡入淡出或
禁止玩家输入时，可监听：

```csharp
flow.TransitionStarted += OnTransitionStarted;
flow.ActiveSceneChanged += OnActiveSceneChanged;
```

跨系统初始化服务可实现 `IGameSystemService` 并放在对应系统场景的
`GameSystemSceneRoot` 子物体中。服务会按 `InitializationOrder` 从小到大执行一次。

## UI 是否要与 Play 场景拆分

这里采用拆分。全局菜单、HUD、加载遮罩和结束页保持在 `Systems_UI`，能避免每个
关卡复制 Canvas、EventSystem 和手机/电脑布局。仅当某个 UI 完全属于单个关卡、
离开该关卡后没有意义时，才把它放进该关卡的 `LevelContent` 下；它不能再创建
EventSystem，也不能替代全局 UI。

## 约束与协作边界

- 全项目只能有一个 Main Camera、一个 AudioListener、一个 EventSystem。
- 各程序通过相机 Manager、输入服务和游戏流程 API 协作，不跨场景直接查找并修改
  对方组件的私有数据。
- 一个关卡由一个人负责其 `.unity` 文件；公共系统场景由对应功能负责人修改。
- 流程场景负责本场景灯光和环境设置。`SceneManager.SetActiveScene` 会把当前关卡设为
  Active Scene，使运行时新生成物体默认归属当前关卡。
- 打包前运行配置检查和 EditMode 测试；不要手工调整 Bootstrap 的构建序号。
