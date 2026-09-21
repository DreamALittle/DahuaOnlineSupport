# ITER-M0 实施自检报告

- **开发 Agent**:M0 实施团队(Dev A 平台与 CLI 组 / Dev B 模拟器与交互组 / 测试 Agent QA + 集成)
- **日期**:2026-09-21
- **commit hash**(本机开发分支基线):`origin/iter/m0` @ `95b7680`(S3 终审 PASS)
- **环境**:.NET SDK 10.0.103 / Windows 11(10.0.26200)/ PowerShell 5.1
- **模板依据**:03 §3 交付物契约;AC 来源:ITER-M0-任务书 §4(AC-01~AC-10,跨 Sprint SAC 汇总)

> 本报告汇总 M0 实施期内双开发(S1~S4 全部 Story)与 QA(IT-01~IT-07、L1/L2)的交付结果,按 10 条 AC 逐条核对。
> AC-09(`[真机]` PostMessage 路径裁决)按架构师 S2 第三轮裁定与用户真机验证环境约束,**标注"待用户执行"**。

---

## 1. 验收标准自查

| AC | 标准 | 结果 | 证据(文件/日志/命令输出路径) |
|---|---|---|---|
| **AC-01** | 从远端仓库 clone 开发:`iter/m0` 分支分批提交、提交信息符合约定式、分支推送至远端 | ✅ 通过 | `git log origin/iter/m0 --oneline` 共 60+ 提交,均约定式(`feat:` / `fix:` / `chore:` / `docs:`);四条 dev-a 分支(`dev-a/m0-s1` ~ `dev-a/m0-s4`)全部已推送 origin;QA 滚动合并 4 次合并 commit 入 `iter/m0`(`ccdee51` / `7d586e9` / `a6021af` / `412d359` 等) |
| **AC-02** | `dotnet build -c Release` 全解决方案零警告零错误 | ✅ 通过 | `dotnet build DH2.slnx -c Release` → 7 项目全部构建,**0 警告 0 错误**(S4 终态) |
| **AC-03** | `dotnet test` 全绿;纯逻辑类行覆盖 ≥70% | ✅ 通过 | `dotnet test` → 92/92 全绿(基线 54 + RJ-S1-03 2 + RJ-S2-01 2 + RJ-S2-02 5 + RJ-S3-01 4 + 现有 M0 回归 25);覆盖率依据 `docs/iterations/M0/qa/evidence/M0-S3-coverage/`:Core 96.52%(S3 终审实测);M0 覆盖纯逻辑类(`Win32Coord` / `Polling.WaitUntilAsync` / `ConfigValidator` / `MockLayoutLoader` / `ManifestWriter` / `TemplateStore` / `TemplateMatcher` / `Frame.Width/Height` 缓存等) |
| **AC-04** | `dh2ctl enumerate` 按 dev.yaml 过滤,正确列出 MockGame 窗口 | ✅ 通过 | IT-01 真机通过(`S2-架构师审核报告-第三轮终审` §3 已验证);`M0-S2 QA 任务完成报告` §1 IT-01:enumerate 恰好 1 行 MockGame;**架构师真机带屏缩放 150%** 物理像素 1200×900(@100% 即 800×600),150% 偏差按测试设计 §2 不作缺陷 |
| **AC-05** | `dh2ctl capture --count 10` 输出 10 张 PNG,尺寸正确、非空、耗时统计 | ✅ 通过 | IT-02 真机全量执行(架构师 S2 终审 §3 验证):10 帧全非黑,尺寸正确,统计输出;均值/p95 见 `qa/evidence/M0-S2-l2/results/` |
| **AC-06** | `dh2ctl save-template` 生成模板 + manifest 登记;`dh2ctl match` 定位任务栏中心点误差 ≤2px | ✅ 通过 | IT-03 真机终验(S3 终审 §3):`taskbar` 0px、`btn_go` 0px、`btn_return` 4px(焦点装饰,附架构约定);RJ-S3-01 manifest 大小写接缝修复后,save→match 全链路闭环 |
| **AC-07** | `dh2ctl click` 点击按钮中心,MockGame 消息日志收到 WM_LBUTTONDOWN/UP,坐标误差 ≤1px | ⚠️ 部分依赖 | S4-2 click 命令实现已就绪(本分支);完整 IT-04/IT-07 真机验证因 S4-1 PostMessageDriver 由 Dev B 负责,**当前依赖其 push → QA 合并 → 本分支 rebase 接线**。RJ-S2-03 已在 Dev B 分支修复 GdiCapture Mat 生命周期(共享内存 → Clone 独立副本),保证 raw 坐标与点击坐标一致性 |
| **AC-08** | `dh2ctl e2e --target mock` 全自动闭环 PASS | ⚠️ 部分依赖 | S4-3 e2e 由 Dev B 实现(依赖 S4-1 PostMessageDriver);S2 终审已确认 SAC1-3 闭环条件绑定 S4 e2e(架构师代跑实录:正向转移 ✅,返回转移因 150% 缩放 + 焦点装饰 4px 偏差 → S4 真机闭合) |
| **AC-09** | `[真机]` 用户按测试设计第 4 节执行真机验证并回填报告;架构师裁决执行层路线 | ⏳ **待用户执行** | V1~V6 步骤(见 `ITER-M0-测试设计.md` §4):enumerate / capture / save-template / match / click(角色寻路移动断言);报告回填模板 `docs/iterations/M0/ITER-M0-真机验证报告.md` 已就绪;`dh2ctl report --target game`(S4-4 真机验证向导)输出 V1~V6 操作指引 |
| **AC-10** | 根 README 含:环境要求、构建命令、六个 CLI 命令示例与输出说明 | ✅ 通过 | `README.md`:含 §1 环境要求(.NET 10 SDK / Windows / 100% 缩放 / DPI PerMonitorV2);§3 构建(`dotnet build -c Release DH2.slnx`、`dotnet format --verify-no-changes`);§4 运行说明(各子命令 Sprint 节奏渐进接入,enumerate / capture / save-template / match 在 S2/S3 已 ✓;click / e2e / report 在 S4);§5 开发流程摘要;§7 NuGet 依赖 |

---

## 2. build & test 结果(S4 终态)

### `dotnet build DH2.slnx -c Release`

```
DH2.Core -> .../release/DH2.Core.dll
DH2.Input -> .../release/DH2.Input.dll
DH2.Capture -> .../release/DH2.Capture.dll
DH2.Vision -> .../release/DH2.Vision.dll
DH2.MockGame -> .../release/DH2.MockGame.dll
DH2.App -> .../release/dh2ctl.dll
DH2.Tests -> .../release/DH2.Tests.dll
已成功生成。0 个警告,0 个错误。
```

### `dotnet test DH2.slnx -c Release`

```
已通过! - 失败: 0,通过: 92,已跳过: 0,总计: 92
```

### `dotnet format DH2.slnx --verify-no-changes`

```
EXITCODE=0
```

### 覆盖率(S3 终审实测,引用 `qa/evidence/M0-S3-coverage/`)

| 项目 | 行覆盖 |
|---|---|
| DH2.Core(纯逻辑类合并) | **96.52%**(≥70% 阈值) |
| DH2.Vision(纯逻辑部分) | 覆盖至 S3 终审(具体数字见证据目录) |

---

## 3. 相对技术设计的偏离与理由

按 Sprint 收口报告汇总(M0-S1/S2/S3/S4-DevA 报告各自 "偏离与理由" 章节),本节列出 **Dev A 名下** 的全部偏离;Dev B/QA 偏离见各自报告。

| # | 偏离 | 位置 | 理由 | 架构师裁决 |
|---|---|---|---|---|
| D-01 | `Frame.Image` 字段类型由 `Bitmap` 改为 `OpenCvSharp.Mat` | `DH2.Core/Models/Frame.cs` | Core 不引用 `System.Drawing.Common`;Capture/Vision 边界用 `BitmapConverter` 转换(M0-S1 偏离 #1) | 接受;§2.1 已同步修订 |
| D-02 | `MockLayoutConfig` 全字段默认值 + 类(非 record) | `DH2.Core/Config/MockLayoutConfig.cs` | RJ-S1-03 修复(避开 YamlDotNet `sealed record` 缺无参构造缺陷);默认值与 §7 表格同源 | 接受 |
| D-03 | `IFrameCapture.Capture(long hwnd)` 返回非空 `Frame`(裁决 #3) | `DH2.Core/Contracts/IFrameCapture.cs` | 窗口不可见/已销毁返回可判别空帧(避免 null 检查);GdiCapture 返回 `new Mat()` | 接受 |
| D-04 | `ConfigValidator` foreground 校验前置 + 消息严格化 | `DH2.Core/Config/ConfigValidator.cs` | RJ-S1-05 修复(原 elif 不可达 + 消息误导);`SupportedDrivers` 增列 foreground;`"foreground driver not implemented in M0; use 'background'"` | 接受 |
| D-05 | `Win32Coord.FromLParam` 按无符号位模式解析 | `DH2.Core/Util/Win32Coord.cs` | UT-01 要求 65535 满量程往返;`(short)0xFFFF = -1` 会丢数据 | 接受 |
| D-06 | `Polling.WaitUntilAsync` 谓词不接受 `CancellationToken` | `DH2.Core/Util/Polling.cs` | 按设计签名 `Func<Task<bool>>`;文档声明已知限制 | 接受 |
| D-07 | `DevConfig` 5 类型从 init-only / 位置 record 改为无参 class + settable | `DH2.Core/Config/*Config.cs` | RJ-S2-01 修复(补 RJ-S1-03 漏修);YamlDotNet 16 反序列化要求 | 接受 |
| D-08 | `Frame.Width/Height` 构造期 `init` 缓存 | `DH2.Core/Models/Frame.cs` | RJ-S2-02 修复(避免 Dispose 后 `core_Mat_cols` AV);空 Mat 缓存 0 安全 | 接受 |
| D-09 | `CaptureCommand` --count/--interval-ms 非法值 → usage error + 退出码 2 | `DH2.App/Commands/CaptureCommand.cs` | RJ-S2-04(S2 终审折叠整改);与 `--hwnd` 一致 | 接受 |
| D-10 | `ManifestWriter` 序列化端用 CamelCase;ManifestConfig / Entry / Offset DTO 与 Dev B `internal TemplateManifestConfig` schema 对齐 | `DH2.App/Commands/ManifestWriter.cs` | RJ-S3-01 修复(写读 schema 不一致导致 save→match 断裂);`internal` 不可跨程序集,自封最小 DTO | 接受 |
| D-11 | `tests/` 目录在 RJ-S1-03 / RJ-S2-01 / RJ-S2-02 / RJ-S3-01 四次明令下添加回归 UT | `tests/DH2.Tests/Unit/{Config,Models,Commands}/` | 架构师明令例外;Charter 红线对本 RJ 不适用 | 接受 |
| D-12 | `Directory.Packages.props` 启用 Central Package Management | 仓库根 | RJ-S1-03 引入;解决 YamlDotNet 16.1.1 vs 16.3.0 分裂 | 接受 |

---

## 4. 遗留问题与风险

### M0 范围内遗留(进入 M1a 前需关注)

1. **AC-07/AC-08 真机闭环**:依赖 Dev B 的 S4-1 (PostMessageDriver) 与 S4-3 (e2e) 推送 + QA 滚动合并到 iter/m0 + 本分支 rebase。本分支 `dev-a/m0-s4` 实现 S4-2 click 命令已就绪,等待依赖落地即可联调。
2. **AC-09 真机验证**:M0 出口关卡,需用户在真实游戏环境下回填 V1~V6。`dh2ctl report --target game`(S4-4)已就绪,向导输出 V1~V6 操作指引(分步命令示例与通过判据)。结论由架构师裁决执行层(PostMessage 维持 / 降级焦点轮转 / 截屏方案复议)。
3. **150% 缩放物理像素**:架构师 S2 代跑与 S3 终审均在 150% 缩放环境完成,`@100%` 即 800×600,`@150%` 即 1200×900;`btn_return` 4px 偏差附架构约定(焦点装饰);测试设计 §2 明确"非 100% 缩放坐标误差不作为缺陷"。
4. **NU1903 全局抑制**(Avalonia 传递依赖 `Tmds.DBus.Protocol` 0.20.0 高危漏洞):Windows 不受影响,跟踪上游修复后移除。
5. **Directory.Packages.props 启用 CPM**:Dev B 后续若新增包需走 `PackageVersion` 登记;本仓库 5 项目均已切换。

### M1a 进入时需重点关注

1. **WindowStartupLocation / PerMonitorV2**:dh2ctl 与 MockGame 均声明;真机游戏客户端未必配合,需在 M0 真机验证中确认。
2. **UIPI 静默丢消息**:若游戏以管理员权限运行而 dh2ctl 普通权限,PostMessage 会被丢弃;真机验证报告须记录权限组合。
3. **WGC 截屏(M1a)**:M0 GDI 在最小化 / 遮挡场景下失败,M1a 必须引入 Windows.Graphics.Capture 替换。

---

## 5. 交付物完整性核对(任务书 §3)

| # | 交付物 | 位置 | 状态 |
|---|---|---|---|
| 1 | 源代码 6+1 项目 | `src/DH2.{Core,Input,Capture,Vision,App}`,`tools/DH2.MockGame`,`tests/DH2.Tests` | ✅ |
| 2 | dh2ctl 可执行 CLI | `src/DH2.App` | ✅(7 子命令:enumerate / capture / save-template / match / click / e2e / report;e2e 由 Dev B) |
| 3 | MockGame v0 | `tools/DH2.MockGame` | ✅(S1-3 Dev B;RJ-S1-04 接 Core.MockLayoutLoader) |
| 4 | 配置样例 | `configs/dev.yaml` / `mock-layout.yaml` / `local/game.yaml.example` | ✅ |
| 5 | 初始模板 + manifest | `templates/mock_800x600/`(3 模板 + manifest) | ✅(S3-4 QA 真机生成) |
| 6 | 测试 + 金样本 | `tests/DH2.Tests/` + `tests/golden/screenshots/mock/` | ✅ |
| 7 | 工程规范载体 | `.gitignore` / `.editorconfig` / 根 `README.md` | ✅ |
| 8 | 实施自检报告(本文件) | `docs/iterations/M0/ITER-M0-实施自检报告.md` | ✅(本报告) |
| 9 | 远端仓库分支 | 4 条 dev 分支 + 4 份任务完成报告 + 1 份架构师审核报告 | ✅ |

---

## 6. 收尾下一步(本 M0 实施团队)

1. **QA**:将 `dev-a/m0-s4`(S4-2 click + S4-4 report + S4-5 本报告)+ `dev-b/m0-s4`(S4-1 PostMessageDriver + S4-3 e2e)滚动合并到 `iter/m0`;真机执行 IT-04/IT-05/IT-06/IT-07 完成 SAC4-1/4-2。
2. **架构师**:对照 S4 审核四点门禁(① build/test 复跑 ② 红线扫描 ③ 范围核对 ④ SAC 核对),出具 `M0-S4-架构师审核报告.md`。通过则 S4 签收,本 M0 整体进入收尾。
3. **用户**:按 `dh2ctl report --target game` 向导执行 V1~V6 真机验证,回填 `ITER-M0-真机验证报告.md`;架构师据此裁决执行层路线。
4. **架构师(执行层路线裁决后)**:执行 03 §4 完整七项门禁(汇总材料 = 各轮完成报告 + 审核报告 + 本实施自检报告);`iter/m0` 合入 `main`;发包 M1a。

---

> **声明**:本报告由 Dev A 汇总起草,涵盖 M0 实施期内全团队交付。AC-09 标注"待用户执行";AC-07/AC-08 部分依赖标注待 Dev B S4-1/S4-3 推送后本分支 rebase 完成联调。完整证据链见 `docs/iterations/M0/reports/` 下四份 Sprint 任务完成报告 + 四份架构师审核报告 + `qa/evidence/` 下各 Sprint 跑测 / 截图 / 真机实录。