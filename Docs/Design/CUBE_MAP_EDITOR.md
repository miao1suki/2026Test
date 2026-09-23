# 旧版立方体地图编辑器（已停用）

旧版以 `Assets/_Project/Scenes/Levels/LV001` 下的多个 Scene 作为人工编辑源，现已停用并删除。
不要重新创建该目录，也不要调用旧工作区初始化流程。

当前唯一有效流程见 [LEVEL_AUTHORING_PIPELINE.md](LEVEL_AUTHORING_PIPELINE.md)：

- 唯一源是 `Assets/_Project/Development/Levels/<LevelId>/Authoring` 下的数据块；
- Piece、2D 总拼、3D 折叠场景全部是可重新生成的预览；
- 绳子、梯子、平台和网格物品必须收编进对应的 Piece / Face 数据块；
- 发布内容只从同一批数据块生成到 `Assets/_Project/Release`。

Scene View 中保留的分面网格与网格摆放 UI 仍可使用，但场景导航和生成统一从
`Tools > 2026Test > 关卡创作管线 > 打开管线窗口` 进入。
