# Timeline 相机与 Project 相机管理集成

## 1. 权威边界

项目中的实际 `Camera` 只能由 `_Project` 的 `CameraControlManager` 写入。
Timeline 相机工具不得直接修改：

```text
Camera.transform.position / rotation
Camera.orthographic / orthographicSize
Camera.fieldOfView
Camera.projectionMatrix
```

`CameraControlManager` 是唯一写入 Camera Transform、投影模式、正交尺寸、FOV 和投影矩阵的组件。
`CameraModeController` 是唯一决定当前 2D/3D 模式与 2D 平面 Yaw 的权威。
`CameraFollowController`、Timeline、编辑器预览和其他业务模块只能向
`CameraModeController` 提交申请，或读取它和 `CameraControlManager` 的权威状态，
不得维护第二套模式、投影或平面朝向状态。

`TimelineCamRig` 是 Timeline 与 Project 相机系统之间唯一的运行时适配层。
它实现 `Project.CameraModes.ICameraControlSource`，通过 Project 的
`RequestControl`、`Retarget` 和 `Release` 接口参与控制权仲裁。

## 2. 控制生命周期

`CameraTimelineTrack` 使用轨道级 Mixer 管理层级：

1. 轨道出现第一个有效片段时，`TimelineCamRig` 申请 `Cutscene` 优先级控制权；
2. 轨道内连续片段或片段间隙不再重复抢占相机，避免画面跳回玩法模式；
3. 最后一个相机片段结束后，轨道停止或 Timeline 结束时统一释放 Handle；
4. 释放后由 Project `CameraModeController` 恢复当前 2D 或 3D 玩法视角；
5. 如果 Project 更高优先级来源正在控制相机，Timeline 只更新期望状态，不直接写入。

## 3. 2D / 3D 模式申请

`Project.CameraModes.CameraModeController` 是唯一模式权威：

- `CameraViewModeRequestHandle` 表示一次可撤销的模式申请；
- `ICameraViewModeRequester` 提供申请者名称和优先级；
- `RequestMode` 负责把申请加入权威队列；
- `CameraControlManager` 在写入 Camera 前强制应用权威投影；
- `orthographicSize`、`fieldOfView` 和最终投影模式不再由 Timeline 决定。
- 2D 平面 Yaw 也只由 `CameraModeController` 保存和推进；其他模块提交的 Yaw 只是申请参数。

`CameraTimelineClip` 只能选择：

- `None`：不申请，保持 Project 当前模式；
- `Side2D`：申请切换为正交 2D；
- `Perspective3D`：申请切换为透视 3D。

申请 `Side2D` 时可通过 `overrideSide2DYaw` 提供绝对
`side2DYawDegrees`，表示转向完成后的 2D 平面朝向。

片段开始申请，片段停止或结束时释放申请。释放后自动回到剩余申请中优先级最高的模式。

`CameraFollowController` 通过 `SetRequestedMode` 持有玩法侧申请，
自定义 Inspector 修改“申请模式”时会直接提交申请，而不是只修改显示字段。
内部 API 可传入任意角度，Inspector 提供“向左90”和“向右90”两个累加按钮；
每点击一次都会在当前申请 Yaw 上增加或减少 90 度。
`CameraModeController` Inspector、相机工具面板和编辑预览也通过同一申请通道工作，
不会调用第二套切换逻辑。

## 4. 2D 正交轴约束

正交片段可启用世界轴约束：

- `allowPositionX/Y/Z` 决定相机允许沿哪些轴移动；
- 只保留 X 时，得到只能左右移动的横版相机；
- 锁住 Y、Z 时，避免相机离开指定 2D 平面；
- `clampPositionX/Y/Z` 可进一步限制每个轴的世界坐标范围；
- 约束只在目标投影为正交时执行，不影响透视 3D 运镜。

## 5. 手动接管

`OrbitCameraControl` 只在以下条件同时满足时工作：

1. Timeline 已实际持有 Project 相机控制权；
2. 当前片段开启手动接管；
3. Input System 可用。

它只修改 `TimelineCamRig` 的环绕参数，最终仍由
`CameraControlManager` 写入 Camera。

## 6. 其他 Timeline 轨道

打击、位移、音效和特效轨道继续保留原有职责。它们与 Project 相机接口没有
直接冲突，因此不强行引入 Project 相机类型或生命周期依赖。
