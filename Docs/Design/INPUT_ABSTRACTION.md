# 新 Input System 封装

## 目标和边界

`Project.InputAbstraction.Runtime` 是玩法代码访问输入的唯一入口。调用方不直接
引用 `Keyboard.current`、`Gamepad.current`、`Mouse.current`、`Touchscreen.current`
或 `InputAction`。这样后续接入移动端、双端输入、重绑定和手柄提示时，只需替换或
配置封装内部的数据源。

封装只负责读取输入和报告设备状态，不负责玩家移动、UI 导航状态机、输入消费优先级
或具体玩法行为。

## 快速使用

```csharp
using Project.InputAbstraction;
using UnityEngine;

public sealed class PlayerInputConsumer : MonoBehaviour
{
    private void Update()
    {
        Vector2 move = GameInput.ReadVector2(InputActionId.Move);
        Vector2 look = GameInput.ReadVector2(InputActionId.Look);
        if (GameInput.WasPressedThisFrame(InputActionId.Jump))
        {
            // 请求玩家状态机跳跃。
        }
    }
}
```

可用 API：

- `GameInput.IsPressed(action)`：当前帧持续按住；
- `GameInput.WasPressedThisFrame(action)` / `WasReleasedThisFrame(action)`：按下/松开边沿；
- `GameInput.Axis(action)`：读取一维值；
- `GameInput.ReadVector2(action)`：读取移动、视角、菜单导航等二维值；
- `GameInput.PointerPosition` / `PointerDelta`：鼠标指针位置和增量；
- `GameInput.ActiveDeviceMode` 与 `HasKeyboard/HasMouse/HasGamepad/HasTouch`：设备提示和适配判断。

稳定动作 ID 位于 `InputActionId`，目前包括 `Move`、`Look`、`Navigate`、`Jump`、
`Interact`、`Cancel`、`Submit`、`Pause`、`Crouch`、`Sprint`、`Attack`、
`CameraModeSwitch`、`PointerPrimary` 和 `PointerSecondary`。

## 场景配置

在场景中放置一个 `InputService`（建议放到启动场景的系统根节点）。不配置
`Action Asset` 时，封装会创建默认 `Gameplay` Action Map：

- 键盘：WASD 移动、鼠标移动视角、Space 跳跃、E 互动、Tab 切换视角、Esc 取消/暂停；
- 手柄：左摇杆移动、右摇杆视角、South 跳跃/确认、West 互动、East 取消、Start 暂停；
- 鼠标左/右键分别映射 `PointerPrimary` / `PointerSecondary`。

也可以创建自定义 `InputActionAsset` 并拖到 `InputService.Action Asset`。资产必须有
名为 `Gameplay` 的 Action Map，动作名使用 `InputActionId.ToString()` 的名称。
缺失的动作会安全返回默认值，但建议完整配置以避免运行期功能缺失。

## 双端/移动端适配

后续适配不改玩法调用方，实现 `IInputSource`，把设备、触摸虚拟摇杆、UI 按钮等
合成为同一组动作，然后在启动时替换数据源：

```csharp
InputService service = InputService.EnsureInstance();
service.SetExternalSource(mobileInputSource);
// 或：GameInput.UseExternalSource(mobileInputSource);
```

外部数据源不要求依赖 Unity Input System；它可以由移动端触摸、第二端设备、网络输入
或测试脚本实现。清除覆盖后会恢复 `InputService` 的 Unity Input System 数据源：

```csharp
GameInput.ClearExternalSource(mobileInputSource);
```

外部数据源的生命周期由注册者负责，`InputService` 不会替它调用 `Dispose`。

## 约定和注意事项

- `WasPressedThisFrame` 等边沿查询只能在帧内读取，不要缓存到下一帧；
- 输入封装不消费输入，也不阻止多个系统读取；需要 UI/玩法优先级时由上层协调；
- `PointerPosition` 在没有鼠标时返回 `Vector2.zero`；移动端应通过外部数据源提供等价值；
- `ActiveDeviceMode` 是最近更新设备的提示，不应作为玩法逻辑条件；
- 不要在新功能中直接加入 `UnityEngine.InputSystem` 的设备查询，保持双端适配边界集中。
