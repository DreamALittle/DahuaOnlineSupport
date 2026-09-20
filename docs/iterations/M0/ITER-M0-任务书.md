# ITER-M0 任务书:技术验证与工程骨架

| 项 | 内容 |
|---|---|
| 迭代 | M0(路线图首个迭代) |
| 状态 | 已发包 |
| 周期 | 3~5 天 |
| 前置 | 无(首个迭代) |
| 配套文档 | [技术设计](ITER-M0-技术设计.md) · [测试设计](ITER-M0-测试设计.md) · 流程见 [03-协作流程](../../03-协作流程与审核规范.md) |

## 1. 目标

1. 建立解决方案骨架与工程规范载体(git / editorconfig / CI 脚本化命令);
2. 跑通**最小技术闭环**:窗口枚举 → 截屏 → 模板匹配 → 后台点击 → 状态变化确认(全部对 MockGame);
3. 提供 `[真机]` 验证工具,由用户对真实游戏窗口执行同样闭环,**确定执行层走后台 PostMessage 还是降级焦点轮转**(全项目关键决策)。

## 2. 范围

**含:**

- DH2.sln 与 6 个项目(Core / Input / Capture / Vision / App / MockGame)+ 测试项目;
- GDI 截屏实现(WGC 留接口,M1a 实现);
- 模板匹配(OpenCvSharp,单模板,无 mask 要求);
- PostMessage 后台点击(焦点轮转只留接口与配置开关,不实现);
- `dh2ctl` CLI 六个子命令(enumerate / capture / save-template / match / click / e2e)+ 真机验证向导 report;
- MockGame v0(布局/状态机/消息日志/状态文件);
- 单元测试(L1)与模拟窗口测试(L2)、金样本入库。

**不含(越界即审核不通过):**

- ROI/锚点体系、场景分类器、OCR(M1a);
- 行为层原子操作、任务引擎(M1b);
- Guardian、SQLite、五开(M1c/M3);
- WGC 截屏、焦点轮转实现。

## 3. 交付物清单

| # | 交付物 | 位置 |
|---|--------|------|
| 1 | 源代码 6+1 项目,build 零警告零错误 | `DH2.sln`, `src/`, `tools/`, `tests/` |
| 2 | dh2ctl 可执行 CLI | `src/DH2.App` |
| 3 | MockGame v0(Avalonia) | `tools/DH2.MockGame` |
| 4 | 配置样例 | `configs/dev.yaml`, `configs/mock-layout.yaml`, `configs/local/game.yaml.example` |
| 5 | 初始模板与 manifest | `templates/mock_800x600/` |
| 6 | 测试(L1+L2)与金样本 | `tests/DH2.Tests`, `tests/golden/` |
| 7 | 工程规范载体 | `.gitignore`, `.editorconfig`, 根 `README.md`(构建/运行/命令示例) |
| 8 | 实施自检报告 | `docs/iterations/M0/ITER-M0-实施自检报告.md`(模板见 03 文档第 3 节) |
| 9 | 远端仓库分支 | clone 远端仓库,`iter/m0` 分支开发并推送,策略按 03 文档第 7 节 |

## 4. 验收标准(AC,审核 G6 逐条核对)

| # | 标准 | 验证方式 |
|---|------|----------|
| AC-01 | 从远端仓库 clone 开发:`iter/m0` 分支分批提交、提交信息符合约定式、分支推送至远端 | 审阅 git log 与远端分支 |
| AC-02 | `dotnet build -c Release` 全解决方案零警告零错误 | 复跑 |
| AC-03 | `dotnet test` 全绿;纯逻辑类(lParam 编码/manifest 解析/配置绑定/阈值判定/布局计算)行覆盖 ≥70% | coverlet 报告 |
| AC-04 | `dh2ctl enumerate` 按 dev.yaml 过滤,正确列出 MockGame 窗口(hwnd/title/process/rect) | IT-01 |
| AC-05 | `dh2ctl capture --count 10` 输出 10 张 PNG,尺寸正确、内容非空,输出每帧耗时统计 | IT-02 |
| AC-06 | `dh2ctl save-template` 生成模板并登记 manifest;`dh2ctl match` 在新截图上定位任务栏,**中心点与 mock-layout 真值误差 ≤2px** | IT-03 |
| AC-07 | `dh2ctl click` 点击按钮中心,MockGame 消息日志收到 WM_LBUTTONDOWN/UP,**坐标误差 ≤1px** | IT-04 |
| AC-08 | `dh2ctl e2e --target mock` 全自动闭环 PASS:匹配任务栏→计算按钮→点击→状态转移→截图确认"返回"按钮出现 | IT-05 |
| AC-09 | `[真机]` 用户按测试设计第 4 节执行真机验证并回填报告;架构师据此裁决执行层路线 | 用户回填 |
| AC-10 | 根 README 含:环境要求、构建命令、六个 CLI 命令示例与输出说明 | 审阅 |

**注:AC-09 是里程碑出口关卡,其结论可能导致执行层设计变更,由架构师处理,不影响本迭代其余 AC 的验收。**

## 5. 出口条件

全部 AC 通过 + 架构师审核 APPROVED → 发包 M1a(感知层完整化)。
