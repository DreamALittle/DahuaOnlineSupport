# DH2 Scrum 开发流程(Sprint/Story 执行细则)

03 文档定义迭代级总流程,本文细化到 Sprint/Story 级与团队编制。两者冲突时以 03 为准。
**核心纪律:一次只推进一个 Sprint;开发只做自己名下 Story,完成即停;Sprint Review 通过才进入下一 Sprint。禁止一次性从头做到尾。**

文档版本 v1.1(2026-09-20:团队编制 2 开发 + 1 测试)

## 1. 角色映射与团队编制

| 角色 | 承担者 | 模块域 / 职责 |
|---|---|---|
| Product Owner | 用户 | 优先级决策、真机验证(AC-09 等 `[真机]` 项)、业务风险决策 |
| Scrum Master / 架构与审核 | 架构师 Agent | 发卡(Sprint 计划与负责人指派)、技术决策、Sprint Review、迭代七项门禁、流程把控 |
| **Dev A(平台与 CLI 组)** | 开发 Agent A | `DH2.Core`(模型/契约/配置/工具)、`DH2.App`(dh2ctl CLI)、未来 Annotator |
| **Dev B(模拟器与交互组)** | 开发 Agent B | `DH2.MockGame`、`DH2.Input`、`DH2.Capture`、`DH2.Vision` |
| **测试 Agent(QA + 集成)** | 测试 Agent | 全部测试编码与执行(按测试设计)、测试资产与证据、`iter/m0` 集成分支维护、DEF 缺陷管理 |
| Product Backlog | 06-开发路线图 | 里程碑 → 迭代 |
| Sprint Backlog | `docs/iterations/{迭代号}/sprints/*.md`(含负责人列) | 架构师编写 |

## 2. 工作区与分支策略

| 参与者 | 工作区(各自独立 clone) | 分支 |
|---|---|---|
| 架构师 Agent | D:\Repos\DH2 | main(文档) |
| Dev A | D:\Repos\DH2-DevA | `dev-a/m0-s{n}` |
| Dev B | D:\Repos\DH2-DevB | `dev-b/m0-s{n}` |
| 测试 Agent | D:\Repos\DH2-QA | `iter/m0`(集成,QA 独占推送) |

- 开发分支基于**最新 `origin/iter/m0`** 创建;开工前先 `git fetch` + rebase;
- **滚动集成**:Story 完成即推送自己的 dev 分支;测试 Agent 随时将其合并进 `iter/m0` 并跑构建冒烟;
- 冲突处理:项目文件类机械冲突(sln/csproj)QA 可自行解决并记录;逻辑冲突退回对应开发处理;
- `main` 只在迭代收尾、七项门禁通过后合入(架构师执行)。

## 3. 节奏与粒度

- **Sprint** = 0.5~1 个工作日;迭代 = 2~5 个 Sprint,串行;
- **Story** = 半天内可完成、独立可构建的模块/功能单元,Story 卡标明负责人(Dev A / Dev B / 测试 Agent);
- 每笔 commit 对应一个 Story 或其一部分;**禁止实施自己名目之外的 Story 或后续 Sprint 内容**(越界=审核不通过,即使代码正确)。

## 4. Sprint 生命周期(每轮)

```
架构师发卡(含负责人) ─▶ Dev A / Dev B 并行实施自己名下 Story(滚动推送)
                              │
                              ▼
                    测试 Agent:滚动合并进 iter/m0 → 测试编码与执行 → 证据/资产 → DEF 清单
                              │
                              ▼
                 架构师 Sprint Review(四项门禁,以 QA 报告为输入)
                       │通过                    │不通过
                       ▼                        ▼
                 发放下一 Sprint          整改清单返工(对应角色)
```

1. **Sprint Planning(架构师)**:发放当期 Story 卡(目标、Stories+负责人、技术设计章节引用、SAC、测试用例引用);用户以"开始 M0-S{n}"触发三位 Agent;
2. **实施(Dev A / Dev B)**:并行做自己名下 Story,小步提交、即完即推;
3. **测试与集成(测试 Agent)**:滚动合并 + 冒烟;按测试设计完成测试编码与执行;生成/更新证据与资产;输出缺陷清单(DEF);
4. **Sprint Review(架构师)**,轻量四项门禁:
   - ① `iter/m0` 上 `dotnet build` / `dotnet test` 复跑复现;
   - ② 红线 API 快扫(grep 清单同 03 §4 G1);
   - ③ 范围核对:无越界、无缺失(对照 Story 卡负责人列);
   - ④ SAC 逐条核对(以 QA 测试报告与双开发简报为证据);
   结论写入 Story 卡"审核记录"节。

## 5. DEF 缺陷协议

- 编号 `DEF-S{n}-NN`,由测试 Agent 创建:复现步骤、期望/实际、证据路径、指派(按模块域:Core/App→Dev A;MockGame/Input/Capture/Vision→Dev B;tests/→测试 Agent 自查);
- 修复:对应开发在自己分支修复,commit 注明 `fix: DEF-S{n}-NN`,推送后由 QA 复测关闭;
- 阻塞级缺陷(导致 SAC 无法判定)由架构师决定是否叫停 Sprint。

## 6. 交付物模板(每 Sprint)

**开发简报**(Dev A → `M0-S{n}-简报-DevA.md`;Dev B → `M0-S{n}-简报-DevB.md`,置于 `docs/iterations/M0/sprints/`):

```markdown
# SPRINT M0-S{n} 简报(Dev A / Dev B)
- Agent / 日期 / 分支与 commit 范围:
## 完成的 Story 与证据
| Story | 结果 | 证据(构建输出/文件路径) |
## SAC 相关自查(仅本组相关项)
| SAC | 结果 | 说明 |
## 偏离与理由(相对技术设计)
## 遗留问题
```

**QA 测试报告**(`docs/iterations/M0/qa/M0-S{n}-测试报告.md`):

```markdown
# SPRINT M0-S{n} 测试报告
- 测试Agent / 日期 / iter/m0 commit:
## 集成记录(合并了哪些 dev 分支、冲突处理)
## 用例执行矩阵
| 用例 | 结果 | 证据路径 |
## 覆盖率(纯逻辑类行覆盖)
## 缺陷清单
| DEF | 摘要 | 指派 | 状态 |
## 资产生成记录(模板/金样本,如有)
## 结论(该 Sprint SAC 是否全部可判通过)
```

## 7. 迭代收尾

全部 Sprint 通过后:架构师执行 **03 §4 完整七项门禁**(汇总材料=双开发简报+QA 报告+`ITER-M0-实施自检报告.md`)→ 用户执行 `[真机]` 项 → `iter/m0` 合入 `main` → 发包下一迭代。

## 8. 当前 Sprint 划分与负责人速览(M0)

| Sprint | Dev A | Dev B | 测试 Agent |
|---|---|---|---|
| S1 工程骨架与 MockGame | S1-1 骨架、S1-2 Core | S1-3 MockGame v0 | UT-01/02/04/06/08 编码执行、MockGame 冒烟 |
| S2 窗口发现与截屏 | S2-3 CLI 骨架、S2-4 命令集成 | S2-1 Input、S2-2 Capture | IT-01/02、CLI 行为用例 |
| S3 模板匹配与模板库 | S3-3 save-template/match 命令 | S3-1 Store、S3-2 Matcher | UT-03/05/07、IT-03、S3-4 资产生成 |
| S4 后台输入与 e2e | S4-3 report 向导、S4-4 收尾汇总 | S4-1 点击、S4-2 e2e | IT-04~07、覆盖率复核、证据归档 |

Story 卡见 `iterations/M0/sprints/`,严格串行 S1→S4。
