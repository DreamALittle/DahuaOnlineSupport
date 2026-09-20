# DH2 Scrum 开发流程(Sprint/Story 执行细则)

03 文档定义迭代级总流程,本文细化到 Sprint/Story 级。两者冲突时以 03 为准。**核心纪律:开发 Agent 一次只实施一个 Sprint,完成即停,通过 Sprint Review 后才领取下一个。禁止一次性从头做到尾。**

## 1. 角色映射

| Scrum 角色 | 本项目承担者 |
|---|---|
| Product Owner | 用户(优先级决策、真机验证、业务风险决策) |
| Scrum Master / 流程把控 | 架构师 Agent |
| 开发团队 | 开发 Agent |
| Product Backlog | 06-开发路线图(里程碑 → 迭代) |
| Sprint Backlog | `docs/iterations/{迭代号}/sprints/*.md`(Story 卡,架构师编写) |

## 2. 节奏与粒度

- **Sprint** = 0.5~1 个工作日;一个迭代 = 2~5 个 Sprint,串行推进,不并行;
- **Story** = 半天内可完成的模块/功能单元,独立可构建、可测试;
- 每笔 commit 对应一个 Story 或其一部分;**禁止实现当前 Sprint 范围之外的内容**(越界=审核不通过,即使代码正确)。

## 3. Sprint 生命周期(每一轮循环)

```
架构师发 Story 卡 ─▶ 开发 Agent 逐 Story 实施(小步提交)─▶ Sprint 简报 ─▶ 架构师 Sprint Review
      ▲                                                                        │
      └──────────────── 通过:发放下一 Sprint ◀──────────────────────────────────┤
                                                                     不通过:整改清单返工
```

1. **Sprint Planning(架构师)**:发放当前 Sprint 的 Story 卡(目标、Stories、技术设计章节引用、SAC 验收标准、测试用例引用),附启动 prompt;一次只发一个 Sprint;
2. **实施(开发 Agent)**:按卡逐 Story 开发,小步提交,范围严格限定在卡内;
3. **Sprint Review(架构师)**,轻量四项门禁:
   - ① `dotnet build` / `dotnet test` 复跑复现;
   - ② 红线 API 快扫(grep 清单同 03 §4 G1);
   - ③ 范围核对:无越界(未提前做后续 Sprint)、无缺失(卡内 Story 齐全);
   - ④ SAC 逐条核对证据;
   产出 Sprint 审核意见(通过 / 整改清单),记录在 Story 卡的"审核记录"节;
4. **Sprint 简报与 Retro(开发 Agent / 架构师)**:开发 Agent 填写 Sprint 简报(模板见下);架构师在迭代收尾汇总改进项写入审核报告。

## 4. Story 卡模板(架构师填写)

```markdown
# SPRINT {迭代号}-S{n}:{名称}
| 项 | 内容 |
|---|---|
| 目标 | 本 Sprint 交付的能力(一句话) |
| 依赖 | 前置 Sprint / 无 |
| 预估 | 0.5~1 天 |

## Stories
| ID | Story | 技术设计引用 | 测试引用 |
|----|-------|--------------|----------|
| S{n}-1 | ... | 技术设计 §x | UT-xx |

## SAC(Sprint 验收标准,Review 逐条核对)
- [ ] SAC{n}-1 ...

## 范围外(严禁实施)
- ...

## Sprint 简报(开发 Agent 填写后,本卡附链接)
(链接到本目录 M0-S{n}-Sprint简报.md)

## 审核记录(架构师填写)
(通过 / 整改清单,日期与结论)
```

## 5. Sprint 简报模板(开发 Agent 填写)

```markdown
# SPRINT {迭代号}-S{n} 简报
- 开发Agent / 日期 / commit 范围(hash..hash):
## 完成的 Story 与证据
| Story | 结果 | 证据(测试输出/文件路径) |
## SAC 自查
| SAC | 结果 | 说明 |
## 偏离与理由(相对技术设计)
## 遗留问题
```

## 6. 迭代收尾

全部 Sprint 通过后:架构师执行 **03 §4 完整七项门禁** → 用户执行 `[真机]` 项 → 关闭迭代 → 发包下一迭代(含其 Sprint 拆分)。

## 7. 当前 Sprint 划分(M0)

见 `iterations/M0/sprints/`:S1 工程骨架与 MockGame → S2 窗口发现与截屏 → S3 模板匹配与模板库 → S4 后台输入与 e2e 闭环。严格串行。
