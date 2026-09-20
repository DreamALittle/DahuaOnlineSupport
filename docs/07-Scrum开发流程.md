# DH2 Scrum 开发流程(Sprint/Story 执行细则)

03 文档定义迭代级总流程,本文细化到 Sprint/Story 级与团队编制。两者冲突时以 03 为准。
**核心纪律:一次只推进一个 Sprint;开发只做自己名下 Story;每轮以三份《任务完成报告》收口,架构师集中审查通过并签发放行指令后,才允许开始下一轮。架构师是每轮的收口终点与下一轮的开始起点。**

文档版本 v1.2(2026-09-20:轮次完成报告制 + 架构师收口签发制)

## 1. 角色映射与团队编制

| 角色 | 承担者 | 模块域 / 职责 |
|---|---|---|
| Product Owner | 用户 | 优先级决策、真机验证(AC-09 等 `[真机]` 项)、业务风险决策、指令中转 |
| Scrum Master / 架构与审核(总负责) | 架构师 Agent | 发卡、技术决策、**每轮集中审查(收口)与下一轮放行签发**、迭代七项门禁、流程把控 |
| **Dev A(平台与 CLI 组)** | 开发 Agent A | `DH2.Core`(模型/契约/配置/工具)、`DH2.App`(dh2ctl CLI)、未来 Annotator |
| **Dev B(模拟器与交互组)** | 开发 Agent B | `DH2.MockGame`、`DH2.Input`、`DH2.Capture`、`DH2.Vision` |
| **测试 Agent(QA + 集成)** | 测试 Agent | 全部测试编码与执行、测试资产与证据、`iter/m0` 集成分支维护、DEF 缺陷管理 |
| Product Backlog | 06-开发路线图 | 里程碑 → 迭代 |
| Sprint Backlog | `docs/iterations/{迭代号}/sprints/*.md`(含负责人列) | 架构师编写 |

## 2. 工作区与分支策略

| 参与者 | 工作区(各自独立 clone) | 分支 |
|---|---|---|
| 架构师 Agent | D:\Repos\DH2 | main(文档);iter/m0(审核报告等文档提交) |
| Dev A | D:\Repos\DH2-DevA | `dev-a/m0-s{n}` |
| Dev B | D:\Repos\DH2-DevB | `dev-b/m0-s{n}` |
| 测试 Agent | D:\Repos\DH2-QA | `iter/m0`(集成;测试 Agent 与架构师可推送) |

- 开发分支基于**最新 `origin/iter/m0`** 创建;开工前先 `git fetch` + rebase;
- **滚动集成**:Story 完成即推送自己的 dev 分支;测试 Agent 随时将其合并进 `iter/m0` 并跑构建冒烟;
- 冲突处理:项目文件类机械冲突(sln/csproj)QA 可自行解决并记录;逻辑冲突退回对应开发处理;
- `main` 只在迭代收尾、七项门禁通过后合入(架构师执行)。

## 3. 节奏与粒度

- **Sprint(轮次)** = 0.5~1 个工作日;迭代 = 2~5 个 Sprint,串行;
- **Story** = 半天内可完成、独立可构建的模块/功能单元,Story 卡标明负责人(Dev A / Dev B / 测试 Agent);
- 每笔 commit 对应一个 Story 或其一部分;**禁止实施自己名目之外的 Story 或后续 Sprint 内容**(越界=审核不通过,即使代码正确)。

## 4. 轮次生命周期(架构师收口制)

```
架构师发卡(含负责人),用户向三人转发"开始 M0-S{n}"
        │
        ▼
Dev A / Dev B 并行实施自己名下 Story(滚动推送,报告随分支提交)
        │
        ▼
测试 Agent:滚动合并进 iter/m0 → 测试编码与执行 → 证据/资产 → DEF 清单 → QA 完成报告
        │
        ▼  三份《任务完成报告》齐备并全部进入 iter/m0
架构师集中审查(收口):复跑 build/test · 红线快扫 · 范围核对 · SAC/AC 核对
        │
        ├─ 通过 → 出具审核报告 + 签发"下一轮放行指令"(用户转发给三人)──▶ 下一轮开始
        └─ 不通过 → 整改清单(指派到角色)返工 → 重新收口
```

1. **Sprint Planning(架构师)**:发放当期 Story 卡;用户以"开始 M0-S{n}"触发三位 Agent;
2. **实施(Dev A / Dev B)**:并行做自己名下 Story,小步提交、即完即推;**完成报告随自己的 dev 分支提交**;
3. **测试与集成(测试 Agent)**:滚动合并 + 冒烟;测试编码与执行;证据与资产;**QA 完成报告提交至 iter/m0**(三份报告由此全部进入 iter/m0);
4. **集中审查(架构师,收口)**——**三份完成报告齐备才启动**,否则退回补交:
   - ① `iter/m0` 上 `dotnet build` / `dotnet test` 复跑复现;
   - ② 红线 API 快扫(grep 清单同 03 §4 G1);
   - ③ 范围核对:无越界、无缺失(对照 Story 卡负责人列);
   - ④ SAC 逐条核对(以三份完成报告为证据基础);
   产出 `M0-S{n}-架构师审核报告.md`;**通过时在报告末尾签发下一轮放行指令**(统一触发语 + 本轮注意事项),由用户转发——未收到放行指令,任何人不得开始下一 Sprint。

## 5. DEF 缺陷协议

- 编号 `DEF-S{n}-NN`,由测试 Agent 创建:复现步骤、期望/实际、证据路径、指派(按模块域:Core/App→Dev A;MockGame/Input/Capture/Vision→Dev B;tests/→测试 Agent 自查);
- 修复:对应开发在自己分支修复,commit 注明 `fix: DEF-S{n}-NN`,推送后由 QA 复测关闭;
- 阻塞级缺陷(导致 SAC 无法判定)由架构师决定是否叫停 Sprint。

## 6. 轮次交付物:任务完成报告(统一命名)

位置:`docs/iterations/M0/reports/`,每轮每角色一份;**报告未提交=本轮未完成**。

| 角色 | 文件名 |
|---|---|
| Dev A | `M0-S{n}-DevA-任务完成报告.md` |
| Dev B | `M0-S{n}-DevB-任务完成报告.md` |
| 测试 Agent | `M0-S{n}-QA-任务完成报告.md`(含集成记录/用例矩阵/覆盖率/DEF/资产/结论) |
| 架构师(收口产物) | `M0-S{n}-架构师审核报告.md` |

**开发完成报告模板**(Dev A / Dev B):

```markdown
# SPRINT M0-S{n} 任务完成报告(Dev A / Dev B)
- Agent / 日期 / 分支与 commit 范围:
## 完成的 Story 与证据
| Story | 结果 | 证据(构建输出/文件路径) |
## SAC 相关自查(仅本组相关项)
| SAC | 结果 | 说明 |
## 偏离与理由(相对技术设计)
## 遗留问题
```

**QA 完成报告模板**:

```markdown
# SPRINT M0-S{n} 任务完成报告(QA)
- 测试Agent / 日期 / iter/m0 commit:
## 集成记录(合并了哪些 dev 分支、冲突处理、三份报告齐备性)
## 用例执行矩阵
| 用例 | 结果 | 证据路径 |
## 覆盖率(纯逻辑类行覆盖)
## 缺陷清单
| DEF | 摘要 | 指派 | 状态 |
## 资产生成记录(模板/金样本,如有)
## 结论(该 Sprint SAC 是否全部可判通过)
```

**架构师审核报告模板**(收口产物,签发放行):

```markdown
# SPRINT M0-S{n} 架构师审核报告
- 审核人 / 日期 / iter/m0 commit:
## 材料齐备性(三份完成报告核对)
## 四项门禁结论
| 门禁 | 结果 | 说明 |
## 整改项(如有)
| # | 级别 | 指派 | 问题 | 要求 |
## 结论:通过 / 不通过
## 下一轮放行指令(通过时)
- 统一触发语:`开始 M0-S{n+1}`
- 本轮特别注意事项:……
```

## 7. 迭代收尾

全部 Sprint 通过后:架构师执行 **03 §4 完整七项门禁**(汇总材料=各轮完成报告+审核报告+`ITER-M0-实施自检报告.md`)→ 用户执行 `[真机]` 项 → `iter/m0` 合入 `main` → 发包下一迭代。

## 8. 当前 Sprint 划分与负责人速览(M0)

| Sprint | Dev A | Dev B | 测试 Agent |
|---|---|---|---|
| S1 工程骨架与 MockGame | S1-1 骨架、S1-2 Core | S1-3 MockGame v0 | UT-01/02/04/06/08 编码执行、MockGame 冒烟 |
| S2 窗口发现与截屏 | S2-3 CLI 骨架、S2-4 命令集成 | S2-1 Input、S2-2 Capture | IT-01/02、CLI 行为用例 |
| S3 模板匹配与模板库 | S3-3 save-template/match 命令 | S3-1 Store、S3-2 Matcher | UT-03/05/07、IT-03、S3-4 资产生成 |
| S4 后台输入与 e2e | S4-2 click 命令、S4-4 report、S4-5 收尾汇总 | S4-1 驱动、S4-3 e2e | IT-04~07、覆盖率复核、证据归档 |

Story 卡见 `iterations/M0/sprints/`,严格串行 S1→S4。
