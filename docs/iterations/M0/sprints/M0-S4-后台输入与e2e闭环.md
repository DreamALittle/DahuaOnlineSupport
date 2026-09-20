# SPRINT M0-S4:后台输入与 e2e 闭环

| 项 | 内容 |
|---|---|
| 目标 | PostMessage 后台点击驱动 MockGame 状态转移;`e2e` 全自动闭环 PASS;M0 迭代收尾就绪 |
| 依赖 | M0-S3 |
| 预估 | 1 天 |

## Stories

| ID | Story | 技术设计引用 | 测试引用 |
|----|-------|--------------|----------|
| S4-1 | DH2.Input:PostMessageDriver(MOVE/DOWN/UP 序列、MK_LBUTTON、PostClickDelayMs)+ dh2ctl `click` 命令 | 技术设计 §3.3、§6 | IT-04、IT-07 |
| S4-2 | dh2ctl `e2e --target mock`:EnsureIdle → 枚举 → 截屏 → 匹配任务栏 → 布局计算按钮中心 → 点击 → 状态轮询 → 二次截屏匹配 `mock_btn_return` → PASS/FAIL 与证据落盘 | 技术设计 §6.1 | IT-05、IT-06 |
| S4-3 | dh2ctl `report --target game` 真机验证向导(分步提示 + 证据目录 `artifacts/m0-report/`);**仅实现向导,不执行真机验证** | 技术设计 §6 | 人工核对输出内容 |
| S4-4 | 迭代收尾:`dotnet format --verify-no-changes`;覆盖率复核 ≥70%;填写 `ITER-M0-实施自检报告.md`(AC-01~AC-10 逐条,AC-09 标注"待用户执行");推送 `iter/m0` 至远端 | 任务书 §3/§4 | G3/G4 预演 |

## SAC(Sprint 验收标准,Review 逐条核对)

- [ ] SAC4-1 IT-04/07 通过:raw 日志 2s 内出现 0x201/0x202 且坐标误差 ≤1px;state 转移 Pathfinding;非法句柄返回 ActionResult(false) 不抛异常
- [ ] SAC4-2 IT-05/06 通过:`e2e` 退出码 0 且输出 `E2E: PASS`;后续点击"返回"回 Idle 且 counter 递增
- [ ] SAC4-3 `report` 向导输出 V1~V6 步骤指引并创建证据目录
- [ ] SAC4-4 format 通过、覆盖率达标、自检报告完整、`iter/m0` 已推送远端

## 范围外(严禁实施)

- 焦点轮转 SendInput 实现(仅保留声明占位,属后续迭代)
- 真机验证执行(用户专属;伪造结论=红线违规)
- M1a 及之后任何功能

## Sprint 简报(开发 Agent 填写后,本卡附链接)

(链接到本目录 M0-S4-Sprint简报.md)

## 审核记录(架构师填写)

(待 Sprint Review)
