# 2026Test 团队协作总流程

## 1. 目标与当前决定

本项目采用以下协作模型：

```text
策划写目标与验收
        ↓
策划程序 / 程序拆成可实现任务
        ↓
每人从 main 建立短期任务分支
        ↓
各自在明确拥有的文件中工作
        ↓
推送任务分支并交接给整合者
        ↓
整合者检查、合并、运行、打包
        ↓
通过后更新 main
```

固定规则：

- `main` 是唯一稳定集成分支，任何成员都不得直接推送。
- 除整合者外，所有 Git 参与者必须使用 `$unity-git-branch-worker`。
- 一个任务一个短期分支，不建立长期“某某人的分支”。
- 一个 Unity 场景、Prefab 或大型配置资产，在同一时间只有一个明确拥有者。
- `main` 应始终保持可打开、可编译，并尽量保持可运行。
- 最终合并和冲突处理只由整合者完成。开发包或正式包可以由整合者或主程执行，但必须从整合者指定的 `main` 提交构建并留下记录。

当前项目已确认：

- Unity 版本：`6000.3.12f1`。
- `Visible Meta Files` 已启用。
- `Force Text` 已启用。
- 使用 GitHub 仓库 `miao1suki/2026Test`。

任何人不得自行升级 Unity、修改 Packages、切换渲染管线或批量重序列化项目。

## 2. 团队角色与责任边界

| 角色 | 主要产出 | 默认拥有范围 | 必须避开的范围 |
| --- | --- | --- | --- |
| 玩法策划 | 玩法说明、规则、参数、验收条件、少量玩法对象摆放 | `Docs/Design`、被分配的 Level/Piece/Face 数据块、独立数据资产 | 代码、基础 Prefab、ProjectSettings、渲染配置 |
| 策划程序 | 把设计变成可配置组件、玩法 Prefab、关卡逻辑和编辑器工具 | Gameplay 功能目录、被分配的关卡数据块、玩法 Prefab/Data | 核心底层、他人的关卡数据块、全局管线配置 |
| 关卡策划 | 灰盒、动线、空间、遭遇节奏、关卡静态摆放 | 被分配的 Level/Piece/Face/Content 块、关卡专用 Prefab Variant | 核心代码、全局配置、生成预览和 Release 场景 |
| 程序 | 大部分核心系统、公共模块、运行时框架、测试；按安排执行项目构建 | `Code/Core`、`Code/Systems`、`Development/Tests`、系统 Sandbox | Release 关卡场景、全局渲染设置、无关美术资产 |
| TA | Shader、材质、VFX Prefab、效果测试场景、效果参数 | `Rendering/Shaders`、`Rendering/Materials`、`Prefabs/VFX`、系统 Sandbox | Renderer Asset、Render Pipeline Asset、Packages、未经批准的 Render Feature |
| 程序 + TA / 整合者 | 主分支、集成、公共架构、管线、构建、最终质量 | `main`、Bootstrap/Master、ProjectSettings、Packages、Rendering/Settings、Build | 不替成员在同一任务分支上并发修改 |

两名 2D 像素美术和一名音乐音效暂不直接操作 Git，由指定的 Git 成员代为导入运行时资源。

## 3. 推荐项目文件结构

后续重构时使用下列结构。迁移必须作为独立任务由整合者完成，不要让多人同时移动旧资源。

```text
Assets/
  _Project/
    Art/
      Runtime/
        Sprites/
        Textures/
      UI/
    Audio/
      Runtime/
        Music/
        SFX/
      Mixers/
    Code/
      Core/
        Runtime/
        Editor/
        Tests/
      Systems/
        FeatureName/
          Runtime/
          Editor/
          Tests/
      Gameplay/
        FeatureName/
    Content/
      Data/
        Global/
        Gameplay/
        Levels/
    Prefabs/
      Core/
      Gameplay/
      Environment/
      UI/
      VFX/
    Rendering/
      Shaders/
      Materials/
      VFX/
      VolumeProfiles/
      Settings/
    Development/
      Levels/
        LV001/
          Authoring/
            LV001_AuthoringDefinition.asset
            Pieces/
              Piece_01/
                Front/Geometry.asset
                Front/Traversal.asset
                Right/...
                Back/...
                Left/...
          Preview/Scenes/
          Sandbox/
      Sandbox/
        Systems/
          CameraModes/
      Tests/
    Release/
      Levels/
        LV001/
          Scenes/
            LV001_Main.unity
            LV001_GeneratedMap3D.unity
          Data/
    Scenes/
      Bootstrap/        迁移期保留的公共入口
      Levels/           迁移期旧关卡，只读收编后再归档
    ThirdParty/
Docs/
  Design/
  Collaboration/
```

结构规则：

- 所有自研内容进入 `Assets/_Project`，第三方包集中在 `ThirdParty`，不混入自研目录。
- 每个系统使用自己的功能目录，脚本、编辑器扩展和测试靠近该系统。
- 不创建一个所有人都要编辑的巨大 `GameBalance.asset`。参数按功能或关卡拆成独立 Data 资产。
- 运行时 Prefab 与关卡场景分离。程序交付“组件 + 基础 Prefab”，策划通过 Inspector 配置。
- 关卡特有改动优先使用 Prefab Variant，不直接修改公共基础 Prefab。
- Sandbox 按关卡或系统命名，不按职位、姓名建目录；只用于验证，不进入 Build Settings，也不能成为正式内容唯一来源。
- `Development/Levels/<LevelId>/Authoring` 是关卡唯一源数据；Preview 可重新生成。
- `Release` 只保存验证后生成的发布场景，不在其中日常搭关卡。

## 4. 场景拆分与所有权

### 4.1 关卡数据职责

每个正式关卡以“小型源数据块 → 生成预览 → Release 场景”组织：

| 数据 | 内容 | 默认负责方式 |
| --- | --- | --- |
| `Piece_xx/<Face>/Geometry.asset` | 网格物品、地面、墙体、静态结构 | 按关卡空间块分配 |
| `Piece_xx/<Face>/Traversal.asset` | 绳子、梯子、平台与稳定连接 ID | 按关卡空间块分配 |
| `Preview/Scenes/*` | 2D 总拼、3D 折叠可写投影视图 | 工具生成；不作为合并源 |
| `Release/.../LVxxx_Main` | 系统入口与生成地图加载器 | 整合者 |
| `Release/.../LVxxx_GeneratedMap3D` | 验证后的发布关卡实例 | 整合者发布生成 |

不要为了职位拆出 Design、Logic、Layout、TA 等目录。需要多人并行时，按 Level、Piece、Face、Geometry/Traversal 划分；对象与其直接连接数据尽量处于同一块。

### 4.2 跨场景引用

不要依赖从一个 Additive Scene 中的对象直接拖拽引用另一个场景的对象。跨层通信使用以下方式之一：

- 公共 Runtime Service；
- 事件或 ScriptableObject Event Channel；
- 稳定 ID，在场景加载后注册和查找；
- 由 `Master` 或加载器在运行时注入依赖。

策划不应自行发明跨场景引用方案。需要引用时交给策划程序或程序提供可复用组件。

### 4.3 数据块单一编辑负责人制度

本阶段不使用 Scene Fusion、对象锁定或文件锁定工具。GitHub 不会自动阻止两个人同时保存同一个 `.asset`、`.unity` 或 Prefab，因此每个关卡数据块、Prefab 或大型 Data 在一个任务周期内指定一名编辑负责人。

1. 任务分配时列出允许修改的场景、Prefab 和 Data 文件。
2. 整合者在团队置顶消息或任务表中登记负责人、分支和预计交接时间。
3. 只有编辑负责人可以打开并保存这些文件；其他人可以查看，但不得保存。
4. 负责人推送并交接后，在整合者完成合并前仍不安排第二个人编辑。
5. 整合者宣布“已合并，可以接手”后，下一个人才能从新 `main` 开始编辑。

预约表格式：

```text
文件：Assets/_Project/Development/Levels/LV001/Authoring/Pieces/Piece_02/Right/Traversal.asset
拥有者：level-designer
分支：agent/level-designer/lv001-blockout
状态：编辑中 / 已推送待合并 / 可以接手
开始：YYYY-MM-DD HH:mm
预计交接：YYYY-MM-DD HH:mm
```

如果需要别人帮忙：

- 帮忙者制作独立 Prefab、材质、脚本或 Data，不打开被预约的场景；或
- 原负责人先保存、提交、推送并完成交接，整合者合并后再换人接手。

禁止通过聊天说“我应该没动多少”后同时保存同一场景。

### 4.4 场景保存前检查

- 是否只编辑并保存了任务允许的关卡数据块或测试场景？
- Hierarchy 是否只出现任务相关变化？
- 是否无意改动 Lighting、NavMesh、Occlusion、Volume 或全局设置？
- 是否出现大批没有原因的 YAML 变化？
- 新资源是否包含对应 `.meta`？
- 是否误将 Sandbox 场景加入 Build Settings？

Lighting Bake、NavMesh Bake、Occlusion Bake、Build Settings 和全局 Volume 最终生成由整合者在合并后统一执行。

## 5. Prefab 和 Data 的并行协作

场景只是“组装结果”，可复用功能应落在独立资产中：

- 程序：编写 MonoBehaviour、Service、接口和自动测试。
- 策划程序：把程序组件组装为可配置玩法 Prefab，并暴露策划需要的参数。
- 玩法策划：调整独立 Data 或 Prefab Variant，不修改基础实现。
- 关卡策划：把 Prefab 实例收编到分配的 Geometry/Traversal 数据块，避免 Unpack。
- TA：交付 VFX Prefab、材质实例和 Shader，场景内只放实例。
- 整合者：处理公共 Prefab、全局引用和渲染管线接入。

同一个 Prefab 文件仍然遵守独占预约。需要多人并行时，拆成父子 Prefab 或 Variant，而不是同时修改同一个文件。

## 6. 标准任务生命周期

### 6.1 分配

整合者发出的任务必须包含：

- 任务编号和一句话目标；
- 负责人和角色；
- 指定分支名；
- 允许修改的路径；
- 禁止修改的路径；
- 已预约的场景/Prefab；
- 输入依赖；
- 可验证的验收标准；
- 需要执行的测试。

没有路径范围和验收条件的任务不能开始。

### 6.2 成员执行

成员把完整任务交给 AI，并明确要求使用 `$unity-git-branch-worker`。成员只在自己的独立克隆中工作，完成后推送任务分支并按 Skill 格式交接。

任务分支建议控制在半天到两天内。超过两天仍未完成时应拆分，不要让分支长期脱离 `main`。

### 6.3 整合

整合者不直接在成员分支继续开发。推荐流程：

1. 拉取所有远端分支并确认 `main` 干净。
2. 从 `main` 建短期 `integration/<milestone-or-date>` 分支。
3. 阅读成员交接信息，检查提交范围和异常大文件。
4. 按顺序集成：代码与测试 → Prefab/Data → 关卡 Authoring 块 → 全局设置与烘焙数据。
5. 在 Unity 中等待完整导入和编译，处理 Console 错误。
6. 运行相关功能测试和目标场景 Smoke Test。
7. 通过关卡创作管线重新生成 2D/3D 预览，验证稳定 ID 与连接关系，然后发布 Release 场景。
8. 需要时重新生成 Lighting/NavMesh/Build Settings；Build Settings 只能启用 Release 关卡场景。
9. 生成一次开发构建并运行。
10. 全部通过后更新 `main`，再通知成员拉取新基线。

任何场景冲突都由整合者处理。不要让成员为了“解决冲突”自行 merge/rebase 后强推。

### 6.4 完成定义

一项任务只有满足以下条件才算完成：

- 任务分支已推送；
- 工作树干净；
- 只修改允许路径；
- Unity 无新增编译错误；
- 验收条件逐项验证；
- 已提供完整 SHA、主要文件、测试结果和风险；
- 场景/Prefab 的下一位编辑负责人已由整合者确认。

## 7. AI 辅助开发规则

每个人的 AI 提示必须提供：角色、任务、分支、允许路径、禁止路径、验收条件和测试方式。

AI 必须遵守：

- 开始前读取 `$unity-git-branch-worker`。
- 修改前执行 `git status`，确认所在分支和未知改动。
- 不自行扩大任务范围。
- 不手工生成或修改 GUID。
- 不把 `.unity`、`.prefab`、`.asset`、`.meta` 当普通文本做大范围替换。
- 不自行升级包、修改 ProjectSettings、Renderer Asset、Render Pipeline Asset 或 Build Settings。
- 不运行会丢弃他人改动的 Git 命令。
- 代码任务必须至少完成编译级检查；能运行测试时必须运行。
- 结束时列出所有修改、验证结果和未解决风险。

成员本人必须阅读 AI 的 diff 和 Unity Console。AI 说“完成”不等于任务已经通过。

## 8. 非 Git 美术与音频交付

两名 2D 像素美术和音乐音效使用统一的外部交付区，不直接把工程源文件塞入 Unity 仓库。

建议目录：

```text
AssetDrop/
  YYYY-MM-DD_TaskName/
    Source/       PSD、ASEPRITE、DAW 工程、分轨等
    Runtime/      准备导入 Unity 的 PNG、WAV、OGG 等
    MANIFEST.md   用途、版本、作者、导入要求
```

文件命名建议：

- 像素 Sprite：`SPR_Feature_Name_V01.png`
- 普通贴图：`TEX_Feature_Name_V01.png`
- 音效：`SFX_Feature_Action_V01.wav`
- 音乐：`MUS_Scene_Mood_V01.wav`

导入流程：

1. 美术/音频把 `Source` 和 `Runtime` 放到共享云盘的任务目录。
2. 指定 Git 成员核对清单，在自己的资源导入分支中导入 `Runtime` 文件。
3. 在 Unity 中设置 Sprite、压缩、循环、采样率等导入参数。
4. 检查 `.meta`，生成预览场景或 Prefab，并推送分支。
5. 整合者验证构建体积和运行效果后合并。

原始 PSD、ASEPRITE、DAW 工程和大型分轨默认留在外部资产库。仓库只保存游戏运行所需文件。若运行时二进制资源持续增长，再统一启用 Git LFS；不要由个人临时配置。

## 9. 每日协作节奏

建议每天至少两个集成窗口：午间一次、收工前一次。

```text
开始工作：整合者发布最新 main 与文件负责人表
    ↓
成员拉取 main 并创建任务分支
    ↓
成员独立开发，必要时只提交小型中间版本
    ↓
集成窗口前推送并交接
    ↓
整合者集中合并、运行和反馈
    ↓
通过后更新 main，宣布文件可以由下一位成员接手
```

紧急修复也从最新 `main` 建独立分支，不允许直接改 `main`。

## 10. 冲突与事故处理

### 发现两人同时改了同一场景

双方立即停止继续保存，分别提交并推送自己的当前分支，然后报告：场景名、分支、SHA、各自改了哪些对象。整合者选择：

- 先合并一方，另一方把独立对象做成 Prefab/Data 后重新应用；
- 使用 UnityYAMLMerge 尝试语义合并，并在 Unity 中逐对象验证；
- 选择一方场景为基线，由整合者手动重做另一方的小量改动。

不得让 AI 直接“自动修好”复杂场景冲突后未经 Unity 验证就提交。

### 推送被拒绝或分支分叉

成员按 `$unity-git-branch-worker` 停止并报告，不 force push。由整合者判断远端提交归属。

### Unity 打开后出现大批无关变更

不要提交、不要批量恢复。关闭 Unity，记录 Unity 版本、打开过的场景和 `git status`，交给整合者判断。

## 11. 可选工具升级

当前优先使用“分层场景 + 单一编辑负责人”，成本最低，也最适合现阶段项目规模。

如果以后确实需要多人同时进入同一场景，可评估 Scene Fusion 一类实时场景协作工具。但即使采用：

- 仍保留场景分层和文件拥有者；
- 一次会话只指定一人提交最终场景；
- 新增贴图、模型、音频仍通过版本控制；
- 实时同步不能替代 Git、备份和整合测试。

当大型二进制源文件进入仓库、同文件并发修改频繁发生或 Git 历史膨胀时，再整体评估 Git LFS、Unity Version Control 或 Perforce。不要同时使用两个主版本库。

## 12. 构建权限与构建基线

整合者和主程都可以打包，但不能各自从不同工作状态随意出包。

每次构建必须满足：

- 整合者先指定一个已推送到 `origin/main` 的完整提交 SHA；
- 构建者使用干净工作区，确认 `HEAD` 与指定 SHA 完全一致；
- 不从个人任务分支、未提交改动或尚未合并的 integration 分支生成对外包；
- 使用约定的 Unity 版本、平台和构建选项；
- 包名或归档目录包含版本、平台和短 SHA；
- 构建后填写构建记录，并把结果交给整合者验收；
- 只有整合者确认的包可以标记为团队正式版本。

主程可以自行生成用于程序验证的开发包，但也必须记录来源 SHA，并明确标注为 `Development`，不能与正式包混淆。
