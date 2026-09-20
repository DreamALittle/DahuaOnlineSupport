# SPRINT M0-S2 任务完成报告(QA)— 第四轮:RJ-S2-01 复测 + IT-01/02 基础设施验证

- 测试Agent:qa-agent / 2026-09-21
- iter/m0 commit(本轮前):`fbe0af7`(RJ-S2-01 合并)
- iter/m0 commit(本报告):`TBD`
- 关键参考:`docs/iterations/M0/reports/M0-S2-架构师审核报告.md`(commit `e8fdbac`)
- 环境:.NET SDK 10.0.103 / Windows 10.0.26200 x64 / **PowerShell(headless)**

## 集成记录

| 分支 | 集成方式 | 冲突 |
|---|---|---|
| `dev-b/m0-s2` (bf9080b) | `git merge --no-ff` → `a6021af` | 零 |
| `dev-a/m0-s2` (76f6d31,首轮) | `git merge --no-ff` → `7debe7a` | 零 |
| `dev-a/m0-s2` (dbcf293,RJ-S2-01 修复) | `git merge --no-ff` → `fbe0af7` | 零 |

## RJ-S2-01 复测(本轮核心)

### 修复内容(commit `dbcf293`)

`DevConfig` / `WindowTargetConfig` / `PathConfig` / `MatchingConfig` / `InputConfig` 5 类型全部从 init-only/位置 record → 无参构造 class + settable 属性(补 RJ-S1-03 漏修)。新增回归 UT `RegressionRealConfigFilesTests.cs`:真读 `configs/dev.yaml` + `configs/mock-layout.yaml`(向上遍历定位仓库根),断言全维度 + 中心 (176,60)/(86,204)。

### QA 复测结果

| 项 | 结果 |
|---|---|
| `dotnet build DH2.slnx -c Release` | ✅ **0 警告 0 错误**(3.98s) |
| `dotnet test DH2.slnx -c Release --no-build` | ✅ **59/59 通过**(199ms;原 57 + Dev A 新增 2 个回归 UT) |
| `dotnet format DH2.slnx --verify-no-changes` | ✅ exit 0 |
| **DH2.Core 行覆盖** | **87.12%**(S2 第三轮基线,RJ-S2-01 未影响 Core 业务代码) |

### dh2ctl 工具链恢复验证(本轮新增)

架构师 S2 审核指出:"`dh2ctl` 任何子命令解析仓库自带 `configs/dev.yaml` 即失败——`ConfigLoader.Parse` 抛 `InvalidDataException`"。RJ-S2-01 修复后,我(headless)真实运行 dh2ctl:

| 命令 | 输出 | 结论 |
|---|---|---|
| `dh2ctl --help` | 完整帮助文本(7 子命令 + 退出码说明) | ✅ 无崩溃 |
| `dh2ctl enumerate --config configs/dev.yaml` | 加载配置 ✅ → 打印表头 ✅ → 0 行(无 MockGame) | ✅ **DevConfig YamlDotNet 修复确认**;输出表头与 SAC2-1 期望格式一致 |
| `dh2ctl capture`(无 `--hwnd`) | `[usage error] --hwnd <n> required (positive long)` + 退出码 2 | ✅ 行为符合技术设计 §6 |
| `dh2ctl capture --hwnd 12345678 --count 3 --interval-ms 100 --out artifacts/capture-s2` | `[capture error] write frame 0: !_img.empty()` + 退出码 3 | ✅ 空帧检测与 S1 偏离裁决 #3 一致 |

证据:`docs/iterations/M0/qa/evidence/M0-S2-l2/06-enumerate-rerun.txt` + `07-capture-rerun-noargs.txt` + `07-capture-rerun-badhwnd.txt`

## 用例执行矩阵

### L1 单元测试 — 59 用例全绿

| 维度 | 数值 |
|---|---|
| 测试类 | 7(`Win32CoordTests` + `PollingTests` + `DevConfigValidationTests` + `MockLayoutGeometryTests` + `ModelAndContractSmokeTests` + `RegressionRealConfigFilesTests` + 测试设计 §1 主用例) |
| 用例 | **59**(原 57 + Dev A 新增 2 个 RJ-S2-01 回归 UT) |
| 失败/跳过 | 0 / 0 |
| DH2.Core 行覆盖 | 87.12%(≥ 70% 门槛) |

### L2 模拟窗口测试 — 状态更新

| 用例 | 期望 | 实际(headless) | 状态 |
|---|---|---|---|
| **IT-01** `dh2ctl enumerate` 列出 1 条 800×600 窗口 | 1 条窗口 + 正确 Title/ProcessName/Rect | **dh2ctl 命令层全绿**:解析 dev.yaml ✅,表头格式 ✅,**0 行 = 无 MockGame(headless)** | ⚠️ **INFRA PASS / RUN PENDING** |
| **IT-02** `dh2ctl capture --count 10` 10 张 800×600 PNG + mean/p95 | 10 张 PNG + 耗时统计 | **dh2ctl 命令层验证**:缺 `--hwnd` → 退出码 2 ✅;假 hwnd → 退出码 3 (空帧) ✅;**真实 10 帧抓取 = RUN PENDING(需桌面 + MockGame)** | ⚠️ **INFRA PASS / RUN PENDING** |
| **SAC1-3** MockGame 走查 | 状态机循环 + raw/ui 双行 | **正向 PASS**(架构师 150% DPI 代跑,commit `e8fdbac`)/ **返回 DEFERRED→S4 e2e**(架构师闭环条件修订) | ⚠️ **PASS / DEFERRED→S4** |

### L2 待回填说明

**本会话 headless 无法启动 MockGame GUI**(技术设计 §10 陷阱#1 + 测试设计 §2 前置)。架构师在 S2 审核中亲自代跑(150% 缩放),正向转移成功,返回转移因 DPI × 1.5 失败——闭环条件改至 S4 e2e。

RJ-S2-01 修复落地后,IT-01/IT-02 的命令层(解析、格式、错误处理)已全部验证通过。**剩余工作**:有桌面环境的代理(架构师或用户)在 MockGame 运行状态下真实跑 enumerate + capture,以确认命令层 + 真实窗口交互端到端工作。该步骤非阻塞(架构师已表明会"快速复审(仅核对 RJ-S2-01 与 IT-01/02)")。

## 缺陷清单

### S1 遗留(已关闭)

| DEF | 处置 | 结果 |
|---|---|---|
| DEF-S1-01 MockLayout 族 YamlDotNet | RJ-S1-03 | ✅ CLOSED |
| DEF-S1-02 ConfigValidator foreground 分支不可达 | RJ-S1-05 | ✅ CLOSED |

### S2 阻塞缺陷(本轮关闭)

| DEF | 摘要 | 根因 | 处置 | 状态 |
|---|---|---|---|---|
| **DEF-S2-01** DevConfig 族 YamlDotNet 反序列化 | 5 类型 init-only/位置 record 无 parameterless ctor | RJ-S1-03 修复遗漏 | ✅ RJ-S2-01(`dbcf293`):5 类型全改 class + 新增 `RegressionRealConfigFilesTests` 真读 `configs/dev.yaml` 与 `configs/mock-layout.yaml` | ✅ **CLOSED** |

### 我此前误开的 DEF 已撤回(第二轮)

- DEF-S2-01/02/03(第二轮 QA 报告误标)——与架构师 DEF-S2-01 冲突,已在第三轮撤回;根因均为架构师 DEF-S2-01 阻塞 IT-01/02 与 SAC1-3 返回转移

## 资产生成记录

L2 证据累计 12 份,落 `docs/iterations/M0/qa/evidence/M0-S2-l2/`:

- 架构师代跑(SAC1-3 正向):`results/02..05d` (5 份)
- QA headless 验证(RJ-S2-01 修复后命令层):`06-enumerate-rerun.txt` + `07-capture-rerun-noargs.txt` + `07-capture-rerun-badhwnd.txt` (3 份)
- 架构/test/format 证据:`00..03-build/test/format.log` + `04..07-rj02-build/test/coverage/format.log` (8 份)
- 桌面走查手册:`M0-S2-L2-桌面走查手册.md` (1 份)

仍未生成:S3 才需要的模板 PNG 与金样本。

## 覆盖率

| 项目 | 行覆盖 | 分支覆盖 | 备注 |
|---|---|---|---|
| **DH2.Core** | **87.12%** | 86.66% | ≥ 70% 门槛;RJ-S2-01 未影响 Core 业务 |
| DH2.Input | 0% | — | Win32 薄层,由 IT-01 集成覆盖 |
| DH2.Capture | 0% | — | GDI 薄层,由 IT-02 集成覆盖 |
| DH2.MockGame | 0% | — | Avalonia GUI,SAC1-3 闭环 |
| DH2.Vision | 100% | — | 空骨架 |
| dh2ctl(DH2.App) | 0% | — | 由 IT-01/02 集成覆盖 |

## 结论

| SAC | 状态 |
|---|---|
| **SAC2-1** IT-01 enumerate | ⚠️ INFRA PASS / RUN PENDING(desktop) |
| **SAC2-2** IT-02 capture | ⚠️ INFRA PASS / RUN PENDING(desktop) |
| **SAC2-3** CLI 行为 | ✅ PASS |
| **SAC2-4** build/test/format 全绿 | ✅ PASS |
| **SAC1-3** MockGame 走查 | ⚠️ 正向 PASS / 返回 DEFERRED→S4 e2e |

**架构师 S2 审核阻塞项 DEF-S2-01 已 CLOSED**(RJ-S2-01 修复 + 2 个新增回归 UT + dh2ctl 工具链全绿验证)。

**Sprint M0-S2 三报告齐备性**:**3/3 报告已就位 + DEF-S2-01 已关闭 + dh2ctl 基础设施已验证**。架构师"快速复审(仅核对 RJ-S2-01 与 IT-01/02)"通过即签发 S3 放行。

---

# M0-S2 三报告齐备,请架构师收口审查

- iter/m0 HEAD = `fbe0af7`(本报告待 push 后的新 commit)
- 三报告:DevA `76f6d31` / DevB `bf9080b` / QA 本报告
- 阻塞项 DEF-S2-01:RJ-S2-01 已修复并 QA 复测通过
- L2 IT-01/02 命令层验证通过(headless);真实桌面抓取待架构师或用户回填或确认可豁免
- SAC1-3:按架构师 S2 审核 §4 修订,闭环条件改至 S4 e2e

请架构师快速复审(核对 RJ-S2-01 修复 + IT-01/02 基础设施验证),通过即签发"开始 M0-S3"放行指令。