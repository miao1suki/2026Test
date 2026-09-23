# 新 Input System 封装

## 目标和边界

`Project.InputAbstraction.Runtime` 是玩法代码访问输入的唯一入口。调用方不直接
引用 `Keyboard.current`、`Gamepad.current`、`Mouse.current`、`Touchscreen.current`
或 `InputAction`。这样后续接入移动端、双端输入、重绑定和手柄提示时，只需替换或
配置封装内部的数据源。

当前支持两种打包操作模式：

- `Desktop`：键盘、鼠标和手柄；
- `Mobile`：触摸虚拟控件与原生 Input System 组合，手柄仍然同时可用。

`Automatic` 会随打包目标选择模式：Android/iOS 使用 `Mobile`，其他目标使用
`Desktop`。玩法代码可以通过 `GameInput.PlatformMode` 查询当前模式，但不要据此
分叉玩法逻辑。

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

## 虚拟摇杆和触摸按钮

在 Hierarchy 中使用：

`GameObject > 2026Test > Input > 创建双平台输入 UI`

工具会生成：

- 左侧 `MoveJoystick`；
- 右侧 `LookJoystick`；
- 跳跃、互动和视角切换触摸按钮；
- 使用新 Input System 的 `EventSystem`；
- `PlatformUILayoutController` 双平台布局控制器。

`VirtualJoystick` 和 `VirtualInputButton` 都直接写入封装内部的虚拟输入源，玩法代码
继续使用 `GameInput`，不需要识别触摸控件。可复制按钮并在 Inspector 中修改
`Action`，也可把摇杆映射到 `Move`、`Look` 或 `Navigate`。

要在 Standalone 编辑器中预览手机触摸输入，可在场景的 `InputService` 上临时把
`Platform Mode` 设为 `Mobile`；最终提交前通常恢复 `Automatic`。

## Desktop/Mobile UI 预设

`PlatformUILayoutController` 可以放在任意 Canvas 根节点，不只限于输入 UI：

1. 点击“收集所有子 UI 为布局对象”，决定哪些 `RectTransform` 参与预设；
2. 在当前 Build Target 下调整 UI；
3. 点击“保存到当前预设”，或直接切换 Build Target；
4. 当 Standalone 与 Android/iOS 互相切换时，编辑器会先捕获旧平台布局，再应用新平台布局；
5. `Desktop Only Objects` 和 `Mobile Only Objects` 用于控制平台专属 UI 显隐。

布局预设分别保存锚点、位置、尺寸、Pivot、缩放和旋转。运行时会在 `Awake` 按实际
打包平台应用对应预设，因此桌面与手机 UI 的位置互不覆盖。第一次收集的新对象会用
当前布局初始化两套预设，之后分别调整即可。

## 约定和注意事项

- `WasPressedThisFrame` 等边沿查询只能在帧内读取，不要缓存到下一帧；
- 输入封装不消费输入，也不阻止多个系统读取；需要 UI/玩法优先级时由上层协调；
- `PointerPosition` 在没有鼠标时返回 `Vector2.zero`；移动端应通过外部数据源提供等价值；
- `ActiveDeviceMode` 是最近更新设备的提示，不应作为玩法逻辑条件；
- 手机模式会合并虚拟触摸与硬件输入；不要为了手柄再创建第二套玩家控制逻辑；
- 切换打包平台前确保当前场景处于打开状态，自动同步只处理已加载场景中的控制器；
- 不要在新功能中直接加入 `UnityEngine.InputSystem` 的设备查询，保持双端适配边界集中。
