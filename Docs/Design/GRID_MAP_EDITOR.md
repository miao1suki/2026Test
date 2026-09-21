# 网格地图搭建工具

## 1. 使用范围

网格工具只编辑独立的小拼图 Scene，不直接编辑 2D 总拼或 3D 主场景的
`[GENERATED] Cube Map`。策划先在自己的小拼图 Scene 中摆放内容，之后由拼合工具
重新生成预览。

打开 Scene View 右上角的工具 HUD，在顶部切换：

`拼合预览  |  网格地图`

两种工具共享一套 Scene View 层，始终只展开当前选择的工具，之后新增工具也应接入
`CubeMapEditorToolState`，不要再把面板全部同时铺满画面。

## 2. 网格尺寸

网格尺寸永远来自当前 `CubeMapWorkspaceDefinition`：

`
单元格宽 = FaceWidth / ColumnsPerFace
单元格高 = PieceHeight / RowsPerPiece
`

默认工作区是 12 × 4 世界单位、6 × 2 单元格，所以当前单元格为 2 × 2。策划不应
在物品预制体上手填世界尺寸；物品的宽高只填写“占用几格”。

每个小拼图的四个面各自共享同一网格，编辑面可在 UI 的“当前网格”区域切换：

`正前 / 正右 / 正后 / 正左`

## 3. 物品仓库和预制体

网格工具会创建并维护：

`
Assets/_Project/Content/Data/MapItems/LV001_GridMapPalette.asset
Assets/_Project/Content/Data/MapItems/<物品>.asset
Assets/_Project/Content/Prefabs/MapItems/<物品>.prefab
`

“快速制作预制体并加入仓库”流程：

1. 在“源物体”选择场景中的 GameObject，或直接选择已有 Prefab；
2. 输入显示名称与占用宽、高（单位是格）；
3. 选择默认是否允许叠加、是否吸附网格；
4. 点击“制作预制体并添加”；
5. 新物品会自动进入仓库并成为当前物品。

源物体是场景物体时，工具会复制成独立 Prefab，不会把源场景对象改造成 Prefab
实例。物品定义保存预制体引用、单元格尺寸、枢轴偏移和默认选项。

## 4. 当前物品和预览

当前物品区域会明显显示名称、预制体缩略图、占用格数和旋转角度。选中仓库物品后，
Scene View 鼠标所指处会显示半透明矩形预览：

- 绿色：在面边界内且不与禁止叠加物品相交，可以放置；
- 红色：超出边界或与禁止叠加物品相交，点击不会放置；
- 当前物品的 footprint 会随旋转即时交换宽、高。

`R` 顺时针旋转 90°，`Shift+R` 反向旋转；UI 中的 ↶ / ↷ 按钮作用相同。

两个临时选项只影响当前摆放，不会修改物品定义资产：

- `允许叠加其他物品`：忽略占用相交检查；
- `强制吸附到网格`：关闭后物品跟随鼠标自由摆放，但仍显示 footprint 和边界检查。

## 5. 鼠标操作

| 操作 | 有当前物品 | 没有当前物品 |
| --- | --- | --- |
| 左键点击 | 放置一个物品 | 选中物品并显示选择框 |
| 左键按住拖动 | 框选范围内批量按格放置 | 框选范围内批量选择 |
| 右键点击 | 删除鼠标下物品 | 删除鼠标下物品 |
| 右键按住拖动 | 连续删除经过路径的物品 | 连续删除经过路径的物品 |
| Delete / Backspace | 删除选中物品 | 删除选中物品 |
| Alt | 暂时交还 Scene View 导航 | 暂时交还 Scene View 导航 |

所有新增、删除和批量操作均进入 Unity Undo。右键拖动使用去重集合，不会因为同一
物品被经过多次而重复销毁。

## 6. 代码分层

`
Runtime/
  GridMapItemDefinition.cs   物品预制体和占用格数
  GridMapPalette.cs           可选物品仓库
  GridMapPlacement.cs         场景实例的格子、旋转和临时选项

Editor/
  GridMapEditorService.cs     创建资产、实例化、删除和网格计算
  GridMapEditorState.cs       当前面、当前物品、旋转和 UI 临时状态
  GridMapSceneInteraction.cs  Scene View 鼠标、预览、框选和删除
  GridMapSceneHud.cs          UI Toolkit 面板
  CubeMapEditorToolState.cs   拼合/网格/未来工具折叠管理
`

`GridMapPlacement` 必须挂在每个由仓库放置的实例根节点上，不能只依赖物体名称。
拼合服务会复制这些实例及其子节点，所以网格摆放结果会自然出现在 2D 总拼和 3D
主场景中。

## 7. 多人协作约定

- 每名策划只编辑自己的 `LV001_Piece_XX.unity`；
- 物品预制体和定义资产放在共享仓库中，新增物品后在提交说明中写清名称与占用格数；
- 不直接编辑生成场景；
- 不在生成场景中手动添加临时物品；
- 修改工作区的列数、行数或面尺寸后，应通知所有策划，因为这会改变单元格大小；
- 同一个小拼图 Scene 仍遵循 Git 分支工作流，不要多人同时提交同一 Scene。

## 8. 验收清单

1. 工作区参数改变后，UI 显示新的单元格宽高；
2. 1×1、横向长物体和旋转后的 footprint 都能正确对齐；
3. 红色预览不能越过面边界或禁止叠加物体；
4. 关闭吸附后可自由摆放，并保留选项与删除能力；
5. 批量放置、框选、右键路径删除和 Delete 都支持 Undo；
6. 预制体制作后自动出现在仓库；
7. 拼合预览仍能复制网格实例；
8. 拼合工具与网格工具不会同时占满 Scene View；
9. Unity 版本保持项目当前版本，不修改 `ProjectVersion.txt`。
