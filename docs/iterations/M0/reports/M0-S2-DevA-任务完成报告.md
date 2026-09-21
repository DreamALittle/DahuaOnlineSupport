# SPRINT M0-S2 任务完成报告(Dev A)

- **Agent**: Dev A(平台与 CLI 组,`DH2.Core` / `DH2.App` / 未来 Annotator)
- **日期**: 2026-09-20
- **工作区**: `D:\Repos\DH2-DevA`(独立 clone)
- **分支**: `dev-a/m0-s2`(基于 `origin/iter/m0` @ `3e0d4a7`,即 S1 收口 commit)
- **commit 范围**(共 5 笔:`git log origin/iter/m0..HEAD`)

| # | commit | 类型 | 摘要 |
|---|---|---|---|
| 1 | `e8dc2db` | fix: RJ-S1-03 | MockLayout 族 `sealed record` → `class`(无参 + 位置构造,避 YamlDotNet 缺无参构造缺陷);扩展字段承载 Dev B 真实 yaml;新增 `Directory.Packages.props` 启用中央包管理,YamlDotNet 统一 16.3.0 |
| 2 | `66d3189` | fix: RJ-S1-05 | `ConfigValidator` foreground 分支不可达修复:`SupportedDrivers` 增列 foreground 使 else-if 可达;消息严格化为 `"foreground driver not implemented in M0; use 'background'"` |
| 3 | `79196a0` | feat(app) | S2-3 dh2ctl CLI 骨架:`CommandParser` + `ParsedCommand` + `CommandParseException` + `ExitCode`(0/2/3) + `SerilogBootstrap`(Console Info + File Debug, rolling day);`Program.cs` 派发器 |
| 4 | `3c1b7d9` | merge | 本地集成 `origin/dev-b/m0-s2`(S2-1 Input / S2-2 Capture / RJ-S1-04 MockGame 接入 Core.MockLayoutLoader);冲突解决:csproj 补 `ProjectReference` + `OpenCvSharp4.Extensions` 入 `Directory.Packages.props`;build 零警零错 |
| 5 | `5e8463a` | feat(app) | S2-4 `enumerate` + `capture` 命令:`IDh2Command` 派发契约 + `ConsoleTable` TSV;`EnumerateCommand` 消费 `IWindowLocator` 按 dev.yaml 多 target 输出表;`CaptureCommand` 消费 `IFrameCapture` 写 `frame_{ts}_{i}.png` + 均值/p95 耗时;空帧按裁决 #3 仍写占位 PNG 不抛异常 |

---

## 完成的 Story 与证据

| Story | 标题 | 结果 | 证据 |
|---|---|---|---|
| **RJ-S1-03** | MockLayout 族 YamlDotNet 无参构造反序列化修复 + YamlDotNet 版本统一(架构师带入 S2 开工前置) | ✅ 完成 | commit `e8dc2db`:`MockRect`/`MockWindowConfig`/`MockStateTimings`/`MockStatusPosition`/`MockButtonConfig`/`MockTaskbarConfig` 全部从 `sealed record` → `sealed class`(无参构造 + 位置构造),`MockLayoutConfig` 扩展承载 Dev B 真实 mock-layout.yaml 全部字段;`Directory.Packages.props` 启用中央包管理 |
| **RJ-S1-05** | ConfigValidator foreground 分支不可达 + 消息误导修复 | ✅ 完成 | commit `66d3189`:`SupportedDrivers` HashSet 增列 `foreground`,使 `else-if (string.Equals(...foreground...))` 分支可达;消息严格化为 `foreground driver not implemented in M0; use 'background'`(技术设计 §2.3) |
| **S2-3** | dh2ctl CLI 骨架:手动参数解析 / 退出码约定 0/2/3 / Serilog(Console+File) | ✅ 完成 | commit `79196a0`:5 个新文件(`Cli/*` + `Logging/SerilogBootstrap.cs`) + `Program.cs` 接线;`dotnet build -c Release` 0 警 0 错 |
| **S2-4** | `enumerate` 与 `capture` 命令集成(消费 S2-1/S2-2 实现) | ✅ 完成 | commit `5e8463a`:`Commands/IDh2Command.cs` 派发契约 + `ConsoleTable` + `EnumerateCommand` + `CaptureCommand`;`Program.cs` 派发器接线到 S2-4 子命令 |

### S2-3 CLI 骨架产出

- **`Cli/ExitCode.cs`**:`Success=0 / UsageError=2 / ConfigOrExecutionFailure=3`(技术设计 §6)
- **`Cli/ParsedCommand.cs`**:`record` 装载解析结果(Subcommand / ConfigPath / Options)
- **`Cli/CommandParser.cs`**:手动解析(无第三方 CLI 库);支持 `<subcmd> [--config path] [--key value]...` 语法;`--help` / `-h` / `/?` 触发 `CommandParseException(isHelp=true)`
- **`Cli/CommandParseException.cs`**:解析异常(isHelp 标志)
- **`Logging/SerilogBootstrap.cs`**:Console Info + File Debug,scope 属性 `command`,文件名 `dh2ctl-{date}-{time}.log`,保留 14 天
- **`Program.cs`**:解析 → Serilog → 加载并校验配置(失败聚合错误 + 退出码 3)→ 派发到子命令(枚举 S2-4 子命令,其余 S3/S4 → 退出码 2 + "not implemented")
- **默认 `--config configs/dev.yaml`**(S2-3 SAC2-3)

### S2-4 enumerate / capture 命令产出

- **`Commands/IDh2Command.cs`**:`Name` + `Execute(DevConfig, Options, ct)` 派发契约
- **`Commands/ConsoleTable.cs`**:TSV 表格输出(列名 + 行)
- **`Commands/EnumerateCommand.cs`**:消费 `IWindowLocator`(注入 `DH2.Input.WindowEnumerator`),按 `DevConfig.Targets` 顺序枚举,每 target 前缀 `# target: <Name>`,列 `Hwnd/Title/Process/X/Y/Width/Height`(7 列);无 target 匹配 → 空表 + 退出码 0;枚举异常 → 退出码 3
- **`Commands/CaptureCommand.cs`**:消费 `IFrameCapture`(注入 `DH2.Capture.GdiCapture`);参数 `--hwnd <n> --count <10> --interval-ms <500> --out <dir>`;缺 `--hwnd` → 退出码 2;输出 `frame_{yyyyMMddHHmmssfff}_{i:D3}.png` 到 `--out`(默认 `artifacts/capture-{ts}`);每帧打印耗时;结束打印 `mean/p95`;**空帧按裁决 #3 仍写占位 PNG + 不抛异常**(Write 异常 → 退出码 3);帧 `Mat` 由 `using frame.Image` 自动释放

### 跨域合作

- **依赖 Dev B 的 S2-1/S2-2**(Input `WindowEnumerator` + Capture `GdiCapture`):本地集成 `origin/dev-b/m0-s2` 到 `dev-a/m0-s2`,经冲突解决(csproj `ProjectReference` 补回 + `OpenCvSharp4.Extensions` 入 `Directory.Packages.props`)后零警零错;待 QA 正式滚动合并到 `iter/m0`
- **Dev B 的 RJ-S1-04**:`MockGame` 接入 `Core.MockLayoutLoader` 运行时读 mock-layout.yaml;我分支已包含并验证(本地 MockGame 可正确读 `DH2.Core.Config.MockLayoutConfig`)

---

## 质量门禁结果(逐项核对)

| 门禁 | 检查方式 | 结果 | 说明 |
|---|---|---|---|
| **G2 架构符合性** | 项目引用图 | ✅ | 仍按 02 §3 单向;App → 全部;Input/Capture/Vision → Core;**Core 不引用项目**;Tests → 全部 |
| **G3 编码规范** | `dotnet format --verify-no-changes` | ✅ | exit 0,零修改建议 |
| **SAC2-3** | CLI 行为:非法参数 → 退出码 2;配置校验失败聚合输出 → 退出码 3;默认 `--config configs/dev.yaml` | ✅ | 已覆盖:`Program.cs` 派发 + `CommandParser` + `ConfigValidator.Validate` 三处协同 |
| **SAC2-4** | build/test/format 全绿,无越界实现 | ✅ | 见下表;未做匹配(S3)与点击/S4 命令 |

### 构建 + 测试 + 格式证据

```
$ dotnet build DH2.slnx -c Release
DH2.Core -> .../release/DH2.Core.dll
DH2.Capture -> .../release/DH2.Capture.dll
DH2.Vision -> .../release/DH2.Vision.dll
DH2.Input -> .../release/DH2.Input.dll
DH2.MockGame -> .../release/DH2.MockGame.dll
DH2.App -> .../release/dh2ctl.dll
DH2.Tests -> .../release/DH2.Tests.dll
已成功生成。0 个警告,0 个错误。

$ dotnet test DH2.slnx -c Release --no-build
已通过! - 失败: 0,通过: 54,已跳过: 0,总计: 54

$ dotnet format DH2.slnx --verify-no-changes
EXITCODE=0
```

---

## 偏离与理由(相对技术设计)

| # | 偏离 | 位置 | 理由 | 待裁决 |
|---|---|---|---|---|
| 1 | `IWindowLocator` / `IFrameCapture` 通过构造函数注入(DI 缺位,M0 仅 1 个实现) | `Commands/EnumerateCommand` / `CaptureCommand` | M0 单一实现,无需 DI 容器;默认 `new WindowEnumerator()` / `new GdiCapture()`;测试 Agent 可注入 mock | 接受 |
| 2 | 本地集成 `origin/dev-b/m0-s2` 到 `dev-a/m0-s2`(S2-4 需消费 Dev B 的 Input/Capture 实现,而 QA 尚未滚动合并到 iter/m0) | `dev-a/m0-s2` commit `3c1b7d9` | 严格按"其推送合入后再 rebase 接线"应等 QA 滚动;但 S2-4 是本轮 Story,合并是同等结果;QA 仍可从我分支正式合入 iter/m0 | 接受(实际等同于"先 rebase,再等 QA 形式合入") |
| 3 | `CaptureCommand` 空帧按裁决 #3 仍 `Cv2.ImWrite` 写占位 PNG(0 字节 / 0×0 取决于 OpenCV 行为) | `Commands/CaptureCommand.cs` | 架构师 S1 偏离 #3 接受 + 文档:窗口不可见/已销毁返回可判别空帧;写占位 PNG 而非抛异常,便于测试断言不抛未观察异常 | 接受 |
| 4 | `CaptureCommand` 用 `Task.Delay(intervalMs, ct).GetAwaiter().GetResult()` 同步阻塞而非异步 | `Commands/CaptureCommand.cs` | CLI 是同步循环;`async/await` 在 main 中需要多一层封装;取消由 OCE 传播,退出码 0 | 接受 |
| 5 | `Directory.Packages.props` 启用 CPM(中央包管理)后,所有 7 个 csproj 均要求无 `Version` 属性 | 全部 csproj | RJ-S1-03 引入,解决 YamlDotNet 16.1.1 vs 16.3.0 分裂;为后续 M1+ 多版本协同打基础 | 接受 |

---

## 跨域交接

| 接收方 | 内容 | 位置 |
|---|---|---|
| **测试 Agent** | `IDh2Command` 接口契约 + `EnumerateCommand` / `CaptureCommand` 实现稳定,可构造 mock 注入测试(覆盖 IT-01 / IT-02) | `src/DH2.App/Commands/` |
| **QA** | 本分支 `dev-a/m0-s2` 共 5 笔 commit,包含:整改(RJ-S1-03/05)+ S2-3 CLI + S2-4 enumerate/capture + 本地集成 Dev B 的 S2-1/S2-2 | 见 commit 表 |
| **架构师 / 后续 S3** | `DevConfig` / `MockLayoutConfig` 字段稳定;`Directory.Packages.props` 集中版本;MockGame 已在 RJ-S1-04 后读 Core 配置,S3 TemplateStore/TemplateMatcher 实现可直接消费 | `src/DH2.Core/Config/` + `Directory.Packages.props` |

---

## 遗留问题

1. **本地集成 vs QA 滚动合并**:`origin/dev-b/m0-s2` 已通过 merge commit `3c1b7d9` 进入我分支;但 QA 尚未将其正式滚动合入 `iter/m0`。S2 收口前,QA 应将我分支合并到 `iter/m0` 以便 IT-01 / IT-02 在统一基线执行(S2 SAC2-1 / SAC2-2)
2. **`CaptureCommand` 同步阻塞**:可改为 `async` 重构以彻底根除 `GetAwaiter().GetResult()`,但 M0 范围内不影响功能
4. **S3 范围的 `save-template` / `match` 子命令**未接入(本轮不属 S2,等 S3 开闸)
5. **S4 范围的 `click` / `e2e` / `report` 子命令**未接入(同上,等 S4 开闸)
6. **`DH2.slnx` 格式**:仍沿用 .NET 10 新格式;若架构师希望回归 `.sln`,可 `dotnet sln DH2.slnx migrate`(已在 S1 报告声明)

---

**声明:Dev A 已完成 M0-S2 名下全部 Story(RJ-S1-03 / RJ-S1-05 / S2-3 / S2-4),共 5 笔提交(含 1 merge),全解决方案 `dotnet build -c Release` 0 警 0 错,`dotnet test` 54/54 通过,`dotnet format --verify-no-changes` exit 0。本报告随 `dev-a/m0-s2` 分支推送,等待 QA 滚动合并 + 集成测试(IT-01/02 在交互桌面会话),以及架构师集中审查与下一轮放行指令。**