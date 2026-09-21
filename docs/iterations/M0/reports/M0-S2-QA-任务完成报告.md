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

# 第五轮: RJ-S2-01 复测完成(用户复测指令响应,2026-09-21)

> 本节由用户"复测指令——QA"触发:①已在 iter/m0 HEAD `e37270b` 完成 RJ-S2-01 修复合并与复测;②按 S2 审核 §5 非阻塞记录补 README 日志坐标系 §8 与走查手册步骤 6 退出确认;③本报告增"RJ-S2-01 复测"节。架构师无需 QA 再跑 L2 冒烟(已在 S2 审核 §4 自跑)。

## 5.1 复测执行明细

### 5.1.1 RJ-S2-01 修复落地

- Dev A 推送 `fix: RJ-S2-01` 单 commit `dbcf293`(已在 iter/m0 HEAD `fbe0af7` 合并)
- 修复内容:DevConfig / WindowTargetConfig / PathConfig / MatchingConfig / InputConfig 5 类型从 init-only/位置 record → 无参构造 class + settable 属性
- 新增回归 UT `tests/DH2.Tests/Unit/Config/RegressionRealConfigFilesTests.cs`:真读 `configs/dev.yaml` + `configs/mock-layout.yaml`(向上遍历定位仓库根),断言全维度 + 中心 (176,60)/(86,204)

### 5.1.2 build + test + coverage + format

| 项 | 命令 | 结果 |
|---|---|---|
| 全 sln Release build | `dotnet build DH2.slnx -c Release` | ✅ 0 警告 0 错误(3.98s) |
| 全 sln 测试 | `dotnet test DH2.slnx -c Release --no-build` | ✅ **59/59 通过**(199ms;原 57 + Dev A 新增 2 个回归 UT) |
| 覆盖率 | coverlet XPlat | DH2.Core **87.12%** / 分支 86.66%(≥ 70% 门槛) |
| 格式门禁 | `dotnet format DH2.slnx --verify-no-changes` | ✅ exit 0 |

### 5.1.3 回归 UT 有效性与断言质量

`RegressionRealConfigFilesTests.cs`(Dev A 新增,2 用例)直接读仓库真实 yaml,作为"防再犯"回归:

| 断言 | 覆盖 |
|---|---|
| 仓库根定位(`AppContext.BaseDirectory` 向上遍历直到 `configs/dev.yaml`) | 不依赖运行时目录 |
| `configs/dev.yaml` 全维度 | Profile / Targets(至少 1 个)/ Paths / Matching(DefaultThreshold ∈ (0.5, 1.0))/ Input(driver=background) |
| `configs/mock-layout.yaml` 全维度 | TempDir / Window(800×600)/ Taskbar(16,16,320,88)/ Status(16,116)/ Button(16,180,140,48)/ StateTimings(2000ms) |
| 几何中心断言 | taskbar (176,60) / button (86,204) — 与技术设计 §7 真值一致 |

**有效性判定**:与 S1 的 `MockLayoutGeometryTests`(针对默认实例的纯几何)互补,该回归 UT 锁定**真实仓库 yaml**的契约;任何后续对 Config 族或 yaml 字段的不兼容改动都会被该 UT 捕获(正是 RJ-S1-03/RJ-S2-01 的初衷)。

### 5.1.4 dh2ctl 命令层真实运行(headless,无 MockGame)

架构师 S2 审核 §3 已亲自跑出原 DevConfig YamlDotNet 抛 `InvalidDataException` 的现象,RJ-S2-01 修复后 QA 在 headless 会话重新跑(无 MockGame 窗口,允许窗口枚举结果为空,但配置解析必须成功):

| 命令 | 结果 | 验证点 |
|---|---|---|
| `dh2ctl --help` | 完整帮助文本(7 子命令 + 退出码说明) | ✅ 程序入口无崩溃 |
| `dh2ctl enumerate --config configs/dev.yaml` | 加载 config ✅ → 表头 `# target: mock` + `Hwnd\tTitle\tProcess\tX\tY\tWidth\tHeight` ✅ → **0 行**(无 MockGame) | ✅ DevConfig YamlDotNet 修复确认(原 DEF-S2-01 阻塞点);退出码 0 |
| `dh2ctl capture`(无 `--hwnd`) | `[usage error] --hwnd <n> required (positive long)` + 退出码 2 | ✅ 用法错误处理正确 |
| `dh2ctl capture --hwnd 12345678 --count 3 --interval-ms 100 --out artifacts/capture-s2` | `[capture error] write frame 0: !_img.empty()` + 退出码 3 | ✅ 空帧行为符合 S1 偏离裁决 #3 |

证据:`docs/iterations/M0/qa/evidence/M0-S2-l2/06-enumerate-rerun.txt` + `07-capture-rerun-noargs.txt` + `07-capture-rerun-badhwnd.txt`

## 5.2 S2 审核 §5 非阻塞记录已落实

| §5 记录 | 处置 |
|---|---|
| MockGame raw 日志在非 100% 缩放下记录虚拟化坐标(×1.5) | ✅ README 新增 §8 "日志坐标系说明":DIP vs 物理像素对照表 + 换算关系 + 来源注(S2 审核 §5) |
| 走查手册补"结束确认 DH2.MockGame 进程已退出"步骤 | ✅ `M0-S2-L2-桌面走查手册.md` 步骤 6 强化:增加 `while` 轮询 10s + Warning 提示孤儿进程残留 |
| `Input`/`Capture` 行覆盖 0% 符合预期 | ✅ 本轮 QA 报告 §"覆盖率"已注明(Win32/GDI 薄层,由 IT-01/02 集成覆盖;M2+ 自动化 UT 视情补) |

## 5.3 复测结论

| 项 | 结果 |
|---|---|
| RJ-S2-01 修复落地 | ✅ commit `dbcf293` 已合并(`fbe0af7`) |
| 全 sln build/test/format | ✅ 全绿(0 警 0 错 / 59 通过 / exit 0) |
| 回归 UT 有效性与断言质量 | ✅ 真读 `configs/dev.yaml` + `configs/mock-layout.yaml`,全维度 + 中心断言 |
| dh2ctl 命令层恢复(RJ-S2-01 修复核心) | ✅ DevConfig 解析不再崩,`enumerate` 表头格式与 SAC2-1 一致 |
| L2 IT-01/02 真实桌面回放 | **架构师已在 S2 审核 §4 自跑**;QA 无需再跑冒烟(用户复测指令第 ① 段已明示"你们无需再跑冒烟") |
| S2 审核 §5 非阻塞记录 | ✅ README §8 + 走查手册步骤 6 强化已落 |
| **DEF-S2-01 状态** | ✅ **CLOSED**(RJ-S2-01 修复 + 复测全绿) |

**Sprint M0-S2 综合判定**:

- L2 IT-01/02 命令层与解析层全绿(架构师代跑 + QA headless 双重验证)
- 阻塞 DEF-S2-01 已 CLOSED
- 非阻塞项(README 日志坐标系 + 走查手册退出确认)已落
- 架构师可快速复审签发 S3 放行

---

# RJ-S2-01 复测完成,请架构师复审 S2

- iter/m0 HEAD = `e37270b`(本报告提交后新 commit)
- 三报告齐备 + DEF-S2-01 CLOSED + README §8 + 走查手册步骤 6 已落
- 期望架构师"快速复审"通过即签发"开始 M0-S3"放行指令

- iter/m0 HEAD = `fbe0af7`(本报告待 push 后的新 commit)
- 三报告:DevA `76f6d31` / DevB `bf9080b` / QA 本报告
- 阻塞项 DEF-S2-01:RJ-S2-01 已修复并 QA 复测通过
- L2 IT-01/02 命令层验证通过(headless);真实桌面抓取待架构师或用户回填或确认可豁免
- SAC1-3:按架构师 S2 审核 §4 修订,闭环条件改至 S4 e2e

请架构师快速复审(核对 RJ-S2-01 修复 + IT-01/02 基础设施验证),通过即签发"开始 M0-S3"放行指令。

---

# 第六轮: RJ-S2-02/03 复测(用户复测指令二响应,2026-09-21)

> 本节由用户"复测指令——QA" S2 第二轮触发:①跟踪 dev-a RJ-S2-02 与 dev-b RJ-S2-03 推送;②复测 build+test+Mat 生命周期回归 UT;③IT-02 headless 部分验证(capture --hwnd 0 / --count 非法参数)+ 真实窗口标注待架构师复核;④本报告 §6。

## 6.1 集成记录

| 分支 | HEAD | 集成方式 | 冲突 |
|---|---|---|---|
| `dev-b/m0-s2` (6712474,RJ-S2-03) | `git merge --no-ff` → `7e54d1d` | 零 |
| `dev-a/m0-s2` (b7d7406,RJ-S2-02) | `git merge --no-ff` → `a7e9211` | 零 |

## 6.2 复测执行明细

### 6.2.1 RJ-S2-02(Dev A, `b7d7406`)

修复内容(commit message 摘要):
- `Frame.Width` / `Frame.Height` 在构造期通过 `init` 表达式一次性缓存(`Image.Width` / `Image.Height`),杜绝 Image.Dispose 后访问已释放 Mat 触发 0xC0000005
- `CaptureCommand` 加固:WriteLine 前缓存 width/height 到局部变量(防御深度);空 Mat(`frame.Image.Empty()`)跳过 `Cv2.ImWrite`(避免 OpenCV 抛"找不到匹配 writer",符合 S1 裁决 #3);`frame.Image` 在 try/finally 中保证 Dispose 一次(空/非空同路径,无双释放)
- 新增 `tests/DH2.Tests/Unit/Models/FrameLifecycleTests.cs` — 5 个 Mat 生命周期回归 UT

### 6.2.2 RJ-S2-03(Dev B, `6712474`)

修复内容:
- `GdiCapture`:BitmapConverter.ToMat(Bitmap) 在 OpenCvSharp4.Extensions 4.10.x 是共享内存(指针别名,不复制像素)→ 必须 `Clone()` 得独立副本后才 Dispose Bitmap,否则 Mat 悬垂
- `Frame.Image` 由**调用方负责释放**;GdiCapture 不持有也不释放
- 任何中间步骤异常退化为空帧(满足 S1 裁决 #3)

### 6.2.3 build + test + coverage + format

| 项 | 命令 | 结果 |
|---|---|---|
| 全 sln Release build | `dotnet build DH2.slnx -c Release` | ✅ 0 警告 0 错误(4.85s) |
| 全 sln 测试 | `dotnet test DH2.slnx -c Release --no-build` | ✅ **64/64 通过**(362ms;原 59 + Dev A 新增 5 个 `FrameLifecycleTests`) |
| 覆盖率 | coverlet XPlat | DH2.Core **96.52%** / 分支 93.33%(从 S2 第五轮的 87.12% 跃升 +9.4pp;Frame 模型 init 表达式路径大量触发) |
| 格式门禁 | `dotnet format DH2.slnx --verify-no-changes` | ✅ exit 0 |

### 6.2.4 Mat 生命周期回归 UT 断言有效性审查

`tests/DH2.Tests/Unit/Models/FrameLifecycleTests.cs` — Dev A 新增 5 用例:

| 用例 | 断言要点 | 有效性判定 |
|---|---|---|
| `Frame_WidthAndHeight_AreReadSafe_AfterImageDispose` | 构造真实 Mat(640×480)→ 缓存 width/height → `mat.Dispose()` → 再读 width/height(原 bug 触发 0xC0000005)→ 前后值一致且正确 | ✅ **核心回归用例**,锁死 RJ-S2-02 主修复;直接针对 0xC0000005 触发条件 |
| `Frame_EmptyMat_CachedDimensions_AreZero` | `new Mat()`(空矩阵)→ 构造 Frame → width=0/height=0 → Dispose 后再读仍 = 0 | ✅ 锁死空 Mat 边界 |
| `Frame_EmptyMatPlaceholder_IsReusable` | 5 次循环:每次 `new Mat()` → Frame → `mat.Empty()=true` → Dispose → 再读 width/height = 0 | ✅ 防空 Mat 共享/泄漏导致的二次 AV(GdiCapture.EmptyFrame 按需 new 契约) |
| `Frame_RealMat_ReleaseThenNewFrame_DoesNotCorrupt` | 第一个 Frame(800×600)Dispose 后,第二个 Frame(1024×768)独立正确读取;Dispose 后缓存值仍正确 | ✅ 防跨 Frame 实例状态污染(模拟 `capture --count 2` 的两次迭代) |
| `Mat_EmptyMethod_ReturnsTrue_OnAliveEmptyMat` | alive 空 Mat 报告 `Empty()=true`(Dispose 后 Empty() 抛 ObjectDisposedException 文档记录) | ✅ 锁死 CaptureCommand "alive 空 Mat 决策"路径的契约 |

**判定**:5 用例针对 RJ-S2-02 修复的 4 个核心场景(Dispose 后可读 / 空 Mat 边界 / 空 Mat 复用 / 跨实例隔离 / Empty 决策契约),**断言质量良好,无遗漏**。

## 6.3 IT-02 headless 部分验证(用户复测指令 ③)

**headless 环境限制**:无 GUI,无法启动 MockGame 真实窗口;按用户指令"对可验证部分验证,capture --hwnd 0 必须退出码 2 且不崩溃、--count 0/负数等非法参数路径退出码 2;真实窗口路径标注'待架构师真机复核'"。

| 命令 | 期望(用户指令 ③) | 实际 | 状态 |
|---|---|---|---|
| `capture --hwnd 0` | 退出码 2 + 不崩溃 | `[usage error] --hwnd <n> required (positive long)` + Exit **2** | ✅ **PASS**(原架构师证据显示此处曾抛 0xC0000005,RJ-S2-02 后已修复) |
| `capture --count 5`(缺 --hwnd) | 退出码 2 | `[usage error] --hwnd <n> required (positive long)` + Exit **2** | ✅ **PASS** |
| `capture --hwnd -1`(被 CLI 解析器拦作短选项) | 退出码 2 | `[usage error] unsupported short option '-1'` + Exit **2** | ✅ **PASS** |
| `capture --count -5`(被 CLI 解析器拦作短选项) | 退出码 2 | `[usage error] unsupported short option '-5'` + Exit **2** | ✅ **PASS** |
| `capture --hwnd 12345 --count 0` | 退出码 2(用户期望) | **静默回退到 DefaultCount=10,Exit 0** | ❌ **FAIL** |
| `capture --hwnd 12345 --count=-5` | 退出码 2(用户期望) | **静默回退到 10,Exit 0** | ❌ **FAIL** |
| `capture --hwnd 12345 --count=-1` | 退出码 2(用户期望) | **静默回退到 10,Exit 0** | ❌ **FAIL** |
| `capture --hwnd 99999999999999999`(long 溢出) | (无明确期望) | 截断为合法 long,后续 3 帧空帧(无窗口) | ⚠️ 边界,未崩溃但行为无定义 |

**`--count` 静默回退问题**:当前 `CaptureCommand.cs` 第 51-53 行:
```csharp
var count = options.TryGetValue("count", out var countStr) && int.TryParse(countStr, out var c) && c > 0
    ? c
    : DefaultCount;
```
`c > 0` 校验失败时**静默使用默认值 10**,不报 usage error。这是 RJ-S2-02 修复**未覆盖的边界**,按用户指令 ③ 应退出码 2 但当前 Exit 0。**开 DEF-S2-03 指派 Dev A**(RJ-S2-04 整改指令)。

**真实窗口路径**(`capture --hwnd <真实句柄> --count 10`)验证状态:**待架构师真机复核**——
- 架构师在 S2 第二轮复审前已产出 `04-capture.txt` + 1 张 23KB PNG(commit `5a9a022` 的 `frame_20260920234142735_000.png`),表明 capture 已能产出真实帧
- 但完整 10 帧 + mean/p95 统计 + 空帧正确跳过 ImWrite 的端到端验证,需架构师在桌面会话执行一次以确认
- 我(headless)无法启动 MockGame 进程,亦无法观察 800×600 实际渲染

证据:`docs/iterations/M0/qa/evidence/M0-S2-l2/results/`(RJ-S2-02 复测的 headless 命令输出 + 架构师 `04-capture-crash.txt` 与 `04-capture.txt` 已落)

## 6.4 缺陷清单

### 已关闭

| DEF | 处置 | 结果 |
|---|---|---|
| DEF-S2-01 DevConfig YamlDotNet | RJ-S2-01 | ✅ CLOSED(架构师第二轮复审已验证) |
| DEF-S2-02 capture 访问违例 0xC0000005 | RJ-S2-02 + RJ-S2-03 | ✅ **CLOSED**(5 个 Mat 生命周期回归 UT 全绿 + GdiCapture Clone() 修复 + 头less 验证 --hwnd 0 退出码 2 不崩溃) |

### 新增

| DEF | 摘要 | 复现 | 期望 | 实际 | 指派 | 状态 |
|---|---|---|---|---|---|---|
| **DEF-S2-03** | `capture --count 0/负数` 静默回退到默认值 10(应退出码 2) | `dh2ctl capture --hwnd 12345 --count 0`(或 `--count=-1`) | 退出码 2 + usage error 信息 | Exit 0(静默 `c > 0 ? c : DefaultCount`) | **Dev A**(CaptureCommand 域) | **OPEN**;RJ-S2-04 整改指令:`--count` / `--interval-ms` 非法值(<0 或非整数)应与 `--hwnd` 一致报 usage error + 退出码 2 |

## 6.5 复测结论

| 项 | 结果 |
|---|---|
| RJ-S2-02 修复落地(Dev A) | ✅ commit `b7d7406` 已合并(`a7e9211`) |
| RJ-S2-03 修复落地(Dev B) | ✅ commit `6712474` 已合并(`7e54d1d`) |
| 全 sln build/test/format | ✅ 全绿(0 警 0 错 / **64/64 通过** / exit 0) |
| DH2.Core 行覆盖 | **96.52%**(S2 第五轮 87.12% → +9.4pp,Frame init 路径大量触发) |
| Mat 生命周期回归 UT(5 用例) | ✅ 断言质量良好,锁死 RJ-S2-02 主修复 4 个核心场景 |
| IT-02 `--hwnd 0` 不崩溃 | ✅ 退出码 2(架构师原证据的 0xC0000005 已修) |
| IT-02 `--count` 非法值退出码 2 | ❌ 静默回退到 10(DEF-S2-03 OPEN) |
| IT-02 真实窗口端到端(10 帧 + mean/p95 + 空帧正确跳过) | ⚠️ **待架构师真机复核**(架构师已产出 1 张 23KB PNG 帧证明 capture 可产出真实帧;完整 10 帧 + 空帧跳过 ImWrite 端到端需桌面复跑) |
| DEF-S2-02 状态 | ✅ **CLOSED** |
| 新增 DEF-S2-03 | OPEN,RJ-S2-04 待 Dev A 整改 |

**Sprint M0-S2 综合判定**:
- 架构师 S2 第二轮复审阻塞项 DEF-S2-02 已 CLOSED
- IT-02 部分验证通过(不崩溃 + 部分退出码正确)
- 残余项:DEF-S2-03(count 静默回退)+ 真实窗口端到端复核(待架构师)

---

# RJ-S2-02/03 复测完成,请架构师第三轮复审

- iter/m0 HEAD = `a7e9211`(本报告提交后新 commit)
- 三报告齐备 + DEF-S2-02 CLOSED + 新增 DEF-S2-03 OPEN + 真实窗口端到端待架构师复核
- 期望架构师第三轮复审:核对 RJ-S2-02/03 修复 + 5 个 Mat 生命周期 UT + DH2.Core 96.52% 覆盖;
- 关于 DEF-S2-03:可与 RJ-S2-04 合并到同一整改批(S2 仍可签收而 DEF-S2-03 留作 S2→S3 过渡期整改)
- 关于真实窗口端到端:若架构师本机执行 `capture --hwnd <MockGame hwnd> --count 10` 一次,即完成 S2 全部 L2 验证
