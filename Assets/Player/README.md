# 玩家系统使用说明

## 目录结构

```text
Assets/Player/
  PlayerController.cs
  Actions/
    PlayerActionBinding.cs
    PlayerActionRunner.cs
  State/
    IPlayerState.cs
    PlayerStateContext.cs
    PlayerStateId.cs
    PlayerStateMachine.cs
    States/
      PlayerNormalState.cs
      PlayerActionState.cs
      PlayerClimbState.cs
      PlayerLockedState.cs
  Signals/
  TimelineSO/
    Demo_ActSO.asset
  Editor/
```

## 组件职责

`PlayerController`：玩家总入口，处理输入、移动、跳跃、蹲下、视角申请、平台承载，并提供可选的梯子攀爬接口。

`PlayerActionRunner`：把 `ActSO.Timeline` 交给 `PlayableDirector` 播放。

`ActSO`：动作数据盒，保存动作编号、名字、Timeline、优先级、打断、锁移动和默认下一动作。

`PlayerActionBinding`：把 `InputActionId` 映射到 `ActSO`。

状态机包含四个主状态：

```text
Normal    普通玩法
Action    Timeline 动作
Climbing  攀爬
Locked    外部系统锁定玩家
```

## 玩家最小配置

玩家 GameObject 需要：

```text
Tag: Player
Rigidbody
CapsuleCollider
BoxCollider (Is Trigger)
PlayableDirector
PlayerActionRunner
TimelineActorHost
PlatformRider
PlayerController
```

`PlayerController` 的引用可以在 Inspector 中手动指定，也可以留空让运行时自动查找。

`LadderClimbSensor` 不再是必备组件。项目中的 `LadderPaths` 模块仍然保留，
需要梯子玩法时再单独挂载。

## Inspector 自动检查

`PlayerController` Inspector 最底部提供：

```text
玩家组件检查
一键补齐玩家组件
```

检查会提示：

```text
GameObject Tag 不是 Player
缺少 Rigidbody
缺少 CapsuleCollider
缺少 PlayableDirector
缺少 PlayerActionRunner
缺少 TimelineActorHost
缺少 PlatformRider
PlayerController 没有引用核心组件
```

一键补齐会自动添加缺失组件、设置 `Player` Tag，并回填：

```text
Rigidbody
CapsuleCollider
PlayerActionRunner
PlatformRider
TimelineActorHost.attackPoint
```

相机、绳索网络和动作绑定属于场景或玩法配置，不会被自动猜测。

## 输入使用

输入由项目的 `InputAbstraction` 统一管理，玩家脚本不直接读取 `Keyboard.current` 或 `Mouse.current`。

当前默认键位：

| 功能 | 键位 |
|---|---|
| 移动 | `WASD` / 手柄左摇杆或十字键 |
| 3D 视角 | 鼠标 / 右摇杆 |
| 跳跃 | `Space` / 手柄下键 |
| 冲刺 | `Left Shift` / 左肩键 |
| 蹲下 | `Left Ctrl` / 按下左摇杆 |
| 切换 2D/3D | `Tab` 或 `F` / 手柄 Select |
| 2D 向左转 | 鼠标左键 |
| 2D 向右转 | 鼠标右键 |
| 交互、攻击、确认、取消、暂停 | 由 `InputActionId` 决定 |

移动、冲刺、蹲下和 `CameraModeSwitch` 由 `PlayerController` 直接处理，不占用动作绑定。

蹲下和冲刺都跟随按键映射里的触发策略：

```text
仅点击：按一次切换状态，再按一次结束状态
仅长按：按住时保持状态，松开后结束
点击/长按可切换：运行时可在两种方式之间切换
```

跳跃、主动作、进入攀爬或 Timeline 接管控制时，会取消蹲下和冲刺的切换状态。

## 动作绑定

需要播放 Timeline 的输入，在 `PlayerController > 输入动作绑定` 中添加：

```text
InputActionId -> ActSO
```

示例：

```text
Attack   -> AttackActSO
Interact -> DoorOpenActSO
Jump     -> JumpActSO
```

行为差异：

```text
Jump 未绑定 ActSO：使用内置物理跳跃
Jump 已绑定 ActSO：内置物理跳跃停用，改为播放 Timeline
其他输入未绑定：输入被识别，但没有动作可执行
```

`ActSO` 主要管理以下规则：

```text
优先级
是否允许打断
是否锁定移动
播放速度
动作结束后默认切换到哪个动作
```

基础移动和纯状态交互不需要 `ActSO`；只有需要完整 Timeline 序列的动作才建议绑定。

## Timeline 信号

`PlayerController` 预留了通用入口：

```csharp
public void ReceiveTimelineSignal()
```

其他脚本可以订阅：

```csharp
playerController.TimelineSignalReceived += OnTimelineSignal;
```

后续推荐在 Timeline 中使用 Signal 轨道，把不同时间点连接到不同的公开方法，例如：

```text
OnAttackStart
OnAttackHit
OnAttackEnd
OnInteractExecute
```

Timeline 负责时间轴和表现，Signal 负责通知玩法脚本，具体伤害、状态、冷却等逻辑仍然写在脚本里。

Signal Asset 建议保存在：

```text
Assets/Player/Signals
```

动作数据盒资产保存在：

```text
Assets/Player/TimelineSO
```

其中 `Demo_ActSO.asset` 是示范数据盒，可以选择自己的 `TimelineAsset` 后直接绑定到 `PlayerController` 的动作输入。

## 平台承载

玩家的 `PlatformRider` 由 `PlatformMove` 自动控制，正常情况下不需要手动调用。

运行时流程：

```text
平台创建 __RiderZone
-> 检测玩家靠近平台表面
-> 创建或复用 __RiderAnchor
-> 玩家临时挂到锚点
-> 平台移动时玩家跟随
-> 跳跃或离台时自动解除
```

平台判定只在玩家脚底接近平台表面时生效，避免在空中被重新吸住。

## 梯子接口

当前 `TestSceneRoy` 演示中已经移除梯子对象，生成器也不会再创建梯子。
`LadderPaths` 和玩家攀爬状态作为可选系统保留。

使用流程：

```text
靠近梯子
-> 按住 W / 上
-> 按住 S / 下
-> 松开停在梯子上
-> 离开范围退出攀爬
```

靠近梯子只会记录候选，不会自动把玩家吸过去。当前玩家状态只沿当前梯子段移动；跨梯段接续数据已存在，但尚未加入玩家跨段逻辑。

## 外部控制接口

常用 API：

```csharp
TryPlayAction(ActSO)
TryPlayAction(InputActionId)
ReturnToNormal()
SetControlLocked(bool)
PushControlLock()
PopControlLock()
ReceiveTimelineSignal()
```

常用事件：

```csharp
ActionStarted
ActionCompleted
ControlLockChanged
TimelineSignalReceived
```

## Inspector 说明

玩家、动作运行器、`ActSO`、平台承载和平台判定区都提供了中文 Inspector。

跳跃绑定 Timeline 时，物理跳跃相关字段会自动隐藏；运行时状态会在 Play Mode 中以只读方式显示。项目公共的“组件说明”折叠面板也会显示玩家和动作系统的主要职责与使用方式。
