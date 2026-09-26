# 2026Test 团队协作总流程

## 当前决定

- Unity 固定使用 `6000.3.12f1`，任何人不得自行升级 Unity、Packages 或渲染管线。
- `main` 是稳定集成分支，成员不得直接推送；最终合并和冲突处理由整合者完成。
- 所有 Git 成员使用 `$unity-git-branch-worker`，一个任务一个短期分支。
- 2D 小拼、2D 总拼、2D 转 3D、关卡数据块和自动发布场景流程已经取消。
- 每个人直接创建、维护自己的 Unity Scene；现阶段不合并多人同时编辑的场景。
- 绳子、梯子、平台和摄像机 2D/3D 视角切换工具继续维护。
- 正式包可由整合者或主程执行，但必须基于整合者指定且已推送的 `main` SHA。

## 文件结构

```text
Assets/_Project/
  Code/Systems/<Feature>/{Runtime,Editor,Tests}
  Content/                 共享数据、材质和内容资产
  Prefabs/                 可复用基础 Prefab 与 Variant
  Rendering/               Shader、材质、VFX 与管线配置
  Scenes/
    Bootstrap/             公共入口
    Levels/<LevelId>/Work/ 每人独立的工作 Scene
  Development/Sandbox/    可丢弃的系统实验场景
  Release/                 仅放整合确认后的发布内容
Docs/{Design,Collaboration}
```

关卡 Scene 建议命名为：

```text
Assets/_Project/Scenes/Levels/<LevelId>/Work/<owner-or-task>_<purpose>.unity
```

不按职位创建关卡目录；目录围绕关卡组织，文件名用负责人或任务标识避免同名。

## 场景协作

1. 每位成员为任务新建唯一 Scene，不复制覆盖他人的工作 Scene。
2. 场景中可直接摆放地形、绳子、梯子、平台和玩法对象。
3. 可复用能力必须做成脚本、Prefab、材质或独立 Data，供不同 Scene 使用。
4. 不依赖跨 Scene 拖拽引用；需要共享状态时由程序提供 Service、事件、稳定 ID 或运行时注入。
5. 推送时明确交接 Scene 路径、依赖资产和试玩入口。
6. 关卡完成前不设计自动拼场景或 Unity YAML 场景合并；最终组合方案由整合者另行决定。

绳子、梯子与平台使用 `Tools > 2026Test > 路径机关 > 显示傻瓜式 Scene 工具`。摄像机视角切换继续使用独立的 CameraModes 接口；两类工具不依赖旧关卡拼合系统。

## 角色边界

| 角色 | 主要工作 |
| --- | --- |
| 玩法策划 | 玩法规则、参数、验收和自己 Scene 中的少量搭建 |
| 策划程序 | 可配置玩法组件、Prefab、编辑器工具和策划接入支持 |
| 关卡策划 | 自己 Scene 的灰盒、路线、节奏、绳梯平台与环境摆放 |
| 程序 | 公共系统、运行时 API、测试；也可按固定 SHA 打包 |
| TA | Shader、材质、VFX Prefab 和独立效果测试 Scene |
| 程序 + TA / 整合者 | Git 集成、公共架构、管线、最终验证和发布 |

2D 美术与音乐音效暂不直接操作 Git，由指定 Git 成员在独立资源分支导入运行时文件。

## 标准任务流程

1. 整合者给出目标、分支名、允许/禁止路径、验收条件和测试方式。
2. 成员从最新 `origin/main` 创建独立任务分支，只修改任务范围。
3. Unity 中等待编译，检查 Console，执行功能测试和目标 Scene Smoke Test。
4. 检查 diff，提交并只推送自己的任务分支。
5. 交接完整 SHA、主要文件、Scene 路径、验证结果和风险。
6. 整合者在独立集成分支处理冲突、测试和合并，再更新 `main`。

禁止 merge/rebase/force-push 成员分支，禁止把 Library、Temp、Logs、Builds 或本机设置提交到仓库。

## Prefab 与公共资产

- 程序交付组件和基础 Prefab；策划优先用 Inspector 或 Variant 配置。
- 不 Unpack 公共 Prefab 来制造隐藏分叉。
- 同一 Prefab、Data、ProjectSettings 或管线资产仍需单一负责人；Scene 独立并不意味着公共文件可以并发改。
- Lighting、NavMesh、Occlusion、Build Settings 和全局 Volume 由整合者统一处理。

## 构建

- 整合者指定一个已推送到 `origin/main` 的完整 SHA。
- 构建者使用干净工作区并确认 `HEAD` 与该 SHA 一致。
- 不从个人分支、未提交状态或临时集成分支制作对外包。
- 记录 Unity 版本、平台、SHA、Development Build 状态、输出位置与 Smoke Test 结果。
- 主程可制作开发包或正式候选包；正式版本仍由整合者验收。

## 冲突与事故

- 如果两人误改同一 Scene，立即停止保存，各自提交并推送，交给整合者选择基线或人工重做；不要自动合并复杂 Unity YAML。
- 如果推送被拒绝、分支分叉或出现未知改动，不强推、不丢弃，按 Skill 报告。
- Unity 打开后出现大量无关变更时不要提交，记录版本、Scene 和 `git status` 后交给整合者判断。
