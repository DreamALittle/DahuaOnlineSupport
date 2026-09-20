# M0-S1 集成冲突上报表(QA → 架构师)

- 测试 Agent / 日期:qa-agent / 2026-09-20
- iter/m0 本地 commit:`4e3b240`(已 fast-forward 至 origin/iter/m0)
- 状态:**未执行 dev-a/m0-s1 合并**;iter/m0 当前仅含 Dev B 的 S1-3

## 1. 冲突摘要

`dev-a/m0-s1` 推送时,**14 个 MockGame 实现文件**会被 merge 自动删除,因为它们仅存在于 `origin/iter/m0`(Dev B 已合并),且 dev-a 的分支把这些路径标注为删除。

```
merge-base = ee011da  ← Dev A 与 Dev B 均从此 commit 拉分支,无人 rebase 协调
```

## 2. 关键差异(导致合并不可执行)

| 维度 | Dev A(dev-a/m0-s1) | Dev B(已在 iter/m0) | 冲突性质 |
|---|---|---|---|
| MockGame Avalonia 版本 | **11.2.7** | **12.1.2** | API 不兼容,合并后 B 的代码用 12 API 编译失败 |
| MockGame 项目引用 | `ProjectReference` → `src/DH2.Core` | **无**(MockGame 自读 `configs/mock-layout.yaml`) | 引用方向 + MockLayout 归属冲突 |
| MockLayout 实现位置 | `src/DH2.Core/Config/MockLayout*.cs` | `tools/DH2.MockGame/Models/MockLayout*.cs` | **重复定义**,编译期符号冲突 |
| MockGame Assets 资源 | csproj 中**无** `<AvaloniaResource Include="Assets\**" />` | 含,且 `avalonia-logo.ico` 已入仓 | 资源丢失(175875 字节 → 0 字节) |
| MockGame 实现文件 | **全部删除**(Models/Services/ViewModels/Views 共 12 文件) | **全部保留** | 逻辑删 — 功能丢失 |
| MockGame App.xaml/.cs/Program.cs | 最小骨架(注释明言"等 Dev B 在 S1-3 替换") | 完整接线 | 内容冲突但骨架被显式期望替换 |

Dev A 的 csproj 注释(`<!-- 实现:由 Dev B 在 S1-3 实施 -->`)显式表明:Dev A 的策略是"先建空壳 + 等 B 填肉"。**协作契约成立,但缺乏 rebase 机制**,导致双方并行产出互斥。

## 3. 推荐的解决路径(供架构师裁决)

### 路径 A(推荐):Dev A 在自己的分支 rebase 至 iter/m0 并重整 MockGame

Dev A 执行:
```bash
cd D:\Repos\DH2-DevA
git fetch origin
git rebase origin/iter/m0
# 解决 rebase 冲突:接受 Dev B 的 MockGame 实现,放弃 Dev A 的 MockGame 槽改动
# 关键保留:Dev A 的 DH2.slnx、src/、tests/、configs/dev.yaml、README、.editorconfig、.gitignore、Directory.Build.props
# 关键删除/回滚:tools/DH2.MockGame/ 的所有改动(App.axaml/.cs、Program.cs、csproj、app.manifest、MainWindow.axaml/.cs、csproj 中关于 Avalonia 11/ProjectReference 的改动)
# 关键调整:DH2.MockGame.csproj 改用 Avalonia 12.1.2 + AvaloniaResource Assets\** + 无 ProjectReference (保持与 Dev B 一致)
git push --force-with-lease origin dev-a/m0-s1
```

之后 QA 合并:零冲突,所有 Stories 可一次集成。

### 路径 B:Dev A 不动,QA 做"外科手术式合并"

QA 不做。违反 07 §2「逻辑冲突退回对应开发处理」红线。

### 路径 C:双分支回炉,Dev A 重建

昂贵。不推荐。

## 4. 当前保护措施

- `iter/m0` 本地与远端保持 `4e3b240`,**未含 dev-a 任何内容**
- `dev-a/m0-s1` 远端保留,Dev A 可在收到指令后 push --force-with-lease 重写
- 工作区干净,无半成品合并状态
- 我对 dev-a/m0-s1 的报告(`M0-S1-DevA-任务完成报告.md`)尚未读 — 需在决策方向明确后再读,以免对后续审查造成先入为主

## 5. 已知影响(待架构师裁决后填)

- SAC1-1 全 sln Release 构建:暂未执行(S1-1 + MockGame 同时到位才能跑);架构师如决定走路径 A,Dev A rebase 后 QA 立即可跑
- UT-01/02/04/06/08:依赖 `src/DH2.Core/` 的 8 个新文件,dev-a 已全部就位;一旦 dev-a rebase 落地可立即编码执行
- 覆盖率门槛 ≥70%(DH2.Core 纯逻辑):dev-a 已完成所有可测纯逻辑代码,等 rebase 后跑 coverlet 即可
- SAC1-3 MockGame 手动冒烟:仍受 headless 会话限制,本轮任何状态下都无法执行,如实记录"未执行-需真桌面"

## 6. QA 建议:架构师在审核报告的「整改项」可考虑纳入

- 流程整改:开发 Agent 在 S1-1 推送(sln 骨架)前,**不得**对他人域的占位项目做"骨架"提交;或建立 "MockGame 是 Dev B 独占域,Dev A 不写 csproj 改动" 的明确分工
- 工具整改:测试 Agent 应在 dev 分支推送时**自动检测 merge-base 是否等同 iter/m0 HEAD**;若不等,提示"请 rebase 后再推"(本轮手工做,流程化放后续)

## 7. 我现在做什么

- 不合并 dev-a/m0-s1(避免自动删除 14 个 MockGame 文件)
- 不写最终 QA 完成报告(待 dev-a 决策落地后完成)
- cron self `M0-S1-dev-push-watch` 保留;若 Dev A 在我观察之外 push --force-with-lease 重写 dev-a/m0-s1,我会自动进入合并与测试流程
- 等待架构师/用户指令:路径 A / B / C + 是否需要 QA 主动联系 Dev A