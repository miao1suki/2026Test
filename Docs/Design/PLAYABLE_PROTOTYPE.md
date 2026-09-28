# 可运行游戏原型与关卡入口

## 当前可跑流程

```text
Bootstrap
  -> 主菜单“开始游戏”
  -> Level 1 生成 RuntimePlayer
  -> 触碰 LevelGoal
  -> Level 2
  -> 触碰 LevelGoal
  -> Level 3
  -> 触碰 EndGame Goal
  -> Ending
  -> 返回主菜单
```

三个关卡已经放入简单地面、台阶、灯光、出生点和发光终点，用于验证移动、跳跃、
2D/3D 相机、暂停和完整切关流程。它们是可替换的原型内容，不代表正式关卡设计。

标准玩家 Prefab：

`Assets/_Project/Content/Player/RuntimePlayer.prefab`

它使用项目现有 `Assets/Player` 控制器，并按其说明配置了 Rigidbody、CapsuleCollider、
触发 Collider、PlayableDirector、PlayerActionRunner、TimelineActorHost、PlatformRider
和 PlayerController。没有复制或绕过现有玩家逻辑。

## 场景导航工具

打开：

`Tools > 2026Test > 场景 > 场景导航与关卡入口`

窗口按以下类别列出场景：

- 启动入口
- 常驻系统
- 流程界面
- 正式关卡
- 开发与测试

每个场景可以单独打开、Additive 叠加打开或在 Project 中定位。单独打开前会使用
Unity 自己的未保存场景确认，避免丢失编辑内容。

窗口顶部还有 Play 启动方式开关：开启为正式 Bootstrap 流程；关闭为直接当前场景调试。直接调试正式关卡时仍会加载系统场景并生成玩家，不再需要为了调一关手工打开相机、输入和 UI 场景。

## 出生点和终点

导航窗口底部的“当前关卡 · 入口与出口”提供：

- `创建 / 选中玩家出生点`
- `创建关卡终点`

位置优先采用当前选中物体的位置，否则使用 Scene 视图中心。创建后可直接用 Unity
移动工具调整。

出生点会自动写入当前 `LevelSceneContext` 并绑定标准玩家 Prefab。进入关卡后由
`GameFlowController` 在关卡成为 Active Scene 后生成玩家，再把唯一的全局相机绑定
到该玩家。

终点的 `LevelGoal` Inspector 可以配置：

- 下一关 `nextScene`
- 是否为游戏结束点 `endsGame`
- 切换延迟 `transitionDelay`

默认规则为 Level 1 -> Level 2、Level 2 -> Level 3、Level 3 -> Ending。策划可以把
任意终点改到其他关卡，或者勾选结束点。

## 游戏 UI

`Systems_UI` 当前提供：

- 主菜单：开始游戏、退出游戏、基本操作说明
- 游戏 HUD：关卡编号、操作提示、ESC 提示
- ESC 暂停菜单：继续、重开本关、返回主菜单
- 结束页：返回主菜单、退出游戏
- 切场提示：右下角小提示，不再用黑色全屏遮挡关卡

暂停使用 `Time.timeScale = 0`，菜单动画和输入使用未缩放时间，因此暂停状态仍能操作。
切关、重开和返回主菜单前会强制恢复时间缩放。

## 当前表现系统

- 相机：`Systems_Camera` 中的 `CameraFollowController` 在玩家出生时自动绑定，并从
  2D 正交模式开始；玩家原有 Tab/F 切换继续工作。
- UI 动画：菜单、HUD、暂停和结束页使用未缩放时间淡入。
- 关卡终点：发光并持续旋转、呼吸缩放。
- 音效：`GameAudioService` 提供无需外部音频文件的临时按钮、暂停和过关提示音。
  美术音频到位后可直接替换为正式 AudioClip。

## 重建原型

只有在明确要丢弃三个原型关卡的 `LevelContent` 时才使用：

`Tools > 2026Test > 游戏流程 > 重建可运行原型（三关）`

该命令会明确确认，并重建 `Systems_UI` 与三关原型内容。正式关卡开始制作后不要再
执行；场景导航、出生点和终点工具仍可安全独立使用。

