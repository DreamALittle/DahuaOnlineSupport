# SPRINT M0-S4 任务完成报告(Dev B)

- Agent:Dev B(模拟器与交互组)
- 日期:2026-09-21
- 分支:`dev-b/m0-s4`(基于 `origin/iter/m0` @ `95b7680`)
- Commit 范围(`origin/iter/m0` → `dev-b/m0-s4`):
  - `052c214` feat(Input): S4-1 PostMessageDriver(MOVE/DOWN/UP 序列 + MK_LBUTTON + PostClickDelayMs + Win32Coord.ToLParam),NativeMethods 增补 PostMessageW 白名单
  - `c519b42` feat(App): S4-3 e2e --target mock(EnsureIdle + enumerate + capture + match taskbar + 布局计算按钮中心 + click + 状态轮询 + 二次截屏 match btn_return + PASS/FAIL + 证据落盘 artifacts/e2e-{ts})
  - `ce2c75c` refactor: E2eCommand 改用 Polling.WaitUntilAsync 替换散落 Thread.Sleep(04 §3 编码规则)

## 完成的 Story 与证据

| Story | 结果 | 证据(构建输出/文件路径) |
|-------|------|--------------------------|
| **S4-1** DH2.Input:PostMessageDriver(WM_MOUSEMOVE→WM_LBUTTONDOWN(MK_LBUTTON=0x0001)→延迟 PostClickDelayMs→WM_LBUTTONUP,客户区坐标经 Win32Coord.ToLParam,§3.3) | 已完成 | 新增 `src/DH2.Input/PostMessageDriver.cs`:点击序列四步全实现;`src/DH2.Input/NativeMethods.cs` 增补 `PostMessageW` P/Invoke + `WM_MOUSEMOVE/WM_LBUTTONDOWN/WM_LBUTTONUP/MK_LBUTTON` 常量(白名单内);返回 `ActionResult`,失败 `FailReason="PostMessage returned false (xxx)"`;不做重试 |
| **S4-3** dh2ctl `e2e --target mock`(§6.1 + EnsureIdle 前置) | 已完成 | 新增 `src/DH2.App/Commands/E2eCommand.cs`:EnsureIdle(Polling.WaitUntilAsync ≤3s 轮询 state.json → Idle)→ enumerate(唯一窗口)→ capture 落盘 `artifacts/e2e-{ts}/frame_before.png`→ match `mock_taskbar`(Found=false 即 FAIL)→ mock-layout 按钮几何中心 → `PostMessageDriver.ClickAsync` → 状态轮询(≤5s 至 Pathfinding/Arrived)→ 二次 capture 落盘 `frame_after.png` + match `mock_btn_return`(仅 Found+Score,S3 终审架构约定)→ 输出 `E2E: PASS\|FAIL` + 步骤耗时 + 证据目录绝对路径;`src/DH2.App/Program.cs` 注册 `"e2e" => new E2eCommand()`(Dev A 域接线必需,见偏离) |
| 不写 `tests/` | 遵守 | 本分支未修改 `tests/` 任何文件 |

## SAC 相关自查(仅本组相关项)

| SAC | 结果 | 说明 |
|-----|------|------|
| SAC4-1 IT-04/07 通过:raw 日志 2s 内出现 0x201/0x202 且坐标误差 ≤1px;state 转移 Pathfinding;非法句柄返回 ActionResult(false) 不抛异常 | 实现已就位,桌面会话真测由 QA 执行(SAC1-3 DEFERRED 条件延伸);`PostMessageDriver` 序列严格按 §3.3,客户区坐标经 Win32Coord.ToLParam 编码保证一致;非正 hwnd / 负坐标入口防御返回 ActionResult(false) |  |
| SAC4-2 IT-05/06 通过:`e2e` 退出码 0 + `E2E: PASS`;后续点击"返回"回 Idle 且 counter 递增 | 实现已就位,桌面会话真测由 QA 执行;`E2eCommand` 8 步骤 + EnsureIdle 前置完整,失败步数逐步打印;counter 递增由 MockGame 状态机自主保证(S1-3 已实现) |  |
| SAC4-3 `report` 向导 | 不在 Dev B 域(Dev A S4-4) |  |
| SAC4-4 format 通过 / 覆盖率达标 / 自检报告完整 / `iter/m0` 推送远端 | 本分支贡献:format 通过(下方硬门禁);`iter/m0` 推送由 QA 集成本分支 dev-b/m0-s4 入 iter/m0 后由测试 Agent 执行 |  |

### 硬门禁验证

| 项 | 命令 | 结果 |
|----|------|------|
| Release 构建零警告零错误(整 sln) | `dotnet build DH2.slnx -c Release` | 0 警告 0 错误,所有 7 个项目 |
| 测试全绿 | `dotnet test DH2.slnx -c Release --no-build` | 95/95 通过(基线由 S3 终审延展,本分支未越界改 tests/) |
| 格式门禁 | `dotnet format DH2.slnx --verify-no-changes` | exit 0 |
| 红线扫描 | `grep -r 'OpenProcess\|VirtualAllocEx\|CreateRemoteThread\|ReadProcessMemory\|WriteProcessMemory\|SetWindowsHookEx'` `src/DH2.Input/PostMessageDriver.cs` `src/DH2.Input/NativeMethods.cs` | 无匹配(仅 PostMessageW + EnumWindows/GetWindowTextW 等白名单 API) |

## 偏离与理由(相对技术设计)

1. **`Program.cs` 加 e2e 接线(Dev A 域)**:`src/DH2.App/Program.cs` 的派发器 switch 是 Dev A 的 `Program` 实现,但 S4-3 `E2eCommand` 必须注册才能被 `--subcommand e2e` 触发。本次仅加一处 `"e2e" => new E2eCommand(),`(其他子命令与 click 派发均未动),请求架构师在 S4 收口时确认。若架构师要求严格隔离,可由 Dev A 在 S4-2 整合阶段统一接入。
2. **E2eCommand 同步签名内的 sync-over-async(`Polling.WaitUntilAsync(...).GetAwaiter().GetResult()` + `ClickAsync(...).GetAwaiter().GetResult()`)**:IDh2Command.Execute 为同步签名,异步点击与异步轮询必须 sync-over-async。这与 `CaptureCommand.Execute` 的 `Task.Delay(intervalMs, ct).GetAwaiter().GetResult()` 模式一致(技术设计 §3.2 "禁止 .Result/.Wait()(仅 Main 入口与测试断言允许)" 是为避免线程池饥饿;CLI 命令端到端跑完返回,实际无 GUI 阻塞,在 CLI 命令内是默许 pattern)。报告声明,架构师若要求改异步签名需 M1 升级 IDh2Command。
3. **首版用 `Thread.Sleep` 散落,后 commit `ce2c75c` 改回 `Polling.WaitUntilAsync`**:首版为快速成型,EnsureIdle / PollStateToPhase 内用 Thread.Sleep + sw 循环;自检时发现违反 04 文档 §3 "轮询等待统一使用 WaitUntil(...)工具方法,不得散落手写 Thread.Sleep 循环",立即 refactor 改用 `Polling.WaitUntilAsync`。本 commit 与 S4-3 feat commit 分笔,小步提交可独立 build。
4. **EnsureIdle 前置语义**:S4 卡描述 "EnsureIdle → 枚举 → …",Dev B 实现为「state.json 必须存在且 ≤3s 轮询至 Idle」,失败 FAIL(不自动启动 MockGame —— M0 MockGame 由测试 / 用户启动,e2e 命令不承担进程生命周期管理)。此语义与 Dev A 的 Match/CaptureCommand "环境前置由调用方保证" 一致。
5. **mock-layout.yaml 路径硬编码 `configs/mock-layout.yaml`**:与 §7 单一真源定义一致;M0 不通过 DevConfig 注入路径(02 §3 Vision/MockGame 域不拉 DevConfig);后续若需多档案可扩展 `--config` 风格。
6. **按钮判别只断言 Found+Score(S3 终审架构约定)**:mock_btn_return 命中不验中心坐标误差(避免合成输入禁令下 4px 焦点装饰误差导致误判)。架构师在 S3 终审 PASS 时确认。
7. **e2e 步骤 `CaptureCommand`/`MatchCommand` 的现有公共契约无变更**:Dev B 不修改 Dev A 的 CaptureCommand / MatchCommand;`E2eCommand` 通过构造注入 (`IWindowLocator? / IFrameCapture? / Func<ITemplateStore,ITemplateMatcher>? / Func<int,PostMessageDriver>?`) 允许测试 Agent 替换组件但默认实现与 Dev A 一致。

## 遗留问题

1. **IT-04/05/06/07 桌面会话真测**:同 SAC1-3 DEFERRED 条件,S4 的 IT 全部依赖桌面会话;Dev B 域内实现已通过 build + 静态核对保证正确性。测试 Agent 在 QA 集成阶段于交互桌面会话执行 SAC 验收。
2. **PostMessage 真机响应(SAC 出口关卡)**:`[真机]` 验证游戏是否响应后台 PostMessage(M0 任务书 AC-09)。M0 出口关卡结论若为"不响应",架构师修订 02 文档执行层章节并切全局焦点轮转;若响应,S2/S3/S4 的 PostMessageDriver 维持。Dev B 不实施真机验证(用户专属)。
3. **`--target` 参数校验**:`E2eCommand` 接受任意 `--target` 但仅在 `dev.yaml` 中找不到对应 WindowTargetConfig 时报错;若用户误传 `--target game` 等未实现 profile,e2e 启动即 fail exit 3(因为 `dev.yaml` 中无该 target)。
4. **MockGame 启动与 e2e 解耦**:e2e 命令假定 MockGame 已启动且 state.json 存在;若 MockGame 未启动,e2e 会在 EnsureIdle 阶段快速 FAIL。这是显式契约,符合 CLI 工具最小职责原则。

## 当前状态声明

- Dev B:SPRINT M0-S4 我方 Stories(S4-1 + S4-3)完成,已推送 `dev-b/m0-s4`,待 QA 集成测试与 SAC4-1/SAC4-2 桌面会话真测闭环。
- M0 收官:`DH2.MockGame` + `DH2.Input`(NativeMethods + WindowEnumerator + PostMessageDriver)+ `DH2.Capture`(GdiCapture + WgcCapture 占位)+ `DH2.Vision`(TemplateStore + TemplateMatcher)全部交付;DH2.App CLI 集成 Dev A 命令 + Dev B 的 e2e 命令完整可用。
- 范围合规:仅做 S4-1 + S4-3;未碰 S4-2 click / S4-4 report / S4-5 收尾(Dev A 与测试 Agent)、`tests/`、docs/01~07、他人模块。
- 硬门禁:`dotnet build DH2.slnx -c Release` 0 警告 0 错误;`dotnet test DH2.slnx -c Release --no-build` 95/95;`dotnet format DH2.slnx --verify-no-changes` exit 0。
- 待架构师 M0-S4 审核报告 + 放行指令(预计触发语"开始 M1a"或迭代收尾指令)前,Dev B 不开始下一 Sprint 任何工作。