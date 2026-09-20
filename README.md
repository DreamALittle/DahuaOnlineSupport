# DH2 — 大话西游2免费版五开日常辅助工具

> 纯外部视觉自动化工具:截屏识别 + 后台鼠标键盘消息 + 模板驱动流程。
> 文档基线见 `docs/`;开发流程见 `docs/03-协作流程与审核规范.md` 与 `docs/07-Scrum开发流程.md`。

## 1. 环境要求

| 项 | 要求 |
|---|---|
| OS | Windows 10/11(Win32 P/Invoke / GDI / WGC 依赖) |
| .NET SDK | **10.0.103 或更新 LTS**(`dotnet --version` 验证) |
| 工作目录权限 | 与游戏客户端同权限级(避免 UIPI 静默丢消息,见 `docs/iterations/M0/ITER-M0-技术设计.md` §10.4) |
| 显示缩放 | **100%**(M0 GDI 截屏与 PostMessage 坐标按 1 DIP = 1 px,非 100% 环境坐标误差不作为缺陷) |
| DPI 感知 | Avalonia(MockGame)默认 PerMonitorV2;dh2ctl 通过 `app.manifest` 声明 |

> 安装 Avalonia 模板(首次构建 MockGame 前):
> `dotnet new install Avalonia.Templates`

## 2. 解决方案结构

```
DH2.sln
├── src/
│   ├── DH2.Core/        # 模型/契约/配置/工具(纯逻辑,可独立测)
│   ├── DH2.Input/       # 窗口枚举绑定、后台输入
│   ├── DH2.Capture/     # 截屏(M0:GDI;M1a:WGC)
│   ├── DH2.Vision/      # 模板匹配 + manifest
│   └── DH2.App/         # dh2ctl 控制台 CLI
├── tools/
│   └── DH2.MockGame/    # 模拟游戏窗体(Avalonia,Sprint M0-S1 引入)
├── tests/
│   └── DH2.Tests/       # xUnit:L1 单测 + L2 模拟窗口测试(测试 Agent 编写)
├── configs/             # dev.yaml / mock-layout.yaml / local/(不入仓)
├── templates/           # {profile}/manifest.yaml + png/
├── artifacts/           # 运行输出(不入仓)
├── docs/                # 文档体系(架构师维护)
├── Directory.Build.props
├── .editorconfig
├── .gitignore / .gitattributes
└── README.md
```

引用方向(02 §3 强制单向):

```
App → Orchestration → Tasks/Guardian → Behavior → Vision/Capture/Input → Core
                                                                            ↓
Tests → 全部                                                                  Data
```

**Core 不引用任何项目**(允许引用 OpenCvSharp4 类型层 `Mat` 用作契约)。

## 3. 构建

```powershell
# 全解决方案 Release 构建(零警告零错误)
dotnet build -c Release DH2.sln

# 单项目(开发期快迭代)
dotnet build -c Release src/DH2.Core/DH2.Core.csproj

# 格式门禁(G3)
dotnet format DH2.sln --verify-no-changes

# 自动修复
dotnet format DH2.sln
```

## 4. 运行

M0-S1 仅交付工程骨架,CLI 命令(`enumerate` / `capture` / `save-template` / `match` / `click` / `e2e` / `report`)由后续 Sprint 引入(S2~S4)。

```powershell
# 当前仅可构建;MockGame 启动命令(S1-3 由 Dev B 实现,作为占位):
dotnet run --project tools/DH2.MockGame -c Release
```

## 5. 开发流程(摘要)

1. **clone 远端仓库**到独立工作区(`D:\Repos\DH2-DevA` / `DH2-DevB` / `DH2-QA`);
2. `git fetch origin`,基于最新 `origin/iter/m0` 建工作分支(`dev-a/m0-s{n}` / `dev-b/m0-s{n}`);
3. **小步约定式提交**(`feat:` / `fix:` / `chore:` / `docs:`),每笔独立可 build;
4. Story 完成即 push → 测试 Agent 滚动合并到 `iter/m0` 跑冒烟;
5. 全员 Story 完成 → 提交 `docs/iterations/M0/reports/M0-S{n}-{角色}-任务完成报告.md`;
6. 三份报告齐备并进入 `iter/m0` → 架构师集中审查 + 出具 `M0-S{n}-架构师审核报告.md`;
7. 审核通过 → 架构师签发下一轮放行指令 → 用户转发给三位 Agent。

详细流程见 `docs/07-Scrum开发流程.md`。

## 6. 约定

- 公共 API 必带 XML 文档注释(`/// <summary>`);
- 项目/目录/命名空间三者一致(`DH2.Core.Models` ↔ `src/DH2.Core/Models/`);
- 接口 `I` 前缀;异步方法 `Async` 后缀;P/Invoke 类统一 `static partial class NativeMethods`;
- 不触红线(详见 `docs/01-项目总纲.md` §3):不读进程内存、不拦截封包、不全局钩子、不加速瞬移。

## 7. NuGet 依赖版本

锁定版本写入各项目 `csproj`;`dotnet restore` 自动解析。详见 `docs/iterations/M0/ITER-M0-技术设计.md` §1 表格。