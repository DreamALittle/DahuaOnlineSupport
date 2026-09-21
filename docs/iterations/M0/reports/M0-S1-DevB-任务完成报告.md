# SPRINT M0-S1 任务完成报告(Dev B)

- Agent:Dev B(模拟器与交互组)
- 日期:2026-09-20
- 分支:`dev-b/m0-s1`
- Commit 范围(`origin/iter/m0` → `dev-b/m0-s1`):
  - `dc9e828` chore(MockGame): 调整项目骨架 TFM net10.0-windows + DPI PerMonitorV2 + 警告门禁 + YamlDotNet
  - `1ee0eb8` feat(MockGame): mock-layout 配置加载层 + MockPhase/MockStatePayload 模型
  - `5bc8c7d` feat(MockGame): state.json 原子覆写 + 共享消息日志(raw|ui 同写一处)
  - `49f57de` feat(MockGame): SetWindowSubclass 原始消息日志(同进程自身 HWND,WM_LBUTTON/DOWN/UP 透明转发)
  - `354caf6` feat(MockGame): 状态机 Idle↔Pathfinding→Arrived(counter+) + MainViewModel
  - `ca0dcc3` feat(MockGame): 主窗口布局(任务栏/状态文本/主按钮) + 启动接线(子类化生命周期 + UI Pointer 日志 + 清理旧文件)

## 完成的 Story 与证据

| Story | 结果 | 证据(构建输出/文件路径) |
|-------|------|--------------------------|
| S1-3 MockGame v0(Avalonia:布局渲染/状态机/state.json 原子写/SetWindowSubclass+Pointer 双路日志) | 已完成,推送 dev-b/m0-s1,等 QA 集成进 iter/m0 | 源文件:`configs/mock-layout.yaml`、`tools/DH2.MockGame/`(csproj + app.manifest + AppHost.cs + Program.cs + App.axaml + App.axaml.cs + Models/{MockLayout,MockLayoutLoader,MockState}.cs + Services/{NativeMethods,StateStore,MessagesLog,RawMessageLogger,MockStateMachine}.cs + Views/{MainWindow.axaml,MainWindow.axaml.cs} + ViewModels/{MainViewModel,ViewModelBase}.cs)。构建产物:`tools/DH2.MockGame/bin/Release/net10.0-windows/DH2.MockGame.dll`(零警告零错误) |

## SAC 相关自查(仅本组相关项)

| SAC | 结果 | 说明 |
|-----|------|------|
| SAC1-1 `dotnet build -c Release` 全解决方案零警告零错误 | 部分通过(Dev B 域) | Dev A 的 S1-1 项目骨架(解决方案文件 sln + 6+1 项目)未推送;Dev B 仅能对 `tools/DH2.MockGame/DH2.MockGame.csproj` 单独执行 `dotnet build -c Release`,结果 0 警告 0 错误。等 QA 集成 sln 后由测试 Agent 复跑全量。 |
| SAC1-3 MockGame 手动冒烟(点击按钮触发状态机 + state.json 即时更新 + messages.log 同时出现 `|raw|` 与 `|ui|` 行) | 实现已就位,冒烟由测试 Agent 执行 | 自动化冒烟需要桌面会话(Avalonia GUI 进程);Dev B 不写 `tests/`,冒烟归测试 Agent(测试设计 §2 IT-04/IT-05/IT-06)。Dev B 已完成实现侧的代码自检,关键路径见下方代码自检表。 |
| SAC1-4 `dev-b/m0-s1` 已推送远端 | 通过 | 已 `git push -u origin dev-b/m0-s1`(远端 `dev-b/m0-s1` 已建);合并进 `iter/m0` 由测试 Agent 在 QA 集成阶段执行。 |

### 代码自检表(Dev B 实施期静态核对,非自动化断言)

| 关键能力 | 实现位置 | 静态核对结论 |
|----------|----------|--------------|
| 布局按 mock-layout.yaml 渲染 | `App.axaml.cs` + `Views/MainWindow.axaml` | XAML 坐标与 yaml 一致(16/16/320/88、16/116、16/180/140/48);窗口 800×600 来自 yaml.window;PerMonitorV2 由 app.manifest 声明 |
| 状态机 Idle→Pathfinding→Arrived→Idle(counter+) | `Services/MockStateMachine.cs` | Idle/Arrived 仅在 Button.Click 触发转移;Pathfinding 在 `Task.Delay(pathfindingMs)` 后由 `OnPathfindingElapsed` 转移 |
| state.json 原子写 | `Services/StateStore.cs` | 写临时文件 + `File.Replace`(目标已存在)/ `File.Move`(首次),`lock` 串行化;每次状态转移同步落盘 |
| SetWindowSubclass 原始消息日志 | `Services/NativeMethods.cs`(P/Invoke comctl32 #410/#412/#413)+ `Services/RawMessageLogger.cs` | 仅作用于调用方提供的单 HWND(同进程内,非全局钩子);钩子返回 `DefSubclassProc` 透明转发,不影响按钮 Click 触发 |
| Pointer 语义日志(100% 缩放下 DIP==像素) | `Views/MainWindow.axaml.cs` `OnWindowPointerPressed/Released` | 通过 `e.GetPosition(this)` 取 DIP;日志格式 `{unixms}\|ui\|PointerPressed\|x=…\|y=…` |
| messages.log 同时写 raw 与 ui | `Services/MessagesLog.cs`(raw+ui 共用 append,lock 串行) | 同一文件追加,行格式符合 §7 |
| 启动时清理旧日志/状态 | `AppHost` 构造内 `Store.ClearOnStartup() + Log.ClearOnStartup()` | 进程启动即清;首次启动 state.json 不存在则 Move 走首次创建路径 |

### 硬门禁验证(Dev B 域内)

| 项 | 命令 | 结果 |
|----|------|------|
| Release 构建零警告零错误 | `dotnet build tools/DH2.MockGame/DH2.MockGame.csproj -c Release` | 0 警告 0 错误 |
| 格式门禁 | `dotnet format tools/DH2.MockGame/DH2.MockGame.csproj --verify-no-changes` | exit 0 |

注:全 sln 构建与格式门禁需等 Dev A 推送 S1-1(解决方案骨架)后由 QA 复跑。

## 偏离与理由(相对技术设计)

1. **Avalonia 版本:12.1.2 vs §2 "Avalonia 11"**:`dotnet new avalonia.mvvm` 模板默认装的是 Avalonia 12.1.2(模板写定发布版)。Avalonia 12 是 Avalonia 11 的后续主版本,API 与 §2 技术选型描述基本兼容。若架构师坚持 11.x,需指明替换为 11.3.x 并调整 `x:DataType`/MVVM 源生成等微差异。请求架构师裁决。
2. **ViewLocator 删除**:模板默认的 `ViewLocator.cs` 通过反射按 `ViewModel → View` 命名约定查找视图;MockGame 是单窗口架构(只一个 `MainWindow` + 一个 `MainViewModel`),通过 `App.axaml.cs` 直接 `new MainWindow { DataContext = ... }` 装配更直接,无需反射。同时该文件 dotnet format 不通过(多余行尾空白)。已从 `App.axaml` 移除 `<local:ViewLocator />`。
3. **`StateStore.Path` 属性遮蔽 `System.IO.Path`**:暴露 `Path` 属性(只读)便于外部观察落盘路径,但构造函数内 `Path.Combine(directory, "state.json")` 被解析为属性。已用 `System.IO.Path.Combine` 全限定解决。属性名保留是为给后续日志/调试便利。
4. **`StateJsonContext` System.Text.Json 源生成**:`StateStore` 的 JSON 序列化使用源生成上下文(为未来 AOT/性能优化预留);M0 不是 AOT 场景,但源生成同时提供编译期类型检查,收益为正。
5. **子类化安装时机**:技术设计 §7 未明确"何时安装 SetWindowSubclass";Dev B 选择 `Window.Opened` 事件(此时 HWND 已就绪),`Window.Closing` 事件卸载(`RemoveWindowSubclass`)。Avalonia 文档建议 `Opened` 用于一次性初始化。
6. **DI 缺失下的服务装配**:MockGame 引入 `AppHost`(进程内静态单例)承载 `MockLayout`/`StateStore`/`MessagesLog`;Avalonia 11/12 无强 DI,Avalonia 13 才引入原生 DI。属"轻量妥协",不引入额外包。

## 遗留问题

1. **项目未加入 sln**:Dev A 的 S1-1(解决方案骨架)未推送,`tools/DH2.MockGame` 当前是独立 csproj,不被任何 sln 引用。`dotnet build DH2.sln` 在 QA 集成前无法执行。请求 QA 集成 `dev-b/m0-s1` 时把 `tools/DH2.MockGame/DH2.MockGame.csproj` 加入 sln(由 QA 在集成 commit 中处理,符合 07 §6 流程)。
2. **SAC1-3 完整冒烟需桌面会话**:点击 → 状态机转移 → state.json 落盘 + 双路日志同写 → 二次循环的端到端冒烟必须在桌面会话运行 MockGame 进程才能完成,Dev B 不写 `tests/`,该冒烟由测试 Agent 在测试设计 §2 IT-04/IT-05/IT-06 中执行。
3. **Avalonia 12 vs 11 裁决未到**:见"偏离"§1,需架构师在 M0-S1 架构师审核报告中给出结论;若必须降回 11.x,Dev B 在收到指令后单笔 commit 调整。
4. **100% 缩放前置条件未自动检测**:SAC1-3 假设 100% 缩放(DIP==像素)。MockGame 启动不主动检测系统缩放,失败由用户在 README 或测试前置条件中保证(技术设计 §10 陷阱#1/#2 文档责任)。

## 当前状态声明

- Dev B:SPRINT M0-S1 我方 Stories(S1-3)完成,已推送 `dev-b/m0-s1`,待 QA 集成测试与 S1-1/S1-2 接线(加入 sln、确认 Core MockLayout 是否需要引用——目前 MockGame 自读 mock-layout.yaml,与 Core MockLayout 解耦)。
- 严格遵守范围:S1-3 之外未触动任何他人域或后续 Sprint 内容(没有动 S1-1 的 sln、S1-2 的 Core、未写 `tests/`)。
- 硬门禁:`dotnet build -c Release` 零警告零错误(Dev B 域内);`dotnet format --verify-no-changes` 通过。
- 待架构师审核 + 放行指令"开始 M0-S2"前,Dev B 不开始下一 Sprint 任何工作。