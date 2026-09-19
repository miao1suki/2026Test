# 2026Test 任务与交接模板

## 1. 整合者派发任务模板

```text
任务编号：
任务名称：
负责人 / 角色：
指定分支：agent/AGENT_ID/TASK-SLUG

目标：

输入与依赖：

允许修改：
- <路径>

禁止修改：
- ProjectSettings/**
- Packages/**
- 未列出的正式场景、Prefab 和 Data

预约文件：
- <Scene/Prefab/Data 路径>

验收条件：
1. <条件>
2. <条件>
3. <条件>

必须验证：
- <验证步骤>

预计交接时间：
```

## 2. 发给成员 AI 的完整模板

```text
使用 $unity-git-branch-worker。

你正在参与 Unity 项目 2026Test。
我的角色：ROLE
任务编号：TASK_ID
指定分支：BRANCH_NAME

任务目标：
GOAL

允许修改：
ALLOWED_PATHS

禁止修改：
FORBIDDEN_PATHS

本任务由我负责编辑的 Scene/Prefab/Data：
OWNED_FILES

验收条件：
ACCEPTANCE

必须执行的验证：
TESTS

开始前先检查仓库、分支和工作区。不要扩大任务范围，不要修改未授权文件，不要手工修改 Unity GUID，不要合并 main、创建 PR 或 force push。

完成后：
1. 检查完整 diff；
2. 提交并推送自己的分支；
3. 输出分支名、完整 SHA、主要文件、验证结果、风险和工作树状态；
4. 不执行合并。
```

## 3. 成员交接模板

```text
任务编号：
负责人 / 角色：
分支：
完整提交 SHA：

结果：

主要文件：
- <路径>

Scene/Prefab/Data 改动：
- 文件：
  修改的对象/区域：

验证：
- 命令或操作：
  结果：通过 / 失败

验收条件：
1. 通过 / 未通过：
2. 通过 / 未通过：

已知风险：无 / 说明
未完成项：无 / 说明
需要整合者执行：无 / 管线接入、烘焙、Build Settings 等
工作树：clean / 不干净及原因
```

## 4. 场景 / Prefab / Data 预约模板

```text
文件：
拥有者：
分支：
用途：
状态：编辑中 / 已推送待合并 / 可以接手
开始时间：
预计交接：
确认可接手时间：
```

规则：这不是技术锁定。只有整合者可以宣布“已合并，可以接手”。成员推送后，在合并完成前仍不安排第二个人编辑。

## 5. 玩法需求模板

```text
# TASK_ID FeatureName

## 玩家目标

## 输入与触发条件

## 完整规则

## 状态与状态切换

## 数值与可调参数

## 成功、失败与中断

## 反馈
- 视觉：
- 音频：
- UI：

## 边界情况

## 需要程序提供的组件/工具

## 验收步骤
1. <步骤>
2. <步骤>
3. <步骤>
```

## 6. TA 管线变更申请模板

```text
效果名称：
效果 Prefab / Material / Shader：
目标平台和质量等级：

需要的管线改动：
- Renderer Feature / Volume / Renderer Asset / 其他

插入阶段或时机：
需要的输入：Color / Depth / Normal / Motion / 其他
输出：
为什么普通材质或现有接口无法完成：
性能风险：
关闭或降级方式：
Sandbox 验证方法：
```

TA 只提交效果本体和申请，由整合者执行全局管线接入。

## 7. 外部美术 / 音频交付清单

```text
任务编号：
作者：
资产类型：2D / UI / SFX / Music
版本：
用途：

Source 文件：
- <文件>

Runtime 文件：
- <文件>

导入要求：
- Sprite PPU / Pivot / Slice：
- Filter / Compression：
- Audio Sample Rate / Channel / Loop：
- 其他：

替换的旧资产：无 / 路径
授权或来源说明：原创 / 已确认授权 / 说明
```

## 8. 整合检查清单

```text
[ ] main 和远端同步，工作树干净
[ ] 成员分支、SHA 与交接一致
[ ] 修改范围符合任务
[ ] 没有 Library/Logs/UserSettings/生成文件
[ ] 新增 Unity 资源都有 .meta
[ ] 没有凭据、未知来源文件或异常大文件
[ ] 没有未经批准的 Packages/ProjectSettings/管线修改
[ ] 代码完成编译和相关测试
[ ] Prefab 引用正常
[ ] 分层场景组合正常
[ ] 没有 Missing Script / Missing Material / 粉色 Shader
[ ] Lighting/NavMesh/Build Settings 已按需统一生成
[ ] 开发构建成功并实际运行
[ ] main 已更新并发布新 SHA
[ ] 已宣布相关文件可以由下一位成员接手
```

## 9. 构建记录模板

```text
构建类型：Development / Release Candidate / Release
构建执行人：整合者 / 主程
Git 分支：main
完整提交 SHA：
Unity 版本：6000.3.12f1
目标平台：
构建选项：
构建时间：
输出名称：PROJECT_VERSION_PLATFORM_SHORT_SHA
输出位置：

Smoke Test：
1. 通过 / 失败：
2. 通过 / 失败：

Console / Player Log 异常：无 / 说明
已知问题：无 / 说明
整合者验收：待验收 / 通过 / 拒绝
```
