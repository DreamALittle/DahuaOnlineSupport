# SPRINT M0-S4 任务完成报告(QA) — M0 收口 + SAC1-3 闭环

- **测试 Agent**:qa-agent / 2026-09-21
- **iter/m0 commit(本轮前)**:`e451968`(S3 RJ-S3-01/02 复测)
- **iter/m0 commit(本报告)**:`1bf06d5`(滚动合并 dev-a/m0-s4 + dev-b/m0-s4 + PostMessageDriver 接缝适配 + S4 UT + 桌面走查手册)
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
| (无) | — | — | — |

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

**声明:QA 已完成 M0-S4 名下全部 Story(集成 dev-a/m0-s4 + dev-b/m0-s4 + S4 集成接缝修复 + IT-07 异常防御 24 UT + 桌面走查手册 + SAC1-3 闭环判定),iter/m0 HEAD `1bf06d5`,`dotnet build` 0 警 0 错,`dotnet test` 121/121 通过,`dotnet format --verify-no-changes` exit 0。Dev A / Dev B / QA 三份任务完成报告齐备,SAC1-3 闭环待架构师在桌面走查手册签字栏签字后即视为 M0 整体收口;等待架构师 M0 终审 + 启动 M1a 的指令。**
