# SPRINT M0-S2 任务完成报告(QA)

- 测试Agent:qa-agent / 2026-09-20
- iter/m0 commit:`7debe7a`(本报告未提交前的最新本地 commit;报告本身会再增一笔)
- 环境:.NET SDK 10.0.103 / Windows 10.0.26200 x64 / **PowerShell(headless 会话)**
- 本报告提交位置:`docs/iterations/M0/reports/M0-S2-QA-任务完成报告.md`

## 集成记录

### 合并的两条 dev 分支

| 分支 | HEAD SHA | 集成方式 | 冲突 |
|---|---|---|---|
| `dev-b/m0-s2` | `bf9080b`(含 aef0af5 RJ-S1-04 + f4a7fa4 S2-1 + f139855 S2-2) | `git merge --no-ff origin/dev-b/m0-s2` → commit `a6021af` | **零冲突** |
| `dev-a/m0-s2` | `76f6d31`(含 e8dc2db RJ-S1-03 + 66d3189 RJ-S1-05 + 79196a0 S2-3 CLI + 3c1b7d9 merge Dev B + 5e8463a S2-4 enumerate/capture) | `git merge --no-ff origin/dev-a/m0-s2` → commit `7debe7a` | **零冲突**(Dev A 已在本地 merge Dev B,QA 二次 merge 是补完形式合入,无新增内容) |

### 冲突处理记录

- **本轮无逻辑冲突、无机械冲突**。
- S1 期间发现的逻辑冲突(MockGame 归属 + Avalonia 版本)已在 RJ-M0-S1 裁决书闭环,S2 未再触发新冲突。

### 三份完成报告齐备性(架构师收口前提)

| 报告 | 路径 | 当前状态 |
|---|---|---|
| `M0-S2-DevA-任务完成报告.md` | `docs/iterations/M0/reports/M0-S2-DevA-任务完成报告.md` | ✅ 已随 Dev A 合并进入 iter/m0(commit `76f6d31`) |
| `M0-S2-DevB-任务完成报告.md` | `docs/iterations/M0/reports/M0-S2-DevB-任务完成报告.md` | ✅ 已随 Dev B 合并进入 iter/m0(commit `bf9080b`) |
| `M0-S2-QA-任务完成报告.md` | 即本文件 | ⏳ 本提交后进入 iter/m0 |
| `M0-S2-架构师审核报告.md` | `docs/iterations/M0/reports/` | ⏳ 架构师收口产出(待三报告齐备后) |

**齐备性结论**:本提交落盘后,三份角色报告全部齐备,可触发架构师集中审查。

## RJ-S1-03/05 修复复测(S2 放行指令第 ③ 条)

### RJ-S1-03(MockLayout 族 YamlDotNet 修复)

**修复内容**(commit `e8dc2db`):`MockRect` / `MockWindowConfig` / `MockStateTimings` / `MockStatusPosition` / `MockButtonConfig` / `MockTaskbarConfig` 全部从 `sealed record` → `sealed class`(无参构造 + 位置构造);`MockLayoutConfig` 扩展承载 Dev B 真实 yaml 字段;`Directory.Packages.props` 启用 CPM,YamlDotNet 统一 16.3.0。

**QA 复测**:
- `tests/DH2.Tests/Unit/Config/MockLayoutGeometryTests.cs` 已**恢复真实 YAML 解析期望**(S1 期间因 bug 注释掉的用例重新启用)
- `Parse_ValidYaml_ReturnsAllSections`:✅ PASS(MockWindowConfig.Title / Width / Height、Taskbar/Button 几何、StateTimings.PathfindingMs、TempDir 全部正确)
- `Parse_MinimalYaml_AppliesAllDefaults`:✅ PASS(空 `{}` 返回带默认值的实例)
- `Parse_UnknownProperty_IsIgnored`:✅ PASS(配置演进容错)
- `Load_FromExistingRepoFile_ReturnsConfig`:✅ PASS(加载仓库内 `configs/mock-layout.yaml`)
- **MockLayoutLoader 行覆盖从 S1 的 26.7% 升至 73.3%**(由 UT 触发)
- **DH2.Core 总行覆盖从 S1 的 79.24% 升至 87.12%**

### RJ-S1-05(ConfigValidator foreground 分支可达 + 消息严格化)

**修复内容**(commit `66d3189`):`SupportedDrivers` HashSet 增列 `foreground`;消息严格化为 `"foreground driver not implemented in M0; use 'background'"`(技术设计 §2.3)。

**QA 复测**:
- `tests/DH2.Tests/Unit/Config/DevConfigValidationTests.cs` 中 `Validate_DriverForeground_AddsNotImplementedError` 已**恢复原期望**(S1 期间放宽到"接受任一消息"以保留 SAC 通过,本轮还原严格断言)
- ✅ PASS(`e.Path.Contains("Driver") && e.Message.Contains("not implemented") && e.Message.Contains("M0")`)
- **ConfigValidator 行覆盖从 S1 的 94.9% 升至 100%**(56/59 → 61/61)

## 用例执行矩阵

### L1 单元测试(全部通过)

| 用例 | 来源 | 结果 | 证据路径 |
|---|---|---|---|
| **UT-01** `Win32Coord.ToLParam/FromLParam`(9 用例) | `docs/iterations/M0/ITER-M0-测试设计.md §1` | ✅ **PASS** | `tests/DH2.Tests/Unit/Util/Win32CoordTests.cs` + `docs/iterations/M0/qa/evidence/M0-S2-l2\01-test.log` |
| **UT-02** `Win32Coord.ClientToScreen`(4 用例) | 同上 | ✅ **PASS** | 同上 |
| **UT-04** `DevConfig` 校验(11 用例,含 RJ-S1-05 复测) | 同上 | ✅ **PASS** | `tests/DH2.Tests/Unit/Config/DevConfigValidationTests.cs` |
| **UT-06** `mock-layout` 解析与几何(11 用例,含 RJ-S1-03 复测:真实 YAML 解析 4 用例 + 边界 3 用例 + 几何 4 用例) | 同上 | ✅ **PASS** | `tests/DH2.Tests/Unit/Config/MockLayoutGeometryTests.cs` |
| **UT-08** `Polling.WaitUntilAsync`(8 用例) | 同上 | ✅ **PASS** | `tests/DH2.Tests/Unit/Util/PollingTests.cs` |
| 覆盖补强:Models/Contracts + ConfigValidator null-section | (支撑测试) | ✅ **PASS** | `tests/DH2.Tests/Unit/Models/ModelAndContractSmokeTests.cs` |

**测试总计**:6 测试类 / **57 用例 / 全绿 / 0 失败 / 0 跳过**(用时 212ms)
**DH2.sln 全量**:`dotnet build -c Release` 0 警告 0 错误(7 项目)
**格式门禁**:`dotnet format DH2.slnx --verify-no-changes` exit 0

### L2 模拟窗口测试(SAC1-3 闭环条件 + IT-01/IT-02)

**【硬条件】本会话仍为 headless**(PowerShell,无 GUI 显示)。架构师在 S1 审核报告 §4 与 S2 放行指令第 ② 条明确要求:

> "S2 审核若 SAC1-3 未闭环,则 S2 不通过"

> "若你的会话仍无 GUI,请向用户说明并由用户在桌面会话代跑,严禁静默跳过"

**处置**:

✅ 已生成桌面走查手册:`docs/iterations/M0/qa/evidence/M0-S2-l2/M0-S2-L2-桌面走查手册.md`(8473B),用户按手册在交互桌面会话执行 7 步骤,产出证据落 `docs\iterations\M0\qa\evidence\M0-S2-l2\results\`。

⏳ **用户回填结果后,本表由 QA 读取并填入**:

| 用例 | 期望 | 状态 | 证据路径 |
|---|---|---|---|
| **IT-01** `dh2ctl enumerate` 列出 MockGame 窗口恰好 1 条(800×600) | ⏳ 待用户回填 | ⏳ | `results\03-enumerate.txt` |
| **IT-02** `dh2ctl capture --count 10` 输出 10 张 PNG,非黑帧,耗时统计 | ⏳ 待用户回填 | ⏳ | `results\04-capture.txt` + `results\04-capture-pngs\*.png` |
| **SAC1-3** MockGame 手动冒烟:点"前往"→ Pathfinding → Arrived + 双路日志(`\|raw\|` + `\|ui\|` 同行)→ 点"返回"→ Idle + counter+1 | ⏳ 待用户回填 | ⏳ | `results\05{abc}-*.{json,log}` |

> ⚠️ **本报告不伪造任何 L2 结果**。用户回填后,QA 会:
> 1. 读取 `docs\iterations\M0\qa\evidence\M0-S2-l2\results\` 下所有证据文件
> 2. 在本表填入真实结果
> 3. 若任一用例 FAIL 或异常 → **开新 DEF**,不静默通过
> 4. 提交 commit `qa: M0-S2 L2 证据落地 + 报告补完` 并 push iter/m0
> 5. 真正发出"M0-S2 三报告齐备,请架构师收口审查"

## 覆盖率(纯逻辑类行覆盖)

测试设计 §1 + 03 §4 G4 口径:**DH2.Core 全部 + Vision 纯逻辑类行覆盖 ≥70%**

### DH2.Core 覆盖明细(coverlet 6.0.4,XPlat Code Coverage)

```
package-level:line-rate=0.8712 branch-rate=0.8666
```

**✅ 87.12% 行覆盖**(S1 的 79.24% → S2 的 87.12%,+7.88pp;RJ-S1-03 修复后 MockLayoutLoader 反序列化路径可达,新增 4 用例)

| 类 | 行覆盖 | 命中/总 | 备注 |
|---|---|---|---|
| `DH2.Core.Util.Polling` | 94.4% | 17/18 | — |
| `DH2.Core.Util.Win32Coord` | 100% | 11/11 | — |
| `DH2.Core.Models.ActionResult` | 100% | 1/1 | — |
| `DH2.Core.Models.Frame` | 0% | 0/3 | 仍需 `Mat`(S3 UT-07 金样本补) |
| `DH2.Core.Models.MatchResult` | 100% | 2/2 | — |
| `DH2.Core.Models.Point` | 100% | 1/1 | — |
| `DH2.Core.Models.Rect` | 100% | 2/2 | — |
| `DH2.Core.Models.Size` | 100% | 1/1 | — |
| `DH2.Core.Models.Win32Window` | 100% | 1/1 | — |
| `DH2.Core.Contracts.TemplateEntry` | 100% | 8/8 | — |
| `DH2.Core.Config.ConfigError` | 100% | 1/1 | — |
| `DH2.Core.Config.ConfigLoader` | 0% | 0/15 | DevConfig YAML 加载(技术设计 §6)尚未编 UT;**列入 S3 起始补强** |
| `DH2.Core.Config.ConfigValidator` | **100%** | **61/61** | **RJ-S1-05 修复后全分支覆盖** |
| `DH2.Core.Config.DevConfig` | 100% | 5/5 | — |
| `DH2.Core.Config.MockLayoutConfig` | 100% | 6/6 | RJ-S1-03 修复后 |
| `DH2.Core.Config.MockLayoutLoader` | **73.3%** | 11/15 | S1 的 26.7% → S2 的 73.3%(+46.6pp;剩余 4 行在异常分支) |
| 其他 Config records | 100% | — | MockRect/MockWindowConfig/MockButtonConfig/... |

### 其他项目覆盖

| 项目 | 行覆盖 | 备注 |
|---|---|---|
| `DH2.Input` | 0% | Win32 API 调用,纯单测难以覆盖;S3 IT-01 走集成路径 |
| `DH2.Capture` | 0% | GDI 截屏需真实窗口;S2 IT-02 走集成路径 |
| `DH2.MockGame` | 0% | Avalonia GUI,需桌面会话(SAC1-3 闭环补) |
| `dh2ctl`(DH2.App) | 0% | CLI 行为由 IT-01/IT-02 验证 |
| `DH2.Vision` | 100% | 空骨架(空类),100% 是巧合 |

**总体结论**:**DH2.Core 87.12% 行 / 86.66% 分支**,≥70% 门槛(SAC2 G4 严格通过)

## 缺陷清单

### S1 遗留缺陷处置

| DEF | S1 状态 | S2 处置 | 结果 |
|---|---|---|---|
| **DEF-S1-01** MockLayout 族 YamlDotNet 反序列化缺陷 + dead code | OPEN(RJ-S1-03 修复中) | ✅ **CLOSED** — RJ-S1-03 已落地:record → class + 字段扩展 + YamlDotNet 16.3.0;CPM 启用;UT-06 真实 YAML 解析 PASS;MockLayoutLoader 覆盖 26.7% → 73.3% |
| **DEF-S1-02** ConfigValidator foreground 分支不可达 + 消息误导 | OPEN(RJ-S1-05 修复中) | ✅ **CLOSED** — RJ-S1-05 已落地:SupportedDrivers 增列 foreground;消息严格化为技术设计 §2.3;UT-04 恢复原期望 PASS;ConfigValidator 覆盖 94.9% → 100% |

### S2 新增缺陷

| DEF | 摘要 | 复现步骤 | 期望 | 实际 | 证据 | 指派 | 状态 |
|---|---|---|---|---|---|---|---|
| (无) | S2 内未发现新缺陷;L2 相关缺陷将由用户桌面回填结果决定 | — | — | — | — | — | — |

## 资产生成记录(模板/金样本,如有)

**M0-S2 范围无资产生成任务**(S3 才需生成模板与金样本)。

仅维护:

- `tests/DH2.Tests/` 新增 / 恢复 UT-04 与 UT-06 的严格期望(M0-S2 共 6 文件,57 用例)
- `Directory.Packages.props`(Dev A 新增,启用 CPM,QA 不改)
- `docs/iterations/M0/qa/evidence/M0-S2-l2/`:
  - `00-build.log`(1282B)
  - `01-test.log`(1908B)
  - `02-coverage.cobertura.xml`(191KB,<200KB 限制)
  - `03-format.log`(1020B)
  - `M0-S2-L2-桌面走查手册.md`(8473B)
  - `results/`(待用户回填)

## 环境与未执行项

### 已就绪(headless 可达)

- ✅ 全 sln Release build:0 警告 0 错误
- ✅ 57 UT 全绿
- ✅ DH2.Core 行覆盖 87.12% ≥ 70% 门槛
- ✅ Format 门禁通过
- ✅ RJ-S1-03 / RJ-S1-05 复测 PASS(UT 严格期望)
- ✅ DEF-S1-01 / DEF-S1-02 CLOSED

### 未执行(需用户桌面会话)

- ❌ **IT-01 `dh2ctl enumerate`** — 用户在桌面会话代跑
- ❌ **IT-02 `dh2ctl capture --count 10`** — 用户在桌面会话代跑
- ❌ **SAC1-3 MockGame 走查**(架构师 S1 审核报告 §4 明确绑定 S2 闭环条件) — 用户在桌面会话代跑
- ❌ **DH2.Core.Config.ConfigLoader UT**(15 行) — 列入 S3 起始补强
- ❌ **DH2.Core.Models.Frame UT**(3 行,需 Mat) — 列入 S3 UT-07 金样本识别时补

## 结论

| SAC | 状态 | 说明 |
|---|---|---|
| **SAC2-1** IT-01:MockGame 运行时 `dh2ctl enumerate` 恰好列出 1 条 | ⏳ 待用户桌面回填 | 实现层 PASS(Dev A 报告 + Dev B 报告均交付);自动化层待回填 |
| **SAC2-2** IT-02:连续 10 帧截图尺寸正确、非黑帧、输出均值/p95 | ⏳ 待用户桌面回填 | 实现层 PASS(Dev A 报告交付);自动化层待回填 |
| **SAC2-3** CLI 行为:非法参数退出码 2;配置校验失败聚合输出 + 退出码 3;`--config` 缺省 `configs/dev.yaml` | ✅ **PASS** | 静态代码核对 + build 验证;Dev A 自检表与 QA 复跑 build 一致 |
| **SAC2-4** build/test/format 全绿,无越界实现 | ✅ **PASS** | `artifacts\build-s2-final.log` + `artifacts\test-s2-strict.log` + `artifacts\format-s2.log` |
| **SAC1-3**(S1 遗留闭环)MockGame 手动冒烟 | ⏳ 待用户桌面回填 | **S1 审核报告 §4 + S2 放行指令硬条件** |

**Sprint M0-S2 SAC 综合判定**:

- **可自动化部分(SAC2-3/2-4 + RJ-S1-03/05 复测 + DEF-S1-01/02 关闭)全部 PASS**
- **L2 部分(SAC2-1/2-2 与 SAC1-3 闭环)由用户桌面回填决定**
- **本报告不强行声明 L2 PASS**;用户回填后,QA 二次补完报告 + push 后,方触发"三报告齐备"声明

**S2 收口**:**L2 三项用例全部 PASS → 三报告齐备 → 架构师收口**

---

> ⚠️ **当前状态**:L2 三项用例待用户在桌面会话执行并回填证据。QA Agent 收到用户回填后:
> 1. 读取 `docs\iterations\M0\qa\evidence\M0-S2-l2\results\` 所有证据
> 2. 在本报告"用例执行矩阵 / L2"小节填入真实结果
> 3. 若发现 FAIL / 异常 → 开新 DEF,不静默通过
> 4. 二次 commit `qa: M0-S2 L2 证据落地 + 报告补完` 并 push iter/m0
> 5. 真正发出"M0-S2 三报告齐备,请架构师收口审查"
>
> 在那之前,**绝不开始 M0-S3 的任何工作**。
>
> ⚠️ **本报告当前 commit 仅为"待 L2 回填"骨架**——S2 的架构师收口条件严格绑定 SAC1-3 闭环 + IT-01/IT-02 真实回填。
> 若用户在 24 小时内未回填,QA 会主动提示用户,而非在无证据下声明完成。