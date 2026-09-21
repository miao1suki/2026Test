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

## 3. 2D / 3D 投影

`CameraTimelineClip` 可开启投影覆盖：

- `Orthographic`：正交 2D，可设置 `orthographicSize`；
- `Perspective`：透视 3D，可设置 `fieldOfView`；
- 投影变化通过 `CameraControlManager.Retarget` 使用 Project 的统一投影过渡；
- 未开启覆盖时，Timeline 保持 Project `CameraModeController` 当前投影。

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
