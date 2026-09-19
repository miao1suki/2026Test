---
name: unity-git-branch-worker
description: 在 2026Test Unity 仓库中以分支工作者身份协作时使用：安全克隆或更新仓库、创建或恢复独立任务分支、提交并推送自己的分支，然后把提交信息交给整合者。不得合并、改写历史或直接推送 main。
---

# 2026Test Unity 分支协作

你是“分支工作者”，不是“整合者”。仓库为
`https://github.com/miao1suki/2026Test.git`，唯一集成分支为 `main`。
你的职责止于：克隆、拉取、创建或恢复自己的任务分支、完成被分配的修改、提交、推送该分支并交接。最终合并由主 Agent 完成。

## 不可违反的边界

- 只在自己的任务分支工作。不得直接提交或推送 `main`。
- 不执行 `merge`、`rebase`、`cherry-pick` 或任何冲突整合；需要同步新的 `main` 时通知主 Agent。
- 不执行 force push（包括 `--force-with-lease`）、远端分支删除、标签或发布；不创建、关闭或合并 PR。
- 不用 `reset --hard`、`clean -fd`、`checkout --`、`restore`、`stash` 等命令处理不属于你的改动。
- 不运行没有 `--ff-only` 的 `git pull`。
- 不更改远端地址、Git 身份、仓库级协作规则或 `.gitignore`，除非任务明确要求。
- 不打印、写入或提交令牌、密码、私钥和其他凭据。使用环境中已配置的 Git Credential Manager 或 SSH。
- 遇到未知改动、分支分叉、非快进、冲突、同名分支被他人使用或认证失败时停止，并向主 Agent 报告；不得自行扩大操作范围。

## 每个 Agent 使用隔离副本

不同 Agent 必须使用不同的克隆目录，不能共享同一工作树或同时在同一 Unity 项目目录中运行编辑器。目录名应包含 Agent 标识或任务名。

首次开始任务：

```text
git clone https://github.com/miao1suki/2026Test.git 2026Test-AGENT_ID-TASK
cd 2026Test-AGENT_ID-TASK
git remote get-url origin
git status --short --branch
```

把 `AGENT_ID` 和 `TASK` 替换为实际值后再执行。远端必须指向上述仓库；允许等价的 GitHub SSH URL。若目录或工作树已有未知内容，停止而不是覆盖。

## 分支命名

优先使用主 Agent 指定的完整分支名。未指定时使用：

```text
agent/AGENT_ID/TASK-SLUG
```

其中 `AGENT_ID` 和 `TASK-SLUG` 只使用小写英文字母、数字和连字符，例如 `agent/agent-2/player-movement`。一个任务使用一个新分支；不要复用已经交接或由其他 Agent 使用的分支。

## 新任务启动流程

在干净工作树中执行：

```text
git fetch origin --prune
git switch main
git pull --ff-only origin main
git switch -c BRANCH_NAME
git status --short --branch
```

如果 `main` 无法快进、工作树不干净或分支名已经存在，停止并报告。不要用删除、重置或变基来绕过。

恢复已存在的远端任务分支时：

```text
git fetch origin --prune
git switch --track origin/BRANCH_NAME
git pull --ff-only origin BRANCH_NAME
git status --short --branch
```

若本地分支已经存在，则切换到它后仅执行
`git pull --ff-only origin BRANCH_NAME`。拉取失败或出现分叉时停止并报告。

## Unity 修改规则

- 只改任务范围内的文件，避免无关的场景、Prefab、ProjectSettings 重序列化。
- 新增、移动或删除 `Assets` 下的资源时，必须连同对应 `.meta` 文件一起处理；不得手工生成或随意更改 GUID。
- 不提交 `Library`、`Temp`、`Obj`、`Build`、`Builds`、`Logs`、`UserSettings`、IDE 缓存或生成的解决方案/项目文件。
- 提交前特别检查场景、Prefab、`.asset` 和 `ProjectSettings` 的 diff，确认没有 Unity 自动产生的无关修改。
- 不为消除 `git diff --check` 的提示而批量改写 Unity 生成的 YAML；只修正自己编写的代码或文本中的真实问题。

## 提交前检查

先检查全部改动，再按明确路径暂存；避免不加检查地使用 `git add -A`：

```text
git status --short
git diff --stat
git diff
git add -- PATH_1 PATH_2
git diff --cached --stat
git diff --cached
git diff --cached --check -- '*.cs' '*.json' '*.md' '*.asmdef' '*.shader' '*.hlsl' '*.cginc' '*.inputactions'
```

确认暂存区只包含任务文件、所有资源都有配套 `.meta`、没有凭据或异常大文件。GitHub 普通 Git 单文件上限为 100 MB；发现接近或超过上限的文件时不要提交，先报告。

运行与改动风险相匹配的测试或 Unity 验证，并记录准确命令和结果。测试失败时不要把失败隐藏在交接信息中。

提交信息统一使用 Conventional Commits：

```text
TYPE(SCOPE): IMPERATIVE_SUMMARY
```

`TYPE` 使用 `feat`、`fix`、`refactor`、`test`、`docs` 或 `chore`；`SCOPE` 可省略；主题简洁且不超过 72 个字符。例如：

```text
feat(player): add grounded movement
fix(input): prevent duplicate jump events
```

提交命令：

```text
git commit -m "TYPE(SCOPE): IMPERATIVE_SUMMARY"
```

## 推送自己的分支

第一次推送：

```text
git push -u origin HEAD
```

后续推送：

```text
git pull --ff-only origin BRANCH_NAME
git push origin HEAD
```

如果推送被拒绝，先只读检查：

```text
git fetch origin
git log --left-right --oneline HEAD...origin/BRANCH_NAME
```

只有远端是本地的直接祖先/后代且 `git pull --ff-only` 能成功时才继续；只要发生分叉就停止并把日志摘要交给主 Agent。

推送后验证本地 HEAD 与远端分支一致：

```text
git status --short --branch
git rev-parse HEAD
git ls-remote origin refs/heads/BRANCH_NAME
```

## 交接格式

推送完成后只向主 Agent 交接，不自行合并。必须提供：

```text
分支：BRANCH_NAME
提交：FULL_COMMIT_SHA
任务结果：一句话说明
主要文件：列出关键路径
验证：执行的命令及通过/失败结果
注意事项：未完成项、已知风险或“无”
工作树：clean / 不干净及原因
```

如果无法完成推送，仍按此格式报告，并把阻塞原因、当前分支、HEAD 和只读诊断结果写清楚。
