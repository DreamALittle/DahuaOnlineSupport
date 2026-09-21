# ITER-M0 技术设计

开发 Agent 按本文实施。与本文偏离须在自检报告声明,由架构师裁决(03 文档第 2 节)。

## 1. 解决方案结构

```
DH2/
├─ DH2.sln
├─ src/
│  ├─ DH2.Core/        # 契约与纯逻辑:模型、接口、配置实体、工具函数
│  ├─ DH2.Input/       # 窗口枚举绑定、后台输入(Win32)
│  ├─ DH2.Capture/     # 截屏(M0:GDI;WGC 留接口)
│  ├─ DH2.Vision/      # 模板匹配、模板 manifest
│  └─ DH2.App/         # dh2ctl 控制台 CLI
├─ tools/
│  └─ DH2.MockGame/    # 模拟游戏窗体(Avalonia)
├─ tests/
│  ├─ DH2.Tests/       # xUnit:L1 单测 + L2 模拟窗口测试
│  └─ golden/          # 金样本截图与期望(资产,非代码)
├─ configs/            # dev.yaml / mock-layout.yaml / local/(不入仓)
├─ templates/          # mock_800x600/
├─ artifacts/          # 运行输出(不入仓)
├─ docs/               # 本文档体系
├─ .editorconfig  .gitignore  README.md
```

项目引用(仅允许以下方向):App→全部;Tests→全部;Input/Capture/Vision→Core;**Core 不引用任何项目**。

统一 TFM `net10.0-windows`(本机 SDK 10.0.103,无需另装 SDK 8);MockGame 为 Avalonia 项目(包 `Avalonia.Desktop`,可用 `dotnet new install Avalonia.Templates` 后 `dotnet new avalonia.app` 生成,DPI 感知默认 PerMonitorV2);dh2ctl 附 app.manifest 声明 PerMonitorV2;App 控制台项目 `<OutputType>Exe</OutputType>`。

NuGet(取发包日最新稳定版,版本写入根 README):

| 包 | 项目 |
|---|---|
| OpenCvSharp4 + OpenCvSharp4.runtime.win | Vision |
| System.Drawing.Common | Capture, Vision |
| YamlDotNet | Core |
| Serilog + Sinks.Console + Sinks.File | Core(装配), App |
| Avalonia + Avalonia.Desktop + Avalonia.Themes.Fluent | MockGame |
| xunit + coverlet.collector | Tests |

## 2. DH2.Core 设计

### 2.1 基础模型

```csharp
namespace DH2.Core.Models;

public sealed record Win32Window(long Hwnd, string Title, string ProcessName, Rect Bounds);
public sealed record Rect(int X, int Y, int Width, int Height)
{
    public Point Center => new(X + Width / 2, Y + Height / 2);
}
public readonly record struct Point(int X, int Y);
public readonly record struct Size(int Width, int Height);

/// <summary>一帧截屏。Image 归调用方释放。(S1 审核修订:类型 Bitmap→Mat,Core 不引 System.Drawing.Common,Bitmap↔Mat 在 Capture/Vision 边界用 BitmapConverter 转换)</summary>
public sealed record Frame(long Hwnd, DateTime Timestamp, Mat Image)
{
    public int Width => Image.Width;
    public int Height => Image.Height;
}

public sealed record MatchResult(bool Found, double Score, Point Location, Size Size)
{
    public Point Center => new(Location.X + Size.Width / 2, Location.Y + Size.Height / 2);
}

public sealed record ActionResult(bool Success, int Attempts, string FailReason);
```

### 2.2 契约接口

```csharp
namespace DH2.Core.Contracts;

public interface IWindowLocator
{
    /// <summary>枚举并按配置过滤可见顶层窗口。</summary>
    IReadOnlyList<Win32Window> Enumerate(WindowTargetConfig target);
}

public interface IFrameCapture : IDisposable
{
    /// <summary>截取指定窗口客户区当前帧。窗口须可见且未被遮挡(GDI 实现的限制)。</summary>
    Frame Capture(long hwnd);
}

public interface ITemplateMatcher : IDisposable
{
    /// <summary>在 frame 的 ROI(为 null 则全图)内查找模板。坐标相对整帧。</summary>
    MatchResult Match(Mat frame, string templateKey, Rect? roi = null);
}

public interface ITemplateStore
{
    TemplateEntry Get(string key);           // 未注册时抛 KeyNotFoundException
    IReadOnlyList<string> Keys { get; }
    void Reload();                           // 重新读 manifest 与图片
}

public sealed record TemplateEntry(
    string Key, string File, double Threshold,
    Point ClickOffset, string Roi, string Since, Mat Image);
```

`Mat` 为 OpenCvSharp 类型:Core 允许引用 OpenCvSharp4(仅类型层依赖,不承载算法)。

### 2.3 配置实体(YamlDotNet 绑定,启动校验)

```csharp
public sealed class DevConfig
{
    public string Profile { get; init; } = "mock_800x600";
    public List<WindowTargetConfig> Targets { get; init; } = [];
    public PathConfig Paths { get; init; } = new();
    public MatchingConfig Matching { get; init; } = new();
    public InputConfig Input { get; init; } = new();
}
public sealed record WindowTargetConfig(string Name, string? TitlePattern, string? ProcessName);
public sealed record PathConfig(string Templates, string Artifacts);
public sealed record MatchingConfig(double DefaultThreshold = 0.85);
public sealed record InputConfig(string Driver = "background", int PostClickDelayMs = 50);
```

校验规则:`TitlePattern` 与 `ProcessName` 至少一项非空;`DefaultThreshold` ∈ (0.5, 1.0);`Driver ∈ {background, foreground}`(M0 仅实现 background,配置为 foreground 时启动即报"未实现"错误)。校验失败:控制台输出全部错误并以退出码 3 退出。

### 2.4 工具函数

```csharp
public static class Win32Coord
{
    // PostMessage lParam:低16位x,高16位y
    public static int ToLParam(Point clientPoint);
    public static Point FromLParam(int lParam);
    public static Point ClientToScreen(Rect windowClientScreenOrigin, Point clientPoint); // 纯函数供单测
}
public static class Polling
{
    public static Task<bool> WaitUntilAsync(
        Func<Task<bool>> predicate, int intervalMs, int timeoutMs, CancellationToken ct);
}
```

## 3. DH2.Input 设计

### 3.1 NativeMethods(P/Invoke 清单,白名单之外的 Win32 声明须审核)

| API | 用途 |
|---|---|
| `EnumWindows` / `IsWindowVisible` / `GetWindowTextW` / `GetClassNameW` | 窗口枚举 |
| `GetWindowThreadProcessId` | 取 pid |
| `GetClientRect` / `ClientToScreen` | 客户区矩形与坐标换算 |
| `PostMessageW` | 后台鼠标消息 |
| `SendInputW` / `SetForegroundWindow` | **仅声明占位**,M0 不实现焦点轮转 |

**禁止**自行声明 `OpenProcess`:进程名获取一律 `Process.GetProcessById(pid).ProcessName`(框架内部实现,代码无红线 API)。

### 3.2 WindowEnumerator(实现 IWindowLocator)

流程:EnumWindows → IsWindowVisible && 标题非空 → GetWindowTextW → GetWindowThreadProcessId → ProcessName → 按 TargetConfig 的 `TitlePattern`(正则)或 `ProcessName` 过滤 → GetClientRect+ClientToScreen 得客户区屏幕矩形 → 输出 `Win32Window`。异常窗口(已销毁)跳过不抛。

### 3.3 PostMessageDriver(后台输入)

点击序列(全部客户区坐标,经 `Win32Coord.ToLParam` 编码):

```
PostMessage(hwnd, WM_MOUSEMOVE, 0, lParam)          // 可选
PostMessage(hwnd, WM_LBUTTONDOWN, MK_LBUTTON, lParam)
await Task.Delay(PostClickDelayMs)
PostMessage(hwnd, WM_LBUTTONUP, 0, lParam)
```

返回 `ActionResult`:PostMessage 返回 false 即 `Success=false, FailReason="PostMessage returned false"`。**不做**重试(M0 无后置验证语义,重试属行为层)。

DPI:Avalonia 默认 PerMonitorV2;dh2ctl 以 app.manifest 声明(避免 GDI 截屏与坐标被系统虚拟化);M0 测试环境要求 100% 缩放(README 注明),非 100% 环境坐标换算误差不作为缺陷。

## 4. DH2.Capture 设计

### 4.1 GdiCapture(实现 IFrameCapture)

流程:`GetClientRect` + `ClientToScreen` 得屏幕矩形 → `Graphics.CopyFromScreen` → `Bitmap(PixelFormat.Format32bppArgb)` → 包成 Frame。

**已知限制(写入代码注释与 README)**:窗口须在屏幕内、可见、未被他窗遮挡;最小化时得到错误内容。M1a 用 WGC 消除。

### 4.2 WgcCapture

M0 只建 `internal class WgcCapture` 占位(构造抛 `NotImplementedException`),保持 `IFrameCapture` 接口稳定。

## 5. DH2.Vision 设计

### 5.1 TemplateManifest(YAML)

`templates/{profile}/manifest.yaml`:

```yaml
profile: mock_800x600
templates:
  - key: mock_taskbar
    file: png/mock_taskbar.png
    threshold: 0.85        # 缺省用 DevConfig.Matching.DefaultThreshold
    clickOffset: { x: 0, y: 0 }
    roi: taskbar           # 逻辑分区名,M0 仅记录,不参与裁剪
    since: m0
```

`TemplateStore` 加载:解析 manifest → 相对 `templates/{profile}/` 读 PNG → `Cv2.ImRead` 灰度 → 不可变快照;`Reload()` 支持热重载。文件缺失/字段非法:启动失败并列出全部错误。

### 5.2 TemplateMatcher(实现 ITemplateMatcher)

算法:全图或 ROI 裁剪 → 灰度 → `Cv2.MatchTemplate(TM_CCOEFF_NORMED)` → `Cv2.MinMaxLocLoc` 取最大值 → `score ≥ threshold` 则 Found。ROI 裁剪时返回坐标**换算回整帧坐标系**。模板大于帧/ROI:返回 `Found=false, Score=0`。帧与模板的 Bitmap↔Mat 转换用 `OpenCvSharp.Extensions.BitmapConverter`。

## 6. DH2.App(dh2ctl)设计

命令行手动解析(M0 不引第三方解析库)。**退出码:0 成功 / 2 用法错误 / 3 配置或执行失败**。

| 命令 | 参数 | 行为与输出 |
|------|------|-----------|
| `enumerate` | `--config <path>`(默认 configs/dev.yaml) | 打印表:Hwnd / Title / Process / ClientRect |
| `capture` | `--hwnd <n> --count <10> --interval-ms <500> --out <dir>` | 存 `frame_{yyyyMMddHHmmssfff}_{i}.png`;结束打印 均值/p95 耗时(ms) |
| `save-template` | `--hwnd --x --y --w --h --key <name> [--config]` | 截屏→裁剪→存 `templates/{profile}/png/{key}.png`→manifest 登记(幂等覆盖) |
| `match` | `--hwnd --key <name> [--config]` | 输出 JSON 行:`{"found":true,"score":0.97,"location":[x,y],"size":[w,h],"center":[cx,cy]}` |
| `click` | `--hwnd --x --y [--config]` | PostMessage 点击,输出 ActionResult |
| `e2e` | `--target mock [--config]` | 见 6.1 |
| `report` | `--target game [--config]` | 真机验证向导:分步提示+采集证据到 `artifacts/m0-report/` |

### 6.1 e2e 闭环(AC-08)

1. enumerate 定位 MockGame 窗口(唯一,多个则失败退出);
2. capture 一帧;
3. match `mock_taskbar` → 不得 Found=false(否则 FAIL:任务栏定位失败);
4. 从 `configs/mock-layout.yaml` 读按钮矩形 → 客户区中心坐标;**坐标空间规则(S4 终审修正):布局坐标为逻辑值,PostMessage 目标坐标必须换算到帧空间:`scale = frame.Width / layout.window.width`,按钮帧坐标 = taskbar 实测中心 + (按钮布局中心 − 任务栏布局中心) × scale**(接收端框架按物理→DIP 换算消息坐标,投递逻辑坐标在非 100% 缩放下会落空——S4 终审 DEF-S4-01 实测);
5. click 该中心;
6. 轮询 MockGame 状态文件(≤5s)至 `state == "Pathfinding"` 或 `"Arrived"`,超时 FAIL;
7. 再 capture 一帧,match `mock_btn_return`(已随模板库入库)确认"返回"按钮出现;
8. 输出 `E2E: PASS/FAIL` 与步骤耗时,全部证据(帧 PNG、匹配 JSON、状态快照)落 `artifacts/e2e-{ts}/`。

## 7. DH2.MockGame v0 规格

**窗口**:Avalonia `Window`;Title=`DH2.MockGame`;ClientSize 800×600;`CanResize=false`;`WindowStartupLocation=CenterScreen`;DPI 感知由 Avalonia 默认提供(PerMonitorV2);HWND 经 `TryGetPlatformHandle()?.Handle` 获取。

**布局(与 configs/mock-layout.yaml 单一真源,程序启动时读取该文件渲染)**:

| 元素 | 矩形(x,y,w,h) | 样式 |
|------|----------------|------|
| 任务追踪栏 | 16,16,320,88 | 底 #1E1E2E,边框 2px #555555,白字 20px:`师门任务 ({n}/20)` |
| 状态文本 | 16,116(左上角,自适应宽高) | 白字 18px |
| 主按钮 | 16,180,140,48 | 底 #2D5BFF,白字:`前往`/`返回` |

**状态机**:`Idle --点击按钮--> Pathfinding(按钮禁用,状态文本"寻路中...",2000ms)--> Arrived(状态文本"已到达目的地",按钮文字→`返回`,启用) --点击按钮--> Idle(n+1,状态文本"待机")`。

**可观测(供测试断言,不依赖截图)**:

- 原始消息日志:窗口加载后以 comctl32 `SetWindowSubclass` 子类化**自身** HWND(同进程、仅本窗口,非全局钩子,不在红线清单),捕获 WM_MOUSEMOVE/WM_LBUTTONDOWN(0x201)/WM_LBUTTONUP(0x202),解码 lParam,追加写 `%TEMP%\dh2-mockgame\messages.log`,行格式:`{unixms}|raw|{msg}|x={x}|y={y}`;
- 语义日志:Avalonia `PointerPressed`/`PointerReleased` 事件(100% 缩放下 DIP 与像素一致),行格式:`{unixms}|ui|{event}|x={x}|y={y}`,与原始日志同写一个文件;
- 状态文件:`%TEMP%\dh2-mockgame\state.json`,内容 `{"state":"Idle","counter":0,"ts":"..."}`,**每次转移立即原子覆写**;
- 启动时清理旧日志/状态。

按钮逻辑必须由 Avalonia Button 的 Click 事件驱动(验证 PostMessage 能驱动真实控件);子类化钩子只记录不处理。若出现"raw 日志有消息而 UI 状态不转移"的现象,在自检报告记录并交架构师裁决(Avalonia 输入管线对 posted 消息的处理差异会影响 MockGame 断言策略)。

## 8. 配置文件

`configs/dev.yaml`:

```yaml
profile: mock_800x600
targets:
  - name: mock
    titlePattern: '^DH2\.MockGame$'
paths:
  templates: templates
  artifacts: artifacts
matching:
  defaultThreshold: 0.85
input:
  driver: background
  postClickDelayMs: 50
```

`configs/mock-layout.yaml`:按第 7 节布局表逐字段(`window/taskbar/status/button/stateTimings`),另含 `tempDir: "%TEMP%/dh2-mockgame"`(程序解析环境变量)。

`configs/local/game.yaml.example`(真机用,复制改名后填写):

```yaml
profile: game_std            # 首次真机验证先手建 profile 并 save-template
targets:
  - name: game
    titlePattern: '大话西游'   # 按实际窗口标题调整
paths: { templates: templates, artifacts: artifacts }
matching: { defaultThreshold: 0.80 }
input: { driver: background, postClickDelayMs: 60 }
```

## 9. 日志与 artifacts

- Serilog:Console(简) + `artifacts/logs/dh2ctl-{date}.log`(全量);scope 属性:`hwnd`、`command`;
- artifacts 目录结构:`artifacts/capture-*/*.png`、`artifacts/e2e-*/`、`artifacts/m0-report/`;
- 所有 evidence 文件名含时间戳,禁止覆盖已有文件。

## 10. 实施注意事项(已知陷阱)

1. **Avalonia 控件与 PostMessage**:坐标必须命中按钮客户区(DIP==像素,100% 缩放);`MK_LBUTTON=0x0001`;两消息间隔不足可能导致 Click 不触发——按 PostClickDelayMs 配置;
2. **DPI**:Avalonia 自带 PerMonitorV2;dh2ctl 必须带 app.manifest PerMonitorV2,否则 GDI 截屏与坐标被虚拟化缩放;
3. **截屏黑帧/错帧**:目标窗口不得最小化;测试先 `SetWindowPos`/手动确保可见;CopyFromScreen 抓的是屏幕像素,遮挡即污染——测试用例须保证窗口前置(M0 由测试自身启动 MockGame 保证);
4. **管理员权限一致性**:dh2ctl 与 MockGame 同权限级运行(README 注明;UIPI 会静默丢弃发往更高完整性窗口的消息——这也是真机验证要观察的点);
5. **金样本生成**:由 L2 测试在受控环境生成一次并提交 `tests/golden/screenshots/mock/`,文件头注释生成条件(缩放/DPI);L1 识别测试只用这些入库副本,不依赖运行时截图。

## 11. 完成定义(对齐任务书)

全部 AC 满足、`dotnet format --verify-no-changes` 通过、自检报告填写完整后,提交至 `iter/m0` 分支并提请审核。
