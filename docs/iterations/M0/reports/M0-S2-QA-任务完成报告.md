# SPRINT M0-S2 任务完成报告(QA)— 第三轮:与架构师 S2 审核对齐

- 测试Agent:qa-agent / 2026-09-21
- iter/m0 commit(本轮前):`61a3e07`(QA 第二轮:DEF-S2-01/02/03)
- iter/m0 commit(本报告):`TBD`(本提交落盘后)
- 关键参考:`docs/iterations/M0/reports/M0-S2-架构师审核报告.md`(commit `e8fdbac`,CHANGES_REQUIRED)
- 环境:.NET SDK 10.0.103 / Windows 10.0.26200 x64 / PowerShell(headless)

## 集成记录(沿用首轮)

| 分支 | 集成方式 | 冲突 |
|---|---|---|
| `dev-b/m0-s2` (bf9080b) | `git merge --no-ff` → `a6021af` | 零 |
| `dev-a/m0-s2` (76f6d31) | `git merge --no-ff` → `7debe7a` | 零 |
| iter/m0 = `61a3e07`(本轮前 QA 第二轮) | `qa: M0-S2 L2 部分回填——开 DEF-S2-01/02/03` | 零 |

## 架构师 S2 审核校正事实(本轮关键更新)

| 项 | QA 第二轮结论 | 架构师 S2 审核结论 | 校正 |
|---|---|---|---|
| IT-01/IT-02 失败根因 | "用户未执行"(DEF-S2-01/02 归 QA 兜底) | **DevConfig 族 YamlDotNet 反序列化缺陷**(DEF-S2-01 指派 Dev A)——架构师亲自跑,dh2ctl 解析 `configs/dev.yaml` 即崩,故 IT-01/02 不可执行 | **撤回 QA 的 DEF-S2-01/02**,改用架构师 DEF-S2-01 |
| SAC1-3 状态 | "全程未离开 Idle / click 偏差 (X+44, Y+105) / 0 个 \|ui\| 行" | **正向转移 ✅,返回转移 ❌(150% 缩放环境)**:raw 与 ui 双路日志齐;raw 坐标被 DPI × 1.5 虚拟化;二次合成注入无法稳定命中 | 撤回我"click 完全偏离"的过度判断;保留"二次点击因 DPI 失败"为根因 |
| SAC1-3 闭环条件 | "需重跑" | **修订为 S4 e2e**(`dh2ctl click` 用 PostMessage 直接投递客户区坐标,确定性路径);100% 缩放人工冒烟降级为可选 | 本轮不算 FAIL,改为 DEFERRED→S4 |

**架构师 S2 审核完整引述**:`docs/iterations/M0/reports/M0-S2-架构师审核报告.md`(commit `e8fdbac`)

## RJ-S1-03 / RJ-S1-05 复测(沿用首轮,均 PASS)

- RJ-S1-03:UT-06 恢复真实 YAML 解析 4 用例 + 边界 3 用例 + 几何 4 用例,**全绿**;MockLayoutLoader 覆盖 26.7% → **73.3%**
- RJ-S1-05:UT-04 恢复严格期望(`"not implemented in M0"` 消息),**全绿**;ConfigValidator 覆盖 94.9% → **100%**

## 用例执行矩阵

### L1 单元测试(沿用首轮,57 用例全绿)

- `tests/DH2.Tests/Unit/` 6 文件 / **57 用例 / 全绿 / 0 失败 / 0 跳过**
- DH2.Core 行覆盖 **87.12%**,分支 86.66%(≥ 70% 门槛)
- 全 sln Release build:**0 警告 0 错误**
- `dotnet format --verify-no-changes`:exit 0
- 证据:`docs/iterations/M0/qa/evidence/M0-S2-l2/01-test.log` + `02-coverage.cobertura.xml` + `00-build.log` + `03-format.log`

### L2 模拟窗口测试(架构师代跑,5 份证据已在 iter/m0)

**证据目录**:`docs/iterations/M0/qa/evidence/M0-S2-l2/results/`(架构师提交,commit `e8fdbac`)

| 用例 | 期望 | 实际(架构师代跑,150% 缩放) | 状态 |
|---|---|---|---|
| **IT-01** `dh2ctl enumerate` | 退出码 0,1 条 800×600 窗口 | **dh2ctl 命令解析 `configs/dev.yaml` 即抛 `InvalidDataException`**(DEF-S2-01 YamlDotNet bug) | ❌ **BLOCKED** by DEF-S2-01(RJ-S2-01 修复后重跑) |
| **IT-02** `dh2ctl capture --count 10` | 退出码 0,10 张 800×600 PNG + mean/p95 | 同上,命令解析阶段即崩 | ❌ **BLOCKED** by DEF-S2-01(RJ-S2-01 修复后重跑) |
| **SAC1-3** MockGame 走查 | Idle → 点"前往" → Pathfinding → Arrived → 点"返回" → Idle + counter+1;raw 与 ui 双行 | ① 窗口发现/800×600 ✅;② state.json 原子写 ✅;③ 双路日志齐(`\|raw\|` 0x200/0x201/0x202 + `\|ui\|` PointerPressed/Released)✅;④ 正向转移 Idle→Pathfinding→Arrived ✅;⑤ **返回转移 ❌**(150% 缩放 × 1.5 虚拟化坐标;二次合成注入无法稳定命中;两次含 1.5 系数修正均失败) | ⚠️ **正向 PASS / 返回 DEFERRED→S4 e2e** |

**SAC1-3 闭环修订**(架构师裁决 §4):

> "SAC1-3 的'点击→转移'剩余部分,合并到 S4 的 IT-04/e2e 闭环——S4 的 `dh2ctl click` 用 PostMessage 直接投递客户区坐标(带 lParam,无光标、无 DPI 换算),对该断言是确定性路径,比 100% 缩放人工冒烟更强。"
>
> "原'S2 不通过则 SAC1-3 未闭环'条件,修订为:**SAC1-3 必须在 S4 审核前闭环(e2e 方式),否则 S4 不通过**。100% 缩放人工冒烟降级为可选补充。"

## 缺陷清单

### S1 遗留(均已关闭)

| DEF | 处置 | 结果 |
|---|---|---|
| DEF-S1-01 | RJ-S1-03 | ✅ **CLOSED** |
| DEF-S1-02 | RJ-S1-05 | ✅ **CLOSED** |

### S2 新增(以架构师 DEF-S2-01 为权威,我此前误开的 DEF-S2-01/02/03 已撤回)

| DEF | 摘要 | 根因 | 阻塞范围 | 指派 | 状态 |
|---|---|---|---|---|---|
| **DEF-S2-01** | DevConfig 族 YamlDotNet 反序列化缺陷 | `DevConfig` / `WindowTargetConfig` / `PathConfig` / `MatchingConfig` / `InputConfig` 仍为 init-only/位置参数 record(同 S1 DEF-S1-01 同类,RJ-S1-03 修了 MockLayout 族但漏了 DevConfig 族) | **阻塞 IT-01/IT-02**(dh2ctl 解析 `configs/dev.yaml` 即崩);间接影响 S4 的 e2e(若不修,S4 启动即失败) | **Dev A** | **OPEN**,整改指令 **RJ-S2-01**:①按 RJ-S1-03 先例改无参构造 class(settable);②**新增回归 UT:直接解析 `configs/dev.yaml` 与 `configs/mock-layout.yaml`**;③全量 build/test/format 绿后推送,commit 注明 `fix: RJ-S2-01` |

**我此前误开的 DEF 撤回声明**:

- 此前 QA 报告误标"DEF-S2-01(IT-01 未执行)、DEF-S2-02(IT-02 未执行)、DEF-S2-03(SAC1-3 失败)"——与架构师裁决书 §3 的 DEF-S2-01(DevConfig YamlDotNet)冲突
- 撤回理由:用户/架构师已实际执行 IT-01/IT-02,但被 DEF-S2-01 阻塞而无法产出证据;SAC1-3 正向已 PASS,返回转移的失败由架构师在 150% 缩放下代跑发现并归因为"DPI 虚拟化"→ 修订闭环条件至 S4 e2e
- 新增 DEF 编号以架构师为准;我的观察降级为"SAC1-3 失败诊断参考"写入 §"补充观察"

### 补充观察(非新 DEF)

- 桌面走查手册中"raw 坐标 = 注入坐标 × 1.5"说明:Avalonia 在 150% DPI 下,鼠标消息 lParam 与像素不同——日志坐标为**应用逻辑坐标(DIP)**,非物理像素。README 应加一句警示(架构师非阻塞记录 #1)。
- 桌面走查手册补充"结束确认 DH2.MockGame 进程已退出"步骤(架构师非阻塞记录 #2):避免孤儿进程残留污染下次跑。

## 资产生成记录

- L2 证据 5 份(`results/` 下,架构师代跑产生,已入 iter/m0 `e8fdbac`)
- 累计 evidence:`00-build.log` + `01-test.log` + `02-coverage.cobertura.xml` + `03-format.log` + `M0-S2-L2-桌面走查手册.md` + `results/02..05d`
- 仍未生成:S3 才需要的模板 PNG 与金样本

## 覆盖率

- DH2.Core **87.12% 行 / 86.66% 分支**(≥ 70% 门槛)
- DH2.Input / DH2.Capture 0% 符合预期(Win32/GDI 薄层,由 IT-01/02 集成覆盖)——**S2 的 IT-01/02 当前被 DEF-S2-01 阻塞**

## 结论

| SAC | 状态 |
|---|---|
| **SAC2-3** CLI 行为(退出码 / 配置校验聚合 / `--config` 缺省) | ✅ PASS |
| **SAC2-4** build/test/format 全绿,无越界 | ✅ PASS |
| **SAC2-1** IT-01 enumerate | ❌ BLOCKED by DEF-S2-01 |
| **SAC2-2** IT-02 capture | ❌ BLOCKED by DEF-S2-01 |
| **SAC1-3** MockGame 走查 | ⚠️ 正向 PASS / 返回 DEFERRED→S4 e2e(架构师闭环条件修订) |

**架构师 S2 审核结论**:**CHANGES_REQUIRED**(commit `e8fdbac`)
- 唯一阻塞项:DEF-S2-01(RJ-S2-01)
- 修复推送 + QA 复测后,架构师对 S2 快速复审(仅核对 RJ-S2-01 与 IT-01/02),通过即签发 S3 放行
- Dev B 本轮无整改项

**Sprint M0-S2 三报告齐备性**:**当前 3/3 报告已就位**(DevA `76f6d31` + DevB `bf9080b` + QA `61a3e07` 与本报告)——但 **S2 收口条件未满足**(DEF-S2-01 未修复,IT-01/02 未真实通过)。

## 下一步

1. **Dev A**:在 `dev-a/m0-s2` 追加 `fix: RJ-S2-01` 提交(DevConfig 族改 class + 回归 UT 解析仓库真实 yaml + 全量 build/test/format 绿)
2. **QA Agent(本轮后)**:守候 Dev A push → 合并到 iter/m0 → 复跑全测试 → **真实执行 IT-01/IT-02**(现在 DevConfig 可加载 dh2ctl 可用)→ 把结果补入本报告的 L2 表格 → push iter/m0 → 二次 commit `qa: M0-S2 RJ-S2-01 复测 + IT-01/02 补完`
3. **架构师**:快速复审(仅核对 RJ-S2-01 + IT-01/02),通过即签发 S3 放行指令

> ⚠️ **不声明"M0-S2 三报告齐备"**——DEF-S2-01 未修复,IT-01/02 未真实 PASS,S2 收口条件未满足。
> ⚠️ cron `M0-S2-l2-evidence-watch` **更新为守候 Dev A 推 RJ-S2-01 后再次重跑 IT-01/02**。