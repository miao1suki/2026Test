# 绳子路径编辑与四方向接续

## 目标边界

本模块只负责绳子段的编辑、四个正交投影方向下的端点接续检测，以及把平台绑定
到绳子端点。它不实现平台的移动、输入、动画或相机控制。移动系统通过
`RopePathNetwork` 查询路径和下一段。

## 数据结构

- `RopeSegment`：一条绳子，保存本地空间端点 A/B。端点有稳定的 `SegmentId`，并可
  通过 `GetWorldEndpoint`、`SetWorldEndpoint` 读写世界坐标。
- `RopePathNetwork`：场景中的绳子网络。它按四个 `RopeProjectionDirection`
  分别建立缓存图，接续容差默认是 0.12 个世界单位。
- `RopePathGraph`：某个视角的结果，包含投影端点连接、从端点到端点的路径，以及
  `TryGetNext` 查询。它只保存计算结果，不把移动状态写入绳子。
- `RopePlatform`：平台的编辑数据，只保存网络、绳子和停靠端点引用。绑定后可调用
  `SnapTransformToBinding` 对齐位置；移动逻辑由其他系统实现。

## 四方向投影

| 方向 | 屏幕水平轴 | 屏幕垂直轴 | 深度轴 |
| --- | --- | --- | --- |
| Front | 世界 +X | 世界 +Y | 世界 +Z |
| Right | 世界 -Z | 世界 +Y | 世界 +X |
| Back | 世界 -X | 世界 +Y | 世界 -Z |
| Left | 世界 +Z | 世界 +Y | 世界 -X |

每个绳子端点投影成二维坐标。两个来自不同绳子的端点投影距离不超过接续容差时
建立连接；深度不会阻止连接，这正是同一组 3D 绳子在不同正交方向下可以得到不同
拼合路径的关键。一个投影位置有多个端点时会形成分支，完整连接关系仍保存在
`RopePathGraph.Connections` 中。

## Scene 编辑器操作

Scene 窗口左侧的“绳子路径编辑”面板提供：

1. 创建网络、绳子段和平台；
2. 切换 Front/Right/Back/Left 投影方向；
3. 调整接续容差；
4. 对选中的绳子拖动 A/B 端点；黄色虚线显示该端点沿绳子方向向外延伸的预期接续方向；
5. 使用“投影吸附选中端点”把端点移动到当前投影下最近的其他端点，保留自身深度；
6. 如果设计需要 3D 世界坐标也完全重合，可使用“实体吸附选中端点”；
7. 将平台绑定到当前投影下最近的端点，并将平台 Transform 的视觉中心对齐到该端点；
8. 重建四方向路径缓存并查看当前视角的连接数和路径数。

黄色点划线是当前投影已经识别出的端点接续。平台的绑定只改变编辑数据，不会让
平台自动移动。

## 移动系统对接示例

```csharp
RopePathGraph graph = network.GetCachedPath(RopeProjectionDirection.Front);
if (graph.TryGetNext(currentSegment, leavingEndpoint, out RopeEndpointReference next))
{
    // 将平台移动一段绳子的距离，然后停在 next.Endpoint。
    // 具体移动、动画和输入由平台系统负责。
    RopeSegment nextSegment = next.Segment;
    RopeEndpoint nextStop = next.Endpoint;
}
```

如果 `TryGetNext` 返回 false，说明当前端点没有接续绳子。平台系统可以按自己的规则
停留或回退；编辑器不会替它决定移动行为。

## 协作约定

策划只在网络下创建和调整绳子/平台，修改完成后保存对应场景。负责平台移动的程序
只读取 `RopePathNetwork` 和 `RopePathGraph`，不要直接修改 `RopeSegment` 的端点，
也不要在运行时依赖编辑器程序集。
