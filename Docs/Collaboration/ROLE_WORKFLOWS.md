# 2026Test 角色工作卡

## 所有 Git 成员

把以下内容与任务一起交给 AI：

```text
先读取并严格遵守 $unity-git-branch-worker。
我的角色：ROLE
任务编号：TASK_ID
指定分支：BRANCH_NAME
目标：GOAL
允许修改：ALLOWED_PATHS
禁止修改：FORBIDDEN_PATHS
本任务由我负责的 Scene/Prefab/Data：OWNED_FILES
验收条件：ACCEPTANCE
验证方式：TESTS

不要合并 main，不要强推。完成后只推送自己的分支并按 Skill 格式交接。
```

共同规则：固定 Unity `6000.3.12f1`；从最新 `main` 开始；每人新建并只保存自己的 Scene；不再使用 2D 小拼、2D 总拼、2D 转 3D 或关卡数据块生成流程；公共 Prefab/Data 仍须避免并发编辑。

## 玩法策划

- 在 `Docs/Design/Features` 写清玩法目标、输入、状态、边界和可操作的验收步骤。
- 在自己的 Scene 中做少量场景与点位摆放，使用程序提供的组件和 Prefab。
- 不写 C#、Shader，不修改 Packages、ProjectSettings、渲染管线或他人 Scene。
- 交付玩法文档、参数、自己的 Scene 路径和试玩结果。

## 策划程序

- 把玩法拆成运行时逻辑、配置数据、Prefab、编辑器支持和测试。
- 提供易懂的 Inspector 参数、安全默认值和策划可以直接使用的工作流。
- 先在自己的 Sandbox/Scene 验证，不进入他人 Scene 代为修改。
- 公共底层接口先与程序确认；交付组件、Prefab、接入说明和限制。

## 关卡策划

1. 在 `Assets/_Project/Scenes/Levels/<LevelId>/Work` 新建唯一 Scene，文件名包含负责人或任务标识。
2. 只在该 Scene 做灰盒、路线、空间、视线、碰撞和机关摆放。
3. 使用模块化 Prefab/Variant，不修改他人 Scene，不随意 Unpack 公共 Prefab。
4. 绳子、梯子和平台使用 Scene 视图中的“路径机关工具”；保存 Scene 即完成，不生成总拼或折叠场景。
5. 检查出生点、目标、卡死点、摄像机和碰撞后，推送自己的 Scene 与全部新依赖。
6. 交接 Scene 路径、可试玩入口、待处理效果和已知问题。最终如何组合 Scene 以后由整合者决定。

## 程序

- 负责公共系统、数据流、输入、存档、加载、性能模块和自动测试。
- 给其他人提供稳定、最小、有文档的 API；功能先在自己的测试 Scene 验证。
- 不修改他人关卡 Scene，不顺手移动无关资源，不通过删除未知代码解决编译问题。
- 完成前检查 Unity 编译、相关测试和最小 Smoke Test。
- 打包时只使用整合者指定、已推送且工作区干净的 `main` SHA，并记录平台与结果。

## TA

- 在自己的效果测试 Scene 中制作 Shader、材质、VFX Prefab 和参数。
- 关卡成员在各自 Scene 中引用交付的 Prefab，TA 不直接保存他人 Scene。
- Renderer Asset、Render Pipeline Asset、Packages、Graphics/Quality Settings、全局 Volume 与最终 Bake 交给整合者。
- 交付依赖、可调参数、性能等级、降级方案和接入申请。

## 程序 + TA / 整合者

- 分配任务、路径和验收条件，维护 `main` 与集成分支。
- 检查成员分支是否越权、缺 `.meta`、包含本机/生成目录或大文件。
- 在集成分支执行 Unity 编译、测试、场景检查和开发构建，再更新 `main`。
- 暂不尝试自动合并个人 Scene；需要组合时先确定目标场景结构，再逐项收编。
- 管理 ProjectSettings、Packages、Build Settings、URP 全局管线与正式构建验收。

## 非 Git 成员

2D 像素美术与音乐音效把源文件、运行时导出和 `MANIFEST.md` 放到约定交付区，由指定 Git 成员在独立分支导入。清单必须说明用途、版本、导入参数、循环/切片等要求。
