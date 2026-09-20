# SPRINT M0-S2 任务完成报告(QA) — 第二轮: L2 部分回填 + 新增 DEF

- 测试Agent:qa-agent / 2026-09-21
- iter/m0 commit(本轮前):`36915f1`(第一轮骨架)
- iter/m0 commit(本报告):`TBD`(本提交落盘后)
- 环境:.NET SDK 10.0.103 / Windows 10.0.26200 x64 / **PowerShell(headless 会话)**
- 本报告位置:`docs/iterations/M0/reports/M0-S2-QA-任务完成报告.md`

## 集成记录(沿用首轮)

| 分支 | 集成方式 | 冲突 |
|---|---|---|
| `dev-b/m0-s2` (bf9080b) | `git merge --no-ff` → `a6021af` | 零 |
| `dev-a/m0-s2` (76f6d31) | `git merge --no-ff` → `7debe7a` | 零 |
| iter/m0 = `36915f1`(第一轮 QA 骨架 commit) | `qa: M0-S2 QA 骨架报告` | 零 |

## RJ-S1-03 / RJ-S1-05 复测(沿用首轮,均 PASS)

- RJ-S1-03:UT-06 恢复真实 YAML 解析 4 用例 + 边界 3 用例 + 几何 4 用例,**全绿**;MockLayoutLoader 覆盖 26.7% → **73.3%**
- RJ-S1-05:UT-04 恢复严格期望(`"not implemented in M0"` 消息),**全绿**;ConfigValidator 覆盖 94.9% → **100%**

## 用例执行矩阵

### L1 单元测试(沿用首轮,57 用例全绿)

`docs/iterations/M0/qa/evidence/M0-S2-l2/01-test.log` 与 `02-coverage.cobertura.xml`:
- 6 测试类 / **57 用例 / 全绿 / 0 失败 / 0 跳过**
- DH2.Core 行覆盖 **87.12%**,分支 86.66%(≥ 70% 门槛)
- 全 sln Release build:0 警告 0 错误
- `dotnet format --verify-no-changes`:exit 0

### L2 模拟窗口测试(**本轮部分回填**,3 项 FAIL + 2 项 NOT EXECUTED)

**回填证据目录**:`docs/iterations/M0/qa/evidence/M0-S2-l2/results/`

| 用例 | 期望 | 实际 | 状态 | 证据路径 |
|---|---|---|---|---|
| **IT-01** `dh2ctl enumerate` 列出 MockGame 窗口恰好 1 条(800×600) | 1 条 Title=DH2.MockGame,800×600 | **未执行**——用户未跑 enumerate | ❌ **NOT EXECUTED** | (缺 `03-enumerate.txt`) |
| **IT-02** `dh2ctl capture --count 10` 输出 10 张 PNG,非黑帧,耗时统计 | 10 张 800×600 PNG,mean/p95 | **未执行**——用户未跑 capture | ❌ **NOT EXECUTED** | (缺 `04-capture.txt` + `04-capture-pngs/*.png`) |
| **SAC1-3** MockGame 走查:启动 → 点"前往"→ Pathfinding → Arrived → 点"返回"→ Idle(counter+1);`\|raw\|` 与 `\|ui\|` 双行 | 状态机循环 + 双路日志 + counter+1 | 见下方诊断,**FAIL** | ❌ **FAIL** | `02-state-initial.json` / `05a-window-geom.txt` / `05b-state-after-go.json` / `05c-messages-after-go.log` / `05d-state-final.json` |

#### SAC1-3 失败诊断

**期望路径**:Idle →(点"前往")→ Pathfinding(2s)→ Arrived →(点"返回")→ Idle(counter+1)

**实际路径**:全程**未离开 Idle**(state="Idle",counter=0 自始不变)

**关键证据**:`05c-messages-after-go.log`(mockgame 子类化钩子捕获的窗口原始消息):

```
1789945768450|raw|0x0201|x=130|y=309   ← WM_LBUTTONDOWN,客户区 (130,309)
1789945768476|raw|0x0202|x=133|y=314   ← WM_LBUTTONUP
...                                          ← 大量 WM_MOUSEMOVE (0x0200) 拖动痕迹
```

| 维度 | 期望 | 实际 | 偏差 |
|---|---|---|---|
| LBUTTONDOWN 客户区坐标 | (86, 204) | (130, 309) | X+44, Y+105 |
| `\|raw\|` 行计数 | ≥ 1 | 1(LBUTTONDOWN+LBUTTONUP+大量 MOUSEMOVE) | OK |
| `\|ui\|` 行计数(Avalonia Pointer 事件) | ≥ 1 | **0** | 严重缺失 |
| 状态机转移 | Idle → Pathfinding | **未发生**(State 始终 Idle) | — |
| counter 变化 | +1 | 0 | 未递增 |

**点击位置偏差根因分析**(待用户确认):
- 假设 100% 缩放下,期望按钮中心为客户区 (86, 204),与 mock-layout.yaml 真值一致(`window.geometry` taskbar/button 几何)
- 实际点击落在客户区 (130, 309),偏移显著
- 可能成因(优先级排序):
  1. **DPI 缩放非 100%**:即便用户感知 100%,Windows 实际可能有 PerMonitorV2 + 应用级覆盖;若 DPI > 100%,**DIP 坐标 ≠ 像素坐标**,用户在像素空间看到的按钮位置与 Avalonia hit-test 的 DIP 坐标不一致 → click 命中像素按钮但未命中 DIP 按钮 → Avalonia 不触发 Pointer 事件
  2. 用户误判点击位置(可能把"按钮边缘"误认为中心)
  3. MockGame 实际渲染位置与 mock-layout.yaml 不符(布局 bug)
- `05a-window-geom.txt` 中 `physical=(510,357)` 与 `origin + clickAppSpace` 之和不一致(`453+86=539 ≠ 510`,`221+204=425 ≠ 357`)——佐证 DPI 转换链路有问题

## 缺陷清单

### S1 遗留(本轮确认已关闭)

| DEF | S2 处置 | 结果 |
|---|---|---|
| **DEF-S1-01** MockLayout 族 YamlDotNet 反序列化缺陷 | ✅ RJ-S1-03 修复落地 | ✅ **CLOSED** |
| **DEF-S1-02** ConfigValidator foreground 分支不可达 + 消息误导 | ✅ RJ-S1-05 修复落地 | ✅ **CLOSED** |

### S2 新增(本轮)

| DEF | 摘要 | 复现步骤 | 期望 | 实际 | 证据 | 指派 | 状态 |
|---|---|---|---|---|---|---|---|
| **DEF-S2-01** | IT-01 `dh2ctl enumerate` 未执行 | 用户执行桌面走查手册时**未跑步骤 3**(命令未在用户终端运行,无 `03-enumerate.txt`) | `dh2ctl enumerate --config configs\dev.yaml` 退出码 0,输出 1 条 800×600 窗口记录 | **未产出证据** | (无 `03-enumerate.txt`) | **QA 兜底**(让用户在下次走查时补) | **OPEN** |
| **DEF-S2-02** | IT-02 `dh2ctl capture --count 10` 未执行 | 用户执行桌面走查手册时**未跑步骤 4** | `dh2ctl capture --hwnd <n> --count 10 --interval-ms 500 --out <dir>` 退出码 0,产出 10 张 800×600 PNG + mean/p95 统计 | **未产出证据** | (无 `04-capture.txt` + `04-capture-pngs/`) | **QA 兜底** | **OPEN** |
| **DEF-S2-03** | SAC1-3 MockGame 走查 **FAIL** | 用户桌面会话点"前往"按钮:mockgame 子类化钩子捕获 `\|raw\|0x0201\|x=130\|y=309`,但按钮真值中心为 (86, 204);`\|ui\|` 行 0 个;state.json 始终 `Idle`,counter=0 | 1. `\|raw\|0x0201\|x=86\|y=204` ±1px;2. `\|ui\|` 行 ≥ 1;3. state.json `Idle` → `Pathfinding` → `Arrived`;4. 二次点击回 `Idle` + counter=1 | 1. 偏差 (X+44, Y+105);2. **0 个 `\|ui\|`**;3. **state 未转移**;4. **counter 未变** | `05c-messages-after-go.log` + `05b-state-after-go.json` + `05d-state-final.json` | **Dev A**(Avalonia/MockGame 渲染 / 坐标 / DPI)+ **Dev B**(MockLayout 应用)+ **QA**(用户代跑重试) | **OPEN**(硬阻塞:SAC1-3 闭环是 S2 收口前置) |

#### DEF-S2-03 处置优先级建议

- **首先**(最可能):用户在桌面会话以**显式屏幕坐标**重试点击(`origin.x + clickAppSpace.x` + DPI 校正);重跑前在 PowerShell 用 `[System.Windows.Forms.Screen]::PrimaryScreen.Bounds` 与 `Win32 API GetDpiForWindow` 校验实际缩放;若 DPI ≠ 100%,先调整至 100% 再跑
- **其次**:若确认 DPI = 100% 仍偏差,可能是 MockGame 实际渲染位置与 mock-layout.yaml 不一致——需 Dev A 检查 MainWindow.axaml 与 MockLayoutLoader 接线、Dev B 检查 StateStore 是否被点击调用
- **不要**静默接受"差几像素没事";测试设计 §2 明确 `<1px` 误差(AC-07),`>40px` 偏差属完全偏离

## 资产生成记录

- 本轮新增 5 份用户回填证据(`results/` 目录)
- 累计 5 份 L2 证据(已落地 iter/m0 的 docs/iterations/M0/qa/evidence/M0-S2-l2/results/)
- 仍未生成:S3 才需要的模板 PNG 与金样本(`mock_taskbar` / `mock_btn_go` / `mock_btn_return` + `tests/golden/screenshots/mock/idle.png`)

## 覆盖率

- DH2.Core **87.12% 行 / 86.66% 分支**(≥ 70% 门槛)
- 详见 `docs/iterations/M0/qa/evidence/M0-S2-l2/02-coverage.cobertura.xml`

## 结论

| SAC | 状态 |
|---|---|
| **SAC2-3** CLI 行为(退出码 / 配置校验聚合 / `--config` 缺省) | ✅ PASS |
| **SAC2-4** build/test/format 全绿,无越界 | ✅ PASS |
| **SAC2-1** IT-01 enumerate | ❌ NOT EXECUTED(DEF-S2-01) |
| **SAC2-2** IT-02 capture | ❌ NOT EXECUTED(DEF-S2-02) |
| **SAC1-3** MockGame 走查 | ❌ FAIL(DEF-S2-03,硬阻塞) |

**Sprint M0-S2 综合判定**:**L2 三项用例全部 FAIL / NOT EXECUTED**——按架构师 S1 审核报告 §4 与 S2 放行指令第 ② 条,**S2 不通过**。

**三报告齐备性**:**未达齐备**——本报告与 DevA/DevB 报告已在 iter/m0,但 S2 L2 收口条件未满足(SAC1-3 + IT-01 + IT-02 均需重跑)。

## 下一步

1. **QA Agent**:开放 DEF-S2-01/02/03,推送本报告与 5 份回填证据至 iter/m0,继续守候
2. **用户**:在桌面会话按 **DPI = 100% 严格校正** 后重跑桌面走查手册(可参考本报告 §"DEF-S2-03 处置优先级建议");特别注意步骤 3 与 4(IT-01/02 自动化命令)与步骤 5(SAC1-3)
3. **Dev A**(如需):协助排查 MockGame 渲染坐标是否与 mock-layout.yaml 一致
4. **架构师**:在用户回填新证据后,复核 L2 是否真正闭环 → 再决定 S2 签收

> ⚠️ **不声明"M0-S2 三报告齐备"**;S2 收口条件(SAC1-3 + IT-01 + IT-02 真实 PASS)未满足。
> ⚠️ cron `M0-S2-l2-evidence-watch` **保留**,继续每 10 分钟巡检 `results/` 目录。