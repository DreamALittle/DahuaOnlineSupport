# ITER-M0 测试设计

对应任务书 AC-02~AC-09。开发 Agent 负责编码并执行 L1/L2,用户负责 L4 真机项。命名与规范见 04 文档第 7 节。

## 1. L1 单元测试(无外部依赖,CI 可跑)

| # | 被测对象 | 用例 | 期望 |
|---|----------|------|------|
| UT-01 | `Win32Coord.ToLParam/FromLParam` | (0,0)、(100,50)、(65535,65535)、负数防御 | 编码往返一致;lParam=(y<<16)\|x;负坐标抛 ArgumentOutOfRangeException |
| UT-02 | `Win32Coord.ClientToScreen` | 窗口原点(100,200)+客户点(16,16)等三组 | 纯函数结果正确 |
| UT-03 | manifest 解析 | 完整样例 / 缺 key / 缺 file / threshold 越界 / file 指向不存在图片 | 完整→加载成功;异常→聚合报错含条目 key |
| UT-04 | DevConfig 校验 | targets 两字段全空 / threshold=1.2 / driver=foreground / 合法配置 | 前三者启动失败并列出全部错误;合法配置通过 |
| UT-05 | TemplateMatcher 阈值判定(注入伪造匹配结果) | score 恰等于阈值 / 高于 / 低于 | 等于→Found=true;低于→false |
| UT-06 | mock-layout 解析与几何 | 加载样例,计算任务栏/按钮中心 | 中心点=(16+320/2,16+88/2)=(176,60);按钮中心=(86,204) |
| UT-07 | 金样本静态识别 | `tests/golden/screenshots/mock/idle.png` × `mock_taskbar` 模板 | Found=true,中心误差 ≤2px;再测 `mock_btn_go` 同样通过 |
| UT-08 | `WaitUntilAsync` | 立即真 / 超时假 / 中途取消 | 三个语义各一用例,超时与取消不抛未观察异常 |

覆盖率统计口径:DH2.Core 全部 + Vision 的纯逻辑(不含 Mat 交互薄层)≥70%。

## 2. L2 模拟窗口测试(需桌面会话,自动化)

前置 fixture:测试进程自行启动 MockGame(`Process.Start`,等待其状态文件出现即就绪),测试结束确保杀掉(teardown 幂等)。

| # | 场景 | 步骤 | 断言 | 对应 AC |
|---|------|------|------|---------|
| IT-01 | 窗口枚举 | 启动 MockGame → `enumerate` 按 dev.yaml 过滤 | 恰好 1 条,Title/ProcessName/Rect 正确(Rect 为客户区 800×600) | AC-04 |
| IT-02 | 连续截屏 | `capture --count 10` | 10 张 PNG;尺寸 800×600;像素方差 > 阈值(非黑帧);耗时统计打印 | AC-05 |
| IT-03 | 模板定位精度 | 截屏 → match `mock_taskbar` | Found;中心与 mock-layout 真值(176,60)误差 ≤2px | AC-06 |
| IT-04 | 点击精度与消息到达 | 计算按钮中心 → click | MockGame messages.log 在 2s 内出现 `0x201` 与 `0x202` **raw 行**,坐标误差 ≤1px;state.json 转为 Pathfinding(raw 行有而 state 未转移→记录并交架构师裁决) | AC-07 |
| IT-05 | e2e 闭环 | 运行 `e2e --target mock` | 进程退出码 0,输出 `E2E: PASS`,证据目录含两帧 PNG+匹配 JSON+状态快照 | AC-08 |
| IT-06 | 二次循环(状态回转) | e2e 后再 click"返回"按钮位置 | state.json 回 Idle 且 counter=1 | AC-08 |
| IT-07 | 异常输入防御 | click 到 (799,599) 边角、对已关闭窗口句柄点击 | 不抛未处理异常,返回 ActionResult(false) | 规范 4 |

性能记录(不设门禁,写入自检报告):PT-01:capture 30 帧均值/p95;PT-02:e2e 全程耗时。

## 3. L3/L4 边界说明

- L3 回放测试本迭代不建设(金样本静态识别已由 UT-07 覆盖雏形);
- L4 见下节。

## 4. L4 真机验证清单 `[真机]`(用户执行,AC-09)

**环境**:游戏客户端进入世界场景;窗口非全屏(窗口化或无边框);Windows 缩放记录实际值;dh2ctl 与游戏同权限级运行(都普通权限,或都管理员——若游戏以管理员运行而 dh2ctl 不是,PostMessage 会被 UIPI 静默丢弃,请在报告中注明权限组合)。

**步骤**(`configs/local/game.yaml` 由用户按 example 填好):

| 步骤 | 命令 | 通过判据 | 证据 |
|------|------|----------|------|
| V1 | `dh2ctl enumerate --config configs/local/game.yaml` | 列出游戏窗口 | 控制台输出截图 |
| V2 | `dh2ctl capture --hwnd <n> --count 5` | PNG 为游戏画面(非黑帧/非桌面其他区域) | 5 张 PNG |
| V3 | `dh2ctl save-template --hwnd <n> --x..--y..--w..--h..--key tracker_bar`(框选任务追踪栏) | 模板 PNG 内容正确 | 模板文件 |
| V4 | `dh2ctl match --hwnd <n> --key tracker_bar` | found=true 且 score≥0.8 | 输出 JSON |
| V5 | `dh2ctl click --hwnd <n> --x..--y..`(任务追踪栏当前任务条目位置) | **角色开始自动寻路移动** | 点击前后两张截图或 15s 录屏 |
| V6(可选) | 对战斗外其他可点击 UI(如打开任务面板按钮)重复 V5 | 面板打开 | 截图 |

**回填报告模板**(`docs/iterations/M0/ITER-M0-真机验证报告.md`):

```markdown
# ITER-M0 真机验证报告
- 日期 / Windows 版本 / 显示缩放 / 游戏客户端版本:
- 权限组合:两者均普通 / 均管理员 / 不一致(说明):
| 步骤 | 结果 | 证据路径 | 备注 |
|------|------|----------|------|
| V1~V6 | | | |

## 结论(用户勾选)
- [ ] V5 通过:游戏响应后台 PostMessage,角色移动 → 执行层维持后台消息主路线
- [ ] V5 不通过但截屏/匹配通过 → 降级焦点轮转,转架构师处理
- [ ] V2 即不通过(截屏不可用)→ 转 architecture 复议截屏方案
- 异常现象记录:
```

## 5. 通过判定

- AC-01~AC-08:自动化与审阅核对;
- AC-09:用户回填真机报告后,由架构师在审核报告中裁决路线并更新 02 文档决策记录。
