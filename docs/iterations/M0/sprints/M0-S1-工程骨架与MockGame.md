# SPRINT M0-S1:工程骨架与 MockGame v0

| 项 | 内容 |
|---|---|
| 目标 | 可构建的解决方案骨架 + DH2.Core 基础模型与纯逻辑(含单测)+ MockGame v0 可运行并被观测 |
| 依赖 | 无(首个 Sprint) |
| 预估 | 1 天 |

## Stories

| ID | Story | 技术设计引用 | 测试引用 |
|----|-------|--------------|----------|
| S1-1 | 解决方案骨架:DH2.sln + 6+1 项目(Core/Input/Capture/Vision/App/MockGame/Tests),TFM `net10.0-windows`,TreatWarningsAsErrors,引用方向按 02 §3;`.gitignore`/`.editorconfig`/根 README(构建与运行说明);git 分支 `iter/m0` 建立并小步提交 | 技术设计 §1 | — |
| S1-2 | DH2.Core:基础模型(Point/Size/Rect/Win32Window/Frame/MatchResult/ActionResult)、契约接口(IWindowLocator/IFrameCapture/ITemplateMatcher/ITemplateStore/TemplateEntry)、配置实体与校验(DevConfig 族,YamlDotNet 绑定)、Win32Coord、Polling.WaitUntilAsync | 技术设计 §2 | UT-01、UT-02、UT-04、UT-06、UT-08 |
| S1-3 | MockGame v0(Avalonia):按 mock-layout.yaml 渲染(任务栏/状态文本/主按钮)、状态机(Idle→Pathfinding→Arrived→Idle,计数)、state.json 原子写、SetWindowSubclass 原始消息日志 + Pointer 语义日志双路 | 技术设计 §7、§8 | 手动冒烟清单(见 SAC1-3) |

## SAC(Sprint 验收标准,Review 逐条核对)

- [ ] SAC1-1 `dotnet build -c Release` 全解决方案零警告零错误;项目引用方向与 02 §3 一致(无反向/跨层)
- [ ] SAC1-2 UT-01/02/04/06/08 全绿;纯逻辑类行覆盖 ≥70%
- [ ] SAC1-3 MockGame 手动冒烟:运行后点击"前往"→状态"寻路中..."(约 2s)→"已到达目的地"且按钮变"返回"→点击→回"待机"且计数+1;state.json 每次转移即时更新;messages.log 同时出现 `|raw|` 与 `|ui|` 行,raw 行坐标与点击位置一致(100% 缩放)
- [ ] SAC1-4 `iter/m0` 分支存在且包含本 Sprint 的小步约定式提交

## 范围外(严禁实施)

- 任何 Win32 窗口枚举/截屏实现(S2);模板匹配/manifest 加载实现(S3);PostMessage 点击/CLI 命令/e2e(S4)
- NativeMethods 中声明而未用的占位 API 允许,但不得编写调用逻辑

## Sprint 简报(开发 Agent 填写后,本卡附链接)

(链接到本目录 M0-S1-Sprint简报.md)

## 审核记录(架构师填写)

(待 Sprint Review)
