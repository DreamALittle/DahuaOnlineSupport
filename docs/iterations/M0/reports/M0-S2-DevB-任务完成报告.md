# SPRINT M0-S2 任务完成报告(Dev B)

- Agent:Dev B(模拟器与交互组)
- 日期:2026-09-20
- 分支:`dev-b/m0-s2`(基于 `origin/iter/m0` @ `3e0d4a7`)
- Commit 范围(`origin/iter/m0` → `dev-b/m0-s2`):
  - `aef0af5` fix: RJ-S1-04 MockGame 接入 Core.MockLayoutLoader 运行时读 mock-layout.yaml,删除 XAML 与代码中的硬编码几何
  - `f4a7fa4` feat(Input): S2-1 NativeMethods 白名单 + WindowEnumerator 实现 IWindowLocator
  - `f139855` feat(Capture): S2-2 GdiCapture 按客户区屏幕矩形 CopyFromScreen + WgcCapture M1a 占位

## 完成的 Story 与证据

| Story | 结果 | 证据(构建输出/文件路径) |
|-------|------|--------------------------|
| **RJ-S1-04**(S2 首笔整改)MockGame 启动时读 mock-layout.yaml 渲染,几何一律来自配置 | 已完成 | 删除 `tools/DH2.MockGame/Models/MockLayout.cs`、`MockLayoutLoader.cs`、`MockState.cs`(均已走 mavis-trash 回收);新增 `Models/MockPhase.cs`(仅保留枚举);修剪 `configs/mock-layout.yaml` 至 Core 字段(profile/window/taskbar/status/button/stateTimings/tempDir);`AppHost`、`App.axaml.cs`、`MainViewModel`、`MockStateMachine`、`MainWindow.axaml` 均改为绑定 `Core.Config.MockLayoutConfig`;`DH2.MockGame.csproj` 加 `<ProjectReference Include="..\..\src\DH2.Core\DH2.Core.csproj" />`,移除重复 YamlDotNet 引用 |
| **S2-1** DH2.Input:NativeMethods 白名单 + WindowEnumerator | 已完成 | 新增 `src/DH2.Input/NativeMethods.cs`(EnumWindows/IsWindowVisible/IsWindow/GetWindowTextW/GetClassNameW/GetWindowThreadProcessId/GetClientRect/ClientToScreen + RECT/POINT 结构);新增 `src/DH2.Input/WindowEnumerator.cs`(实现 `IWindowLocator`,`Process.GetProcessById` 取进程名,**不直接声明 OpenProcess**) |
| **S2-2** DH2.Capture:GdiCapture + WgcCapture 占位 | 已完成 | 新增 `src/DH2.Capture/NativeMethods.cs`(局部白名单,仅含 GetClientRect/ClientToScreen,Capture 不依赖 Input);新增 `src/DH2.Capture/GdiCapture.cs`(实现 `IFrameCapture`,`Graphics.CopyFromScreen` 按客户区屏幕矩形 + `BitmapConverter.ToMat` 转为 Mat);新增 `src/DH2.Capture/WgcCapture.cs`(构造抛 `NotImplementedException`,M1a 占位);`DH2.Capture.csproj` 加 OpenCvSharp4 + OpenCvSharp4.Extensions 4.10.0.20241107 包 |

## SAC 相关自查(仅本组相关项)

| SAC | 结果 | 说明 |
|-----|------|------|
| SAC2-1 IT-01:MockGame 运行时 `dh2ctl enumerate` 恰好列出 1 条,Title/ProcessName/客户区 Rect 800×600 正确 | 实现已交付 Dev A 的 S2-4 命令集成由 Dev A 集成时跑;`WindowEnumerator.Enumerate` 已实现 Title 正则 + ProcessName 过滤 + GetClientRect+ClientToScreen 客户区 Rect 输出,符合 §3.2 与 §6 enumerate 命令的契约 |  |
| SAC2-2 IT-02:连续 10 帧截图尺寸正确、非黑帧、输出均值/p95 耗时 | 实现已交付;Dev A 的 capture 命令集成由 Dev A 完成;`GdiCapture.Capture` 已按 §4.1 流程(GetClientRect+ClientToScreen → CopyFromScreen → Format32bppArgb → Mat),空帧占位按 S1 偏离裁决 #3(不可见/销毁返回空 Mat 的 Frame) |  |
| SAC2-3 CLI 行为(退出码 2 / 3 / `--config` 缺省) | 不在 Dev B 域,Dev A 责任 |  |
| SAC2-4 build/test/format 依旧全绿,无越界实现 | 通过 | 见下方硬门禁表 |

### 硬门禁验证

| 项 | 命令 | 结果 |
|----|------|------|
| Release 构建零警告零错误(整 sln) | `dotnet build DH2.slnx -c Release` | 0 警告 0 错误,所有 7 个项目 |
| Release 构建零警告零错误(Capture 域) | `dotnet build src/DH2.Capture/DH2.Capture.csproj -c Release` | 0 警告 0 错误 |
| Release 构建零警告零错误(Input 域) | `dotnet build src/DH2.Input/DH2.Input.csproj -c Release` | 0 警告 0 错误 |
| 格式门禁 | `dotnet format DH2.slnx --verify-no-changes` | exit 0 |
| 红线扫描 | `grep -r 'OpenProcess\|VirtualAllocEx\|CreateRemoteThread\|ReadProcessMemory\|WriteProcessMemory\|SetWindowsHookEx'` `src/DH2.Input/` `src/DH2.Capture/` | 仅 DH2.Input NativeMethods 注释中显式标注"不直接声明 OpenProcess";无匹配 API 声明 |

## 偏离与理由(相对技术设计)

1. **`OpenCvSharp4.Extensions` 包未列入 NuGet 表**:技术设计 §1 NuGet 表 Vision 列了 `OpenCvSharp4 + OpenCvSharp4.runtime.win`,未单列 `.Extensions`;§5.2 与 S1 审核报告均隐含 `OpenCvSharp.Extensions.BitmapConverter`(`Bitmap` ↔ `Mat` 互转)。Capture 层必须使用该转换(M0 Capture 边界要 Bitmap)。已在 `DH2.Capture.csproj` 加 `OpenCvSharp4.Extensions 4.10.0.20241107`,版本与 Core 已声明的 OpenCvSharp4 对齐。请求架构师在 M0-S2 收口时把该包并入 NuGet 总表(§5.2 引用应一并入表)。
2. **`System.Drawing.Size` vs `Core.Models.Size` 歧义**:Capture 同时引用 System.Drawing.Common(用于 Graphics/Bitmap)与 DH2.Core(用于 Size 模型),`Size` 类型歧义。已在 `GdiCapture.CopyFromScreen` 调用处显式 `new System.Drawing.Size(...)` 全限定。这是引用两个程序集时不可避免的命名冲突,不改名 Core.Size(M0 已签收)。
3. **文本/按钮文字字段未进 yaml**:`Core.MockLayoutConfig` 字段不含状态/按钮文本(只有几何与计时)。MockGame 内部以 `MockGameTexts` 静态常量固定 §7 标准值(待机/寻路中.../已到达目的地/前往/返回/师门任务 ({0}/20))。理由:①Core 字段约定不含文本;②文本与几何紧耦但变化频率不同,几何需配置化、文本可视为运行时字面量。架构师若希望文本也可配置,需扩 Core 字段集。
4. **`Avalonia.Fonts.Inter` 仍列于 MockGame csproj**:Dev A 合并版本保留。M0 MockGame 文本字重不挑剔,保留无副作用,但 S2 也未触及;可在后续 Sprint 清理。
5. **GdiCapture 异常处理**:捕获全部异常返回空帧(避免污染上游)。S1 偏离裁决 #3 已要求"窗口不可见/已销毁时返回可判别的空帧";本实现将"窗口可见但截屏失败"(设备丢失 / 异常分辨率)也归为空帧——同一语义,未单独区分。如需细分返回码,需 M0 后续扩展 `Frame` 或 `ActionResult`。
6. **未实施 S2 自动化冒烟**:`SAC2-1/SAC2-2` 的 IT-01/IT-02 需 Dev A 的 S2-4(CLI 集成)+ 桌面会话(同 SAC1-3 DEFERRED);Dev B 不写 `tests/`,验收由测试 Agent 在交互桌面执行。Dev B 域内的静态契约正确性已通过 build + 静态核对保证。

## 遗留问题

1. **MockGame 接入 Core 后的端到端冒烟**:SAC1-3 闭环条件绑定到 S2 的 IT-01/IT-04(QA 在交互桌面会话执行)。Dev B 的 RJ-S1-04 已通过 build 验证(几何来自 Core.Config.MockLayoutConfig),但运行时是否正确加载 yaml 并呈现,需 QA 在桌面会话跑 MockGame + 验证主窗口布局。
2. **`Avalonia.Fonts.Inter` 清理**:见偏离 §4。
3. **`Avalonia 12 → 11.2.7` 跨分支统一**:已在 S1 闭环(RJ-S1-02);S2 未引入新变更,M0-S2 当前 MockGame csproj 维持 11.2.7。
4. **GdiCapture 已知限制未写入 README**:技术设计 §4.1 末尾要求"写入代码注释与 README"。代码注释已写,但仓库根 README 未追加 M0 截屏限制段(架构师/用户在真机验证时需知晓)。Dev B 可在 S2 收口后追加,但 S2 卡不在我的域范围——请求架构师裁决是否由 Dev B 在 README 加段落。

## 当前状态声明

- Dev B:SPRINT M0-S2 我方 Stories(RJ-S1-04 + S2-1 + S2-2)完成,已推送 `dev-b/m0-s2`,待 QA 集成测试与 SAC1-3 闭环(交互桌面)。
- 范围合规:仅做 RJ-S1-04 与 S2-1、S2-2;未触碰 S2-3/S2-4(Dev A)、S3/S4(后续 Sprint)、`tests/`、docs/01~07 与迭代任务包、他人模块。
- 硬门禁:`dotnet build DH2.slnx -c Release` 0 警告 0 错误;`dotnet format DH2.slnx --verify-no-changes` exit 0。
- 待架构师 M0-S2 审核报告 + 放行指令(预计触发语"开始 M0-S3")前,Dev B 不开始下一 Sprint 任何工作。