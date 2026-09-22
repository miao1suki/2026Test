# 梯子路径、攀爬检测与玩家状态机接口

## 模块边界

`Project.LadderPaths` 负责：

- 在四个正交视角下计算梯子上下逻辑端的接续关系；
- 提供有体积但无实体端点的白膜梯子，供关卡策划摆放和后续替换美术；
- 在真实 Trigger 未接触时，以低频投影查询补偿 2D 视角的深度错位；
- 确认玩家和候选梯子之间无遮挡后，通知玩家状态机进入、保持或退出攀爬；
- 在玩家爬离路径最上方时提供短暂的顶部离梯宽限。

模块不实现玩家位移、攀爬速度、输入、动画、跳上平台动作或玩家状态机本身。

## 梯子模型和连接规则

`LadderSegment` 默认宽、高均为 2，厚度为 0.25。正面/背面看是接近正方形，左右
侧面看是窄而高的矩形。白膜由两根立柱和若干横档构成，没有端点球；`Bottom`、
`Top` 只存在于路径数据和编辑器辅助线中。

连接时会按相机视向和每个梯子的旋转判断可见面族：

- `FrontBack`：当前看到梯子的正面或背面；
- `LeftRight`：当前看到梯子的左面或右面。

只有来自不同梯子的 `Top` 与 `Bottom`、投影距离在容差内、且可见面族相同，才会
建立连接。因此正面可以接背面，左面可以接右面，但正面族不会连接侧面族。

## Scene 编辑器

Scene 窗口右上角的“梯子路径编辑”面板支持：

1. 创建 `LadderPathNetwork` 和白膜梯子；
2. 切换 Front/Right/Back/Left 预览方向；
3. 调整接续容差；
4. 选中梯子后整体平移和旋转；
5. 将梯子的上/下逻辑端投影吸附到同面族梯子的相反逻辑端，或做完整 3D 吸附；
6. 查看青绿色逻辑端外延线，以及亮绿色的 3D 接续虚线、箭头和圆环。

白膜尺寸、厚度和横档数量可在 `LadderSegment` Inspector 中调整。

## 玩家状态机接入

玩家控制器实现以下接口：

```csharp
public sealed class PlayerStateMachine : MonoBehaviour, ILadderClimbStateReceiver
{
    public void OnLadderClimbEnter(LadderClimbContact contact)
    {
        // 切换为攀爬状态；可读取 contact.Segment、ClosestPoint 和 Source。
    }

    public void OnLadderClimbStay(LadderClimbContact contact)
    {
        // 保持攀爬；TopExitGrace 时不要立即退出，以便玩家完成上平台动作。
    }

    public void OnLadderClimbExit(LadderClimbExitReason reason)
    {
        // 按玩家系统自己的规则离开攀爬状态。
    }
}
```

然后在玩家对象上添加 `LadderClimbSensor`：

- 玩家需具有 Collider，且玩家或梯子一侧满足 Unity Trigger 回调所需的 Rigidbody 条件；
- `Network` 指向当前关卡的梯子网络；
- `State Receiver` 指向实现接口的玩家状态机；不填写时会在同一对象自动查找；
- `Projection Direction` 由 2D/3D 视角系统切换时同步设置；
- `Obstruction Mask` 只勾选真正能挡住玩家和梯子的场景碰撞层，不要包含梯子 Trigger；
- `Projection Query Interval` 默认 0.05 秒，即每秒最多 20 次候选查询；
- `Top Exit Grace Seconds` 默认 0.18 秒。

代码侧也可以调用：

```csharp
sensor.ProjectionDirection = LadderProjectionDirection.Right;
sensor.SetNetwork(levelLadderNetwork);
sensor.SetStateReceiver(playerStateMachine);
```

## 检测和性能

真实接触由梯子的 `BoxCollider (Is Trigger)` 维护。仅当没有 Trigger 接触时，传感器
才按固定时间间隔遍历当前网络的缓存梯子数组，先做无物理查询的二维范围筛选，最终
只对最近候选执行一次 `Physics.Linecast` 遮挡检查。该流程没有为每根梯子逐帧发射
射线，也不会让每个梯子各自运行 `Update`。

如果玩家位于当前梯子 `Top` 上方、该逻辑端在当前视角没有下一段，并且仍在梯子
水平范围附近，传感器会以 `LadderContactSource.TopExitGrace` 继续发送短暂的 Stay。
玩家状态机负责利用这段时间完成爬上平台的位移或动画。

## 路径 API

不使用 `LadderClimbSensor` 的系统可以直接查询：

```csharp
LadderPathGraph graph = network.GetCachedPath(direction);
if (graph.TryGetNext(currentLadder, LadderEndpoint.Top, out var next))
{
    LadderSegment nextLadder = next.Segment;
    LadderEndpoint entry = next.Endpoint;
}
```

`TryFindClimbableLadder` 可用于自定义玩家传感器；`IsTopOfPath` 和
`CanUseTopExitGrace` 可用于自定义顶部离梯逻辑。运行时程序集不依赖 Editor 代码。
