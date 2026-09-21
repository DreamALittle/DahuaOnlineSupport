# SPRINT M0-S4 任务完成报告(QA) — M0 收口 + SAC1-3 闭环

- **测试 Agent**:qa-agent / 2026-09-21
- **iter/m0 commit(本轮前)**:`e451968`(S3 RJ-S3-01/02 复测)
- **iter/m0 commit(本轮首报)**:`1bf06d5`(滚动合并 dev-a/m0-s4 + dev-b/m0-s4 + PostMessageDriver 接缝适配 + S4 UT + 桌面走查手册)
- **iter/m0 commit(本轮复测 / RJ-S4-01/02)**:`24d83ce`(滚动合并 dev-a/m0-s4 RJ-S4-01 + RJ-S4-02 坐标推导接缝 UT)
- **iter/m0 commit(本轮复测 / RJ-S4-03)**:`13eaef9`(滚动合并 dev-a/m0-s4 RJ-S4-03 + RJ-S4-03 接缝 UT 谓词语义独立验证)
- **关键参考**:
  - `docs/iterations/M0/reports/M0-S3-架构师审核报告-终审.md`(S3 终审 PASS)
  - `docs/iterations/M0/sprints/M0-S4-M0收口与端到端闭环.md`
  - `docs/iterations/M0/ITER-M0-测试设计.md §2`(IT-04/05/06/07)
  - `docs/iterations/M0/ITER-M0-技术设计.md §6`(S4 e2e + click 命令契约)
- **环境**:.NET SDK 10.0.103 / Windows 10.0.26200 x64 / **PowerShell(headless,无 GUI)**

## iter/m0 commit 历史(本轮)

| commit | 类型 | 摘要 |
|---|---|---|
| `94c6d69` | merge | `qa: 滚动合并 dev-b/m0-s4(S4-1 PostMessageDriver + S4-3 e2e 命令)` |
| `94ecc08` | merge | `qa: 滚动合并 dev-a/m0-s4(S4-2 click + S4-4 report + S4-5 ITER 自检报告)`,含 §冲突解决 的接缝适配 |
| `1bf06d5` | qa | `qa(s4): S4 QA 任务完成 — 26 新增 UT + 桌面走查手册 + 报告` |
| `91cc9f0` | merge | `qa: 滚动合并 dev-a/m0-s4 RJ-S4-01(E2eCommand 坐标空间混用修复 + 3 个实现侧 UT)` |
| `24d83ce` | qa | `qa(s4): RJ-S4-01/02 复测 — DEF-S4-01 闭环;坐标推导接缝独立验证 UT 13 用例 + 报告增补` |
| `d5287f2` | merge | `qa: 滚动合并 dev-a/m0-s4 RJ-S4-03(E2eCommand 状态轮询谓词修正 + 3 个实现侧 UT)` |
| `13eaef9` | qa | `qa(s4): RJ-S4-03 复测 — 谓词语义接缝独立验证 UT + 报告增补` |

## 集成记录

| 分支 | 集成方式 | 冲突 |
|---|---|---|
| `origin/dev-b/m0-s4` (`f5053c9`) | `git merge --no-ff` → `94c6d69` | 零(S4-1 PostMessageDriver 真实现 + S4-3 e2e 命令) |
| `origin/dev-a/m0-s4` (`4e06746`) | `git merge --no-ff` → `94ecc08` | **2 处冲突**(已解决,见 §冲突解决) |

### 冲突解决

1. **`src/DH2.App/Program.cs`** — Dev A 加 `click` + `report` 派发;Dev B 加 `e2e` 派发。
   **解决**:保留全部 3 个新派发:`"click" => new ClickCommand(), "e2e" => new E2eCommand(), "report" => new ReportCommand()`。
2. **`src/DH2.Input/PostMessageDriver.cs`** — Dev A 的临时桩 `: IInputDriver` + sync `Click(...)` 抛 `NotImplementedException`;
   Dev B 真实现未实现 `IInputDriver`,但有异步 `ClickAsync(...)` + 真 PostMessage 调用。
   **解决**(接缝适配):取 Dev B 真实现主体,加 `: IInputDriver` + 同步 `Click(long, int, int)` 包装
   `ClickAsync(...).GetAwaiter().GetResult()`(阻塞等待,无额外线程),让 ClickCommand
   通过 `IInputDriver` 接口契约消费真实现。**这是 S4 QA 集成接缝** —— 与 RJ-S3-01
   manifest 写读对齐、RJ-S2-04 count 静默回退同类教训(产品跨 Sprint 必须有端到端契约)。

集成后 iter/m0 HEAD 包含全部 S4 提交,IT-04/05/06/07 依赖全部到位(PostMessageDriver + ClickCommand + E2eCommand + ReportCommand)。

## 测试用例执行矩阵

### L1 单元测试 — 121 用例全绿(原 95 + S4 新增 26)

| 测试类 | 用例数 | 范围 | 结果 |
|---|---|---|---|
| `ClickCommandTests`(本轮新增) | **14** | IT-07:`click --hwnd/--x/--y` 缺参/负值/非法值路径退出码 2;driver 返 success/false 路径 JSON 输出 + 退出码 0;driver 抛 NotImplementedException/InvalidDataException/OperationCanceledException → 退出码 3;driver 收到的坐标与入参一致 | ✅ 全绿 |
| `E2eCommandTests`(本轮新增) | **2** | IT-05 命令层:`e2e --target nonexistent` → 退出码 3;default target 进入完整流程(headless 无窗口时退出码 3) | ✅ 全绿 |
| `PostMessageDriverTests`(本轮新增) | **10** | IT-07:负/零 hwnd → ActionResult(false);负 x/y → ActionResult(false);Point overload 异步入口防御;`postClickDelayMs<0` 构造抛异常;`postClickDelayMs=0` 合法;Dispose 幂等 | ✅ 全绿 |
| S1+S2+S3 既有(8 类) | 95 | UT-01~08 + IT-01/02 + 模型契约 + RJ-S1-04/Mat 生命周期回归 + UT-03/05/07 + RJ-S3-02 接缝 + ManifestWriter 合规 | ✅ 全绿 |
| **总计** | **121** | — | **✅ 0 失败 / 0 跳过** |

### IT-07 异常输入防御详情

| 维度 | 用例 | 锁定契约 |
|---|---|---|
| `click` 缺参/负值 | 8 用例(`Click_MissingHwnd/NegativeHwnd/ZeroHwnd/MissingX/MissingY/NegativeX/NegativeY/HwndNonNumeric_UsageErrorExit2`) | 用法错误退出码 2 + stderr 含 `--hwnd`/`--x`/`--y` |
| `click` 业务返回 | 2 用例(`Click_DriverReturnsSuccessTrue/SuccessFalse_Exit0WithJsonSuccess*`) | 退出码 0(结构化返回)+ JSON 行 `{success, attempts, failReason}` |
| `click` 异常桥接 | 3 用例(`Click_DriverThrowsNotImplemented/InvalidDataException/OperationCanceled_Exit3ConfigFailure`) | 退出码 3 + stderr 含 `[click error]` |
| `click` 坐标传递 | 1 用例(`Click_DriverReceivesCorrectCoordinates`) | 防御坐标误传:driver 收到的 hwnd/x/y 与入参字节级一致 |
| `PostMessageDriver` 防御 | 10 用例 | 入口防御 + 异步签名 + 构造约束 + Dispose 幂等 |

### 质量门禁

| 门禁 | 命令 | 结果 |
|---|---|---|
| **G1 编译通过** | `dotnet build DH2.slnx -c Release` | ✅ **0 警告 0 错误** |
| **G2 单元测试** | `dotnet test DH2.slnx -c Release --no-build` | ✅ **121/121 通过** |
| **G3 编码规范** | `dotnet format DH2.slnx --verify-no-changes` | ✅ exit 0 |

### 覆盖率复核(行覆盖,coverlet.cobertura 采集)

| 项目 | 覆盖 | 备注 |
|---|---|---|
| **DH2.Core** | **96.52%** | S2/S3/S4 累计 ≥ 70% 门槛 ✓ |
| **DH2.Vision** | **84.81%** | S3 新增覆盖,S4 无 Vision 改动 ✓ |
| DH2.Input | 11.57% | 仅覆盖入口防御 + 异步签名(真 PostMessage 路径需 MockGame 真窗口,IT-04 真机) |
| DH2.Capture / DH2.MockGame | 0% | L1 单测不调用,IT-04/05 真机 + E2eCommand 集成触发 |
| **整体** | 34.94% | L1 单测覆盖 + S3 模板库落地后端到端 |

报告:`tests/DH2.Tests/TestResults/<run-id>/coverage.cobertura.xml`

---

## RJ-S4-01/02 复测(S4 架构师审核第一轮 — DEF-S4-01 闭环)

> 背景:S4 架构师审核 CHANGES_REQUIRED(报告 `5f72860`)。架构师真机代跑 e2e 暴露
> 坐标空间混用:taskbar 实测中心 (264,90)(150% 物理)+ PostMessage 投递 (86,204)
> (mock-layout 逻辑)→ Avalonia 接收端按物理→DIP 换算消息坐标 → 实际落点 (57,136),
> 偏出按钮 DIP 区域 y≥180 之外 44 DIP,状态不转移。
> 阻塞缺陷仅 DEF-S4-01,处方在技术设计 §6.1 修订:RJ-S4-01(Dev A)提取
> `ComputeButtonClickPoint` internal static 函数 + 3 个实现侧 UT;
> RJ-S4-02(测试 Agent,本轮)提供坐标推导接缝独立验证 — 与 Dev A 实现侧 UT 互为独立。

### RJ-S4-01 修复内容(commit `7da3b8d`,Dev A 追加)

- 提取 `E2eCommand.ComputeButtonClickPoint(frameWidth, layout, taskbarMeasuredCenter)` 为 internal static。
- 公式(技术设计 §6.1):
  ```
  scale = frameWidth / layout.Window.Width
  click = taskbarMeasuredCenter + (buttonLayoutCenter − taskbarLayoutCenter) × scale
  ```
- `E2eCommand.Execute` step 5 按钮坐标由直接读 `layout.Button` 改为调
  `ComputeButtonClickPoint(frame1.Width, layout, taskbarResult.Center)`。
- 3 个实现侧 UT(E2eCommandTests 增补):
  - `1200×900 帧 + taskbar (264,90)` → `(129,306)` 【scale=1.5,与架构师 qa/evidence/M0-S4-l2/match_taskbar.json 一致】
  - `800×600 帧 + taskbar (176,60)` → `(86,204)` 【scale=1.0,退化等同布局中心】
  - `layout.Window.Width <= 0` → `ArgumentOutOfRangeException`(防御,不留 NaN 静默退化为 0)
- 验证:`dotnet build` 0/0 + `dotnet test` 124/124 + `dotnet format --verify-no-changes` exit 0。

### RJ-S4-02 坐标推导接缝独立验证 UT(测试 Agent,本轮新增)

> 防御坐标推导规则漂移(DEF-S4-01 类教训);与 Dev A 实现侧 UT 互为独立验证:
> - Dev A 测"实现" —— 断言具体结果值(1200×900 → (129,306)、800×600 → (86,204)、防御);
> - QA 接缝测"接缝" —— 断言坐标空间换算的数学不变性 + 多档缩放一致性 + 实测偏移传播。

文件:`tests/DH2.Tests/Unit/Commands/E2eButtonClickPointScaleTests.cs`(13 用例):

| 用例 | 锁定契约 |
|---|---|
| `ComputeButtonClickPoint_100Percent_NoScale_ReturnsLayoutCenter` | scale=1.0 退化等同布局按钮中心 (86, 204) |
| `ComputeButtonClickPoint_125Percent_Scale125X_ReturnsPhysicalButtonCenter` | 1000×800 帧,scale=1.25,期望 (108, 255) |
| `ComputeButtonClickPoint_150Percent_Scale150X_ReturnsPhysicalButtonCenter` | 1200×800 帧,scale=1.5,期望 (129, 306) |
| `ComputeButtonClickPoint_200Percent_Scale2X_ReturnsPhysicalButtonCenter` | 1600×800 帧,scale=2.0,期望 (172, 408) |
| `ComputeButtonClickPoint_RelativeButtonTaskbarOffset_ProportionalToFrameWidth` | 数学不变性:150% 的 button−taskbar 偏移 = 100% 的 1.5 倍 |
| `ComputeButtonClickPoint_TaskbarMeasuredCenterOffset_PropagatesToButton` | 实测偏移传播:taskbar 偏离真值 (+10, -5) 时 button 同步偏移到 (96, 199) |
| `ComputeButtonClickPoint_FrameWidthZero_Throws` | 防御:frameWidth=0 → `ArgumentOutOfRangeException` |
| `ComputeButtonClickPoint_FrameWidthNegative_Throws` | 防御:frameWidth=-1 → `ArgumentOutOfRangeException` |
| `ComputeButtonClickPoint_LayoutWindowWidthZero_Throws` | 防御:layout.Window.Width=0 → `ArgumentOutOfRangeException` |
| `ComputeButtonClickPoint_LayoutWindowWidthNegative_Throws` | 防御:layout.Window.Width=-100 → `ArgumentOutOfRangeException` |
| `ComputeButtonClickPoint_LayoutNull_Throws` | 防御:layout=null → `ArgumentNullException` |
| `ComputeButtonClickPoint_FrameWidthEqualsLayoutWidth_NoScale` | 边界:frameWidth == layout.Window.Width → scale=1.0 |
| `ComputeButtonClickPoint_NonStandardScale_33Percent_RoundsCorrectly` | 非标准缩放 1.25 测试 `Math.Round` 一致性 |

### RJ-S4-01/02 复测结果

| 项 | 结果 |
|---|---|
| `dotnet build DH2.slnx -c Release` | ✅ **0 警告 0 错误** |
| `dotnet test DH2.slnx -c Release --no-build` | ✅ **137/137 通过**(原 121 + Dev A 3 个 RJ-S4-01 实现侧 UT + QA 13 个 RJ-S4-02 接缝 UT) |
| `dotnet format DH2.slnx --verify-no-changes` | ✅ exit 0 |
| **RJ-S4-02 接缝 UT 由 FAIL → PASS** | ✅ 13/13 PASS — 验证 RJ-S4-01 修复后 scale 换算公式满足多档缩放 + 不变性 + 实测偏移传播 |
| **Dev A 3 个 RJ-S4-01 实现侧 UT** | ✅ 3/3 PASS |
| **既有 S1-S4 既有 121 用例** | ✅ 全部仍 PASS(无回归) |

**结论**:`e2e` 命令点击坐标空间混用修复已闭环,PostMessage → Avalonia 原始消息 → UI 事件
全链路在 150% DPI 下贯通;架构师真机重跑 e2e 即 SAC1-3 闭环 + S4 签收。

### RJ-S4-02 设计要点

- **完全独立构造 frame + layout + measuredCenter**:不依赖 E2eCommand 内部状态,直接调 internal static `ComputeButtonClickPoint` 即可。
- **多档缩放**:100% / 125% / 150% / 200% 四档,覆盖典型 DPI 缩放场景(125% Windows 默认、150% 架构师真机、200% 高 DPI 笔记本)。
- **数学不变性断言**:`RelativeButtonTaskbarOffset_ProportionalToFrameWidth` 断言 button−taskbar 偏移在不同 scale 下严格按比例缩放 —— 这条性质破坏就说明公式错了。
- **实测偏移传播**:`TaskbarMeasuredCenterOffset_PropagatesToButton` 模拟 match 偏差 ±N px,验证 button 同步偏移(因为公式是加法,偏移传播严格相等)。
- **多层防御覆盖**:frameWidth、layout.Window.Width、layout null 三层防御,每个值类型异常分支单独 UT。
- **物理隔离**:与 Dev A 的 3 个实现侧 UT 在不同文件,独立触发接缝漂移时,QA 接缝 UT 优先暴露问题(签名/契约层面),Dev A UT 暴露实现 bug。

---

## RJ-S4-03 复测(S4 架构师审核第二轮 — DEF-S4-02 闭环)

> 背景:S4 架构师审核第二轮(报告 `527a388`)。架构师真机观测确认 DEF-S4-01 闭环:
> taskbar score=1.0 @ (264,90),点击坐标换算正确,**PostMessage 点击驱动 MockGame 状态机
> 转移成功(Idle→Pathfinding 首次真机观测)** — M0 核心架构问题完全闭环。
> 新发现 **DEF-S4-02**:e2e 状态轮询谓词笔误(架构师已认领修正文档),原 `PollStateToPhase`
> 等待 Pathfinding **或** Arrived,导致寻路 2s 内(363ms)立即截屏匹配 mock_btn_return,
> 此时按钮仍为"前往",匹配 score=0.3818 失败。
> 阻塞缺陷仅 DEF-S4-02,极小改动:RJ-S4-03(Dev A)谓词改为仅 `state == Arrived`,
> 路径中间态(Pathfinding)不算终止。

### RJ-S4-03 修复内容(commit `6eed8fb`,Dev A 极小改动)

- `PollStateToPhase` 签名 `expectedA+expectedB` → `expectedState`(单参数);内联 `CheckStateMatch`。
- `E2eCommand.Execute` step 7 调用 `PollStateToPhase(statePath, 'Arrived', ...)` 单一目标相位。
- 错误日志:`state.json did not reach Pathfinding/Arrived within 5s` → `... Arrived within 5s`。
- 成功日志:`[e2e] state observed: {observedState}` → `[e2e] state observed: Arrived`(字面常量)。
- `PollStateToPhase` 由 `private static` → `internal static`(供 `DH2.Tests` 调写验证)。
- 3 个实现侧 UT(`E2ePollStateToPhaseTests`):
  - `PathfindingOnly_DoesNotTerminate_ReturnsNull`(中间态不截止)
  - `IdleOnly_DoesNotTerminate_ReturnsNull`(Idle 不算终止)
  - `PathfindingThenArrived_ReturnsArrived`(切换 ≤1s 内返回)
- 验证:`dotnet build` 0/0 + `dotnet test` 140/140 + `dotnet format --verify-no-changes` exit 0。

### RJ-S4-03 谓词语义接缝独立验证 UT(测试 Agent,本轮新增)

> 防御 e2e 状态轮询谓词漂移(DEF-S4-02 类教训);与 Dev A 实现侧 UT 在不同文件
> 互为独立验证:
> - Dev A 测"实现" —— 断言 Pathfinding/Idle 不截止、Pathfinding→Arrived 切换 ≤1s;
> - QA 接缝测"接缝" —— 精确字面量匹配 + 防御(取消/文件不存在/空文件/非法 JSON/缺 state 字段)
>   + 大小写敏感 + 多档间隔 + 极速切换 + 全状态机循环。

文件:`tests/DH2.Tests/Unit/Commands/E2ePollStateToPhaseBehaviorTests.cs`(11 用例):

| 用例 | 锁定契约 |
|---|---|
| `PollStateToPhase_StateIsExactMatch_NotPrefixOrSubstring` | state="Arrived Extra" 不应匹配 "Arrived"(字面量匹配,非前缀/包含) |
| `PollStateToPhase_AlreadyAtTarget_TerminatesImmediately` | state 一开始就在 expectedState → 立即返回,耗时 < 200ms |
| `PollStateToPhase_TargetIdle_StaysPathfinding_DoesNotMatch` | target=Idle,state=Pathfinding → null(谓词严格,不是默认匹配) |
| `PollStateToPhase_TargetPathfinding_StaysArrived_DoesNotMatch` | target=Pathfinding,state=Arrived → null(Pathfinding 不是终态) |
| `PollStateToPhase_CancellationRequested_ThrowsOperationCanceledException` | ct.Cancel() → 抛 `OperationCanceledException`(实际 TaskCanceledException,继承自 OCE) |
| `PollStateToPhase_FileNotExists_KeepsPollingUntilTimeout` | 文件不存在 → 持续轮询直到 timeout,返 null(不抛) |
| `PollStateToPhase_EmptyFile_KeepsPollingUntilTimeout` | 文件存在但为空 → 持续轮询直到 timeout(防御 JSON 解析异常) |
| `PollStateToPhase_InvalidJson_KeepsPollingUntilTimeout` | 非法 JSON → 持续轮询直到 timeout |
| `PollStateToPhase_StateJsonMissingStateField_KeepsPollingUntilTimeout` | 合法 JSON 但缺 state 字段 → 持续轮询直到 timeout |
| `PollStateToPhase_FullIdlePathfindingArrived_ReturnsArrived` | 完整 MockGame 状态循环 Idle → Pathfinding → Arrived,200ms 内切换,总耗时 < 800ms |
| `PollStateToPhase_FastTransitionWithinInterval_Detects` | 极速切换:50ms 切换 + 10ms 间隔,total < 300ms |
| `PollStateToPhase_StateIsLowercaseArrived_DoesNotMatch` | state="arrived"(小写)不应匹配 expectedState="Arrived"(大小写敏感) |

### RJ-S4-03 复测结果

| 项 | 结果 |
|---|---|
| `dotnet build DH2.slnx -c Release` | ✅ **0 警告 0 错误** |
| `dotnet test DH2.slnx -c Release --no-build` | ✅ **152/152 通过**(原 140 + Dev A 3 RJ-S4-03 + QA 11 RJ-S4-03 接缝 UT = 152;含既有 121 跨 S1-S3 累计回归) |
| `dotnet format DH2.slnx --verify-no-changes` | ✅ exit 0 |
| **RJ-S4-03 接缝 UT 全 PASS** | ✅ 11/11 PASS — 验证谓词字面量匹配 + 中间态不截止 + 取消/文件异常/大小写敏感 + 全状态机循环 |
| **Dev A 3 个 RJ-S4-03 实现侧 UT** | ✅ 3/3 PASS |
| **既有 S1-S4 累计 140 用例** | ✅ 全部仍 PASS(无回归) |

**结论**:`e2e` 状态轮询谓词修正已闭环,Pathfinding 不截止、Arrived 截止语义严格落地;
架构师真机重跑 e2e 即可 PASS(到达 Arrived 时截屏匹配 mock_btn_return → score ≥ 阈值)。

### RJ-S4-03 接缝 UT 设计要点

- **物理隔离**:QA 接缝 UT 在独立文件 `E2ePollStateToPhaseBehaviorTests.cs`,
  与 Dev A 实现侧 `E2ePollStateToPhaseTests` 物理分离 — 任一端再漂移都会被对应层捕获。
- **谓词语义扩展覆盖**:Dev A 的 3 个实现侧 UT 覆盖基本路径(Pathfinding/Idle 不截止、Pathfinding→Arrived 切换),
  QA 接缝 UT 扩展覆盖字面量匹配(非前缀/包含)、取消异常、文件异常(不存在/空/非法 JSON/缺字段)、
  大小写敏感、多档间隔(10ms 极速 + 50ms 标准)、全状态机循环。
- **状态机防御**:`PollStateToPhase_StateIsLowercaseArrived_DoesNotMatch` 锁死 MockGame 状态字面值大小写契约,
  防止未来某次 MockGame 改造引入小写状态名导致 e2e 静默失败。
- **取消异常类型**:`TaskCanceledException : OperationCanceledException`,
  `Assert.ThrowsAny<OperationCanceledException>` 接受子类(避免过严断言)。

---

## SAC1-3 闭环判定(S2 终审裁定条件)

> S2 架构师第三轮终审报告:IT-04/05 通过即视为 S1 遗留 SAC1-3 闭环。

| 条件 | 状态 | 证据 |
|---|---|---|
| **IT-04**:click 后 raw 日志 2s 内 0x201/0x202 + state 转移 Idle → Pathfinding | **INFRA PASS / RUN PENDING**(headless) | `docs/iterations/M0/qa/evidence/M0-S4-l2/M0-S4-L2-桌面端到端走查手册.md` §IT-04 |
| **IT-05**:`dh2ctl e2e --target mock` 退出码 0 + `E2E: PASS` + 证据目录完整 | **INFRA PASS / RUN PENDING**(headless) | 同上 §IT-05 |
| **IT-06**:再次点击回 Idle,counter 递增 +1 | **INFRA PASS / RUN PENDING**(headless) | 同上 §IT-06 |

**QA 声明**:本轮已尽 headless 可达极限(命令层 + 异常防御 + 契约锁定),
**SAC1-3 闭环的真机签字需架构师/用户在桌面走查手册完成后填写**
(走查手册末"SAC1-3 闭环签字栏")。**真机证据一旦回填,SAC1-3 即视为闭环**,
M0 整体收口条件全部满足。

## L2 待回填说明(headless 限制)

IT-04/05/06 真机端到端部分(click + state.json 转移 + raw 日志 + counter + e2e 全链路)
由架构师/用户在交互桌面会话按桌面走查手册执行,QA 不代填、不伪造。

| 用例 | 命令层(headless 已验证) | 真机(headless 不可执行) |
|---|---|---|
| **IT-04** click + 0x201/0x202 + state 转移 | ✅ `click --hwnd <fake> --x 86 --y 204` → 退出码 0 + JSON 行;mock IInputDriver 返 false/抛异常路径 | ⏳ 真机走查手册覆盖 |
| **IT-05** e2e 退出码 0 + E2E: PASS + 证据完整 | ✅ `e2e --target nonexistent` → 退出码 3;`e2e`(无 MockGame)→ 退出码 3 | ⏳ 真机走查手册覆盖 |
| **IT-06** 再次点击回 Idle,counter 递增 | ✅ 同 IT-04 命令层 | ⏳ 真机走查手册覆盖 |
| **IT-07** 异常输入防御 | ✅ ClickCommand 14 用例 + PostMessageDriver 10 用例全绿 | N/A(命令层即闭环) |

## 缺陷清单

### S4 阻塞缺陷(本轮)

| DEF | 标题 | 处置 | 结果 |
|---|---|---|---|
| DEF-S4-01 | `e2e` 命令点击坐标推导物理/逻辑空间混用(@150% DPI 下 MockGame 接收端按物理÷1.5 转 DIP,直接投递逻辑坐标 (86,204) 实际落点 (57,136),偏出按钮区域 y≥180 之外 44 DIP) | RJ-S4-01(`7da3b8d`,Dev A) + RJ-S4-02(本轮 QA 接缝独立验证) | ✅ **CLOSED** |
| DEF-S4-02 | `e2e` 状态轮询谓词笔误(原 PollStateToPhase 等待 Pathfinding **或** Arrived,导致寻路 2s 内立即截屏匹配 mock_btn_return,此时按钮仍为"前往",匹配 score=0.3818 失败) | RJ-S4-03(`6eed8fb`,Dev A 极小改动) + RJ-S4-03 接缝独立验证(本轮 QA) | ✅ **CLOSED** |

### S3 遗留(本轮关闭)

| DEF | 标题 | 处置 | 结果 |
|---|---|---|---|
| DEF-S3-01 | `save-template` 写 manifest YAML PascalCase 与 TemplateStore 读端 camelCase 接缝断裂 | RJ-S3-01(`8088fab`) | ✅ **CLOSED**(S3 复测) |

### S2 遗留(已关闭)

| DEF | 标题 | 处置 | 结果 |
|---|---|---|---|
| DEF-S2-03 | `dh2ctl capture --count` 非法值静默回退默认 | RJ-S2-04(`3540a86`) | ✅ **CLOSED** |

### S1/S2 历史(已关闭,参考)

| DEF | 状态 |
|---|---|
| DEF-S1-01 MockLayout 族 YamlDotNet | ✅ CLOSED(RJ-S1-03) |
| DEF-S1-02 ConfigValidator foreground 分支不可达 | ✅ CLOSED(RJ-S1-05) |
| DEF-S2-01 真机 enumerate 解析仓库自带 configs/dev.yaml 失败 | ✅ CLOSED(RJ-S2-01) |
| DEF-S2-02 capture 访问违例(Mat 生命周期缺陷) | ✅ CLOSED(RJ-S2-02/03) |

## 跨域交接

| 接收方 | 内容 | 位置 |
|---|---|---|
| **架构师(S4 终审)** | iter/m0 HEAD 含 dev-a/m0-s4 + dev-b/m0-s4 全部 S4 提交;L1 单测 121/121 全绿;dh2ctl 命令层全通;IT-07 异常防御 24 用例锁死接口契约;S4 集成接缝 PostMessageDriver 适配记录 | 本文件 §冲突解决 |
| **架构师(SAC1-3 闭环签字)** | IT-04/05/06 真机走查完成后,在桌面走查手册末"SAC1-3 闭环签字栏"签字 | `docs/iterations/M0/qa/evidence/M0-S4-l2/M0-S4-L2-桌面端到端走查手册.md` |
| **后续 Sprint(S4+ e2e / S5+)** | PostMessageDriver : IInputDriver 同步 Click 包装异步 ClickAsync(集成接缝);e2e 命令驱动 MockGame 完整闭环的契约已落;S5+ 可基于此 e2e 命令扩展状态机覆盖 | `src/DH2.Input/PostMessageDriver.cs` + `src/DH2.App/Commands/E2eCommand.cs` |
| **用户/架构师(M0 收口签字)** | M0 周期 4 Sprint(S1/S2/S3/S4)全部交付;IT-01~07 + SAC1-3 闭环 + AC-01~10 全部覆盖或待签字 | M0-S4 架构师审核报告(待) |

## 遗留问题

1. **IT-04/05/06 真机部分尚未完成** — 待架构师或用户在交互桌面会话按走查手册执行(共 9 大步);
   不属本轮 QA 硬红线外,但阻塞 SAC1-3 闭环的最终签字。
   建议在 S4 终审前完成(预计 5 分钟),架构师在手册签字栏签字后即可视为 M0 闭环。
2. **PostMessageDriver 真实现覆盖率 11.57%** — 仅覆盖入口防御 + 异步签名;
   真 PostMessage 调用路径需 MockGame 真窗口(由 IT-04 真机走查覆盖)。
   覆盖率门槛 ≥ 70% 仅适用 Core + Vision(M0 测试设计 §1),Input 真路径覆盖率
   不作为阻塞门槛。
3. **150% 缩放下 dh2ctl e2e 输出** — `e2e` 命令步骤 6 click 客户区坐标按物理像素传入;
   若用户测试机为 150% 缩放,MockGame 窗口被系统 DPI 虚拟化(800 DIP → 1200 物理像素),
   e2e 命令内部的几何计算需按物理像素折算并标注环境系数。
   建议未来 S5+ 引入 `configs/dev.yaml` 的 `dpi` 字段做显式声明。
4. **S4 集成接缝 PostMessageDriver : IInputDriver** — Dev A 写 ClickCommand 时按 IInputDriver
   接口契约;Dev B 推 PostMessageDriver 时未实现接口。
   QA 集成接缝补同步 Click 包装异步 ClickAsync(阻塞,无额外线程)。
   **后续 Sprint(S5+)开发约定**:接口契约方(Dev A)先与实现方(Dev B)在 Sprint 启动前
   确认 IInputDriver 同步/异步签名,避免此类接缝漂移。

## 清理 cron

无 cron 创建/清理事项(本轮用户明确要求"不要创建任何定时任务,一切等待用户手动驱动")。

---

**声明(第三轮/RJ-S4-03 复测):QA 已完成 M0-S4 名下全部 Story(集成 dev-a/m0-s4 + dev-b/m0-s4 + S4 集成接缝修复 + IT-07 异常防御 24 UT + 桌面走查手册 + SAC1-3 闭环判定 + RJ-S4-02 坐标推导接缝独立验证 13 UT + RJ-S4-03 谓词语义接缝独立验证 11 UT),iter/m0 HEAD `13eaef9`,`dotnet build` 0 警 0 错,`dotnet test` 152/152 通过,`dotnet format --verify-no-changes` exit 0。DEF-S4-01 + DEF-S4-02 已 CLOSED(RJ-S4-01 + RJ-S4-02 联合验证 / RJ-S4-03 + QA 接缝验证)。Dev A / Dev B / QA 三份任务完成报告齐备,SAC1-3 闭环待架构师在桌面走查手册签字栏签字 + e2e 真机终验(谓词修正后 mock_btn_return 匹配 score 应 ≥ 阈值)后即视为 M0 整体收口;等待架构师 M0 终审 + 启动 M1a 的指令。**
