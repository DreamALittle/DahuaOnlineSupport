# M0 迭代终审报告(技术验证与工程骨架)

- 审核人:架构师 Agent | 日期:2026-09-21 | 收口对象:`iter/m0` @ `07ceabd` + 架构师终验记录
- 结论:**有条件收口(PO 已批准)**——工程实质全部验证;两项验证类遗留随 M1a WGC 闭环

## 1. 七项门禁(G1~G7)

| 门禁 | 结论 | 证据 |
|---|---|---|
| G1 红线扫描 | **PASS** | 全仓零命中(仅白名单 P/Invoke:EnumWindows 系/GetClientRect/ClientToScreen/PostMessageW/comctl32 子类化三件套);无注入/钩子/封包类包 |
| G2 架构符合性 | **PASS** | 项目与依赖方向全程符合 02 §3 单向规则;Sprint 卡范围核对 S1~S4 逐轮通过 |
| G3 编码规范 | **PASS** | 每轮 `dotnet format --verify-no-changes` 通过;TreatWarningsAsErrors 全程零警告 |
| G4 测试门禁 | **PASS(附 2 项守护性失败)** | 164 用例:162 绿 + 2 失败为 RJ-S4-05 资产守护 UT 正确检出待重建的 btn_return 资产(按裁cd定随 M1a 转绿);Core 行覆盖 96.52% ≥70% |
| G5 可复现 | **PASS** | 各轮审核均为 origin/iter/m0 全新检出后复跑 build+test 通过 |
| G6 AC 核对 | **AC-01~AC-08、AC-10 ✅;AC-09 待用户** | AC-07/08 的 e2e 链路已实测:锚定 1.0 → 帧空间点击 → Idle→Pathfinding→Arrived 两次观测;全链 PASS 见 §3 遗留(a) |
| G7 文档同步 | **PASS** | 技术设计两处修正(§6.1 坐标空间/谓词)已回写;README/manifest/决策记录同步 |

## 2. M0 回答的关键技术问题(出口关卡成果)

1. **后台 PostMessage 被游戏类窗口接受**(MockGame 实测:raw 0x201/0x202 → UI 事件 → 状态转移)——执行层主路线成立,焦点轮转降级方案备而不用;
2. **坐标空间规则确立**:投递坐标必须为帧空间(物理)像素,推导式 `锚点实测中心 + 布局中心差 × (frame.W/layout.W)`(150% 缩放实测修正,技术设计 §6.1 已回写);
3. **GDI 截屏的边界被实测划清**:无法穿透遮挡,且"保存后立即自匹配"是自指假阳性——**WGC 提前为 M1a 第一优先(PO 已批准)**,资产生成必须配独立内容断言+完整性守护 UT(RJ-S4-05 已建,永久防线);
4. 合成光标/键盘注入禁令确立:真实 UI 转移一律 PostMessage(确定性)或用户人工点击。

## 3. 有条件收口的遗留(全部转入 M1a 出口回归,非 M1a 功能阻塞)

| # | 遗留 | 转绿条件 |
|---|---|---|
| a | e2e 全链 PASS(当前 2.5/3,卡桌面遮挡) | M1a WGC 落地后,任意桌面状态重跑 e2e = PASS(M1a 出口标准) |
| b | mock_btn_return 资产重建(现为污染版,守护 UT 拦截中) | 同上,WGC 截屏后以 dh2ctl click 流程重建 |
| c | RJ-S4-04 按钮双态蓝底的真机视觉复验 | 同上 |

## 4. 交付物清点(任务书 §3)

全部交付:DH2.slnx(6+1 项目)、dh2ctl(8 子命令:enumerate/capture/save-template/match/click/e2e/report+help)、MockGame v0、configs 样例、模板与金样本(守护 UT 护航)、164 用例测试、git 规范全程(约定式提交/分支权限/工作区隔离)、自检报告与 10 份审核文档。

## 5. 后续

- **AC-09 真机验证(V1~V6)**:用户执行(游戏已在线),V5 为执行层路线最终判定;
- 通过后 M0 完全关闭,进入 **M1a(WGC 截屏第一优先 → ROI/锚点/场景分类/OCR)**。
