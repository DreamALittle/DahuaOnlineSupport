using System.Diagnostics;
using System.Text.Json;
using DH2.App.Cli;
using DH2.Capture;
using DH2.Core.Config;
using DH2.Core.Contracts;
using DH2.Core.Models;
using DH2.Core.Util;
using DH2.Input;
using DH2.Vision;
using OpenCvSharp;
using DH2Point = DH2.Core.Models.Point;

namespace DH2.App.Commands;

/// <summary>
/// <c>dh2ctl e2e --target mock</c> —— MockGame 端到端自动化闭环(技术设计 §6.1 + S4-3)。
/// </summary>
/// <remarks>
/// <para>步骤(技术设计 §6.1,S4-3 加 EnsureIdle 前置,RJ-S4-01 修点击坐标推导):</para>
/// <list type="number">
///   <item>EnsureIdle — 轮询 MockGame state.json 至 <c>Idle</c>(≤3s),失败则 FAIL;</item>
///   <item>enumerate — 按 <c>--target</c> 找到 <c>configs/dev.yaml</c> 中对应 WindowTargetConfig,<c>Enumerate</c> 必须恰好 1 个窗口;</item>
///   <item>capture — 取首帧,落 <c>artifacts/e2e-{ts}/frame_before.png</c>;</item>
///   <item>match <c>mock_taskbar</c> — 任务栏定位;Found=false 直接 FAIL(连同证据 JSON);</item>
///   <item>几何计算 — 由 taskbar 实测中心 + (按钮布局中心 − 任务栏布局中心) × scale 推导按钮帧坐标(RJ-S4-01);</item>
///   <item>click — <c>PostMessageDriver.ClickAsync</c> 帧像素坐标(@100% 等同 DIP,@150% 按物理像素投递,Avalonia 接收端自动按物理→DIP 换算消息坐标);</item>
///   <item>状态轮询 — 等 MockGame state.json 转 <c>Pathfinding</c> 或 <c>Arrived</c>(≤5s,§6.1);</item>
///   <item>二次 capture + match <c>mock_btn_return</c> — 仅断言 Found=true + Score ≥ 阈值(不做像素中心断言,S3 终审架构约定);</item>
///   <item>输出 <c>E2E: PASS|FAIL</c> + 步骤耗时 + 证据目录绝对路径。</item>
/// </list>
/// <para>所有证据(<c>frame_*.png</c> / <c>match_*.json</c> / <c>state_*.json</c>)<c>artifacts/e2e-{ts}/</c>。</para>
/// <para>用法错误 → 退出码 2;步骤失败 → 退出码 3;成功 → 退出码 0。</para>
/// </remarks>
public sealed class E2eCommand : IDh2Command
{
    public string Name => "e2e";

    private const int EnsureIdleTimeoutMs = 3000;
    private const int EnsureIdleIntervalMs = 100;
    private const int StatePollTimeoutMs = 5000;
    private const int StatePollIntervalMs = 100;
    private const string DefaultLayoutPath = "configs/mock-layout.yaml";

    private readonly IWindowLocator _locator;
    private readonly IFrameCapture _capture;
    private readonly Func<ITemplateStore, ITemplateMatcher>? _matcherFactory;
    private readonly Func<int, PostMessageDriver>? _driverFactory;

    public E2eCommand(
        IWindowLocator? locator = null,
        IFrameCapture? capture = null,
        Func<ITemplateStore, ITemplateMatcher>? matcherFactory = null,
        Func<int, PostMessageDriver>? driverFactory = null)
    {
        _locator = locator ?? new WindowEnumerator();
        _capture = capture ?? new GdiCapture();
        _matcherFactory = matcherFactory;
        _driverFactory = driverFactory;
    }

    public int Execute(DevConfig config, IReadOnlyDictionary<string, string> options, CancellationToken ct)
    {
        var swTotal = Stopwatch.StartNew();
        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var e2eDir = Path.Combine(config.Paths.Artifacts, $"e2e-{ts}");
        Directory.CreateDirectory(e2eDir);

        // ---- 参数 ----
        var target = options.TryGetValue("target", out var t) && !string.IsNullOrWhiteSpace(t) ? t : "mock";
        var windowTarget = config.Targets.FirstOrDefault(x =>
            string.Equals(x.Name, target, StringComparison.OrdinalIgnoreCase));
        if (windowTarget is null)
        {
            Console.Error.WriteLine($"[e2e error] target '{target}' not in dev.yaml targets");
            return (int)ExitCode.ConfigOrExecutionFailure;
        }

        // ---- mock-layout.yaml(几何单一真源)----
        MockLayoutConfig layout;
        try
        {
            layout = MockLayoutLoader.Load(DefaultLayoutPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[e2e error] load mock-layout.yaml failed: {ex.Message}");
            return (int)ExitCode.ConfigOrExecutionFailure;
        }

        var tempDir = Environment.ExpandEnvironmentVariables(layout.TempDir);
        var statePath = Path.Combine(tempDir, "state.json");

        // ---- EnsureIdle(state.json 存在且为 Idle,否则轮询 ≤3s)----
        if (!EnsureIdle(statePath, out var idleErr))
        {
            Console.Error.WriteLine($"[e2e error] EnsureIdle failed: {idleErr}");
            WriteArtifactState(e2eDir, "state_ensure_idle.json", statePath);
            return (int)ExitCode.ConfigOrExecutionFailure;
        }

        // ---- enumerate(唯一窗口)----
        IReadOnlyList<Win32Window> windows;
        try
        {
            windows = _locator.Enumerate(windowTarget);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[e2e error] enumerate failed: {ex.Message}");
            return (int)ExitCode.ConfigOrExecutionFailure;
        }

        if (windows.Count != 1)
        {
            Console.Error.WriteLine($"[e2e error] expected exactly 1 window for target '{target}', got {windows.Count}");
            return (int)ExitCode.ConfigOrExecutionFailure;
        }

        var hwnd = windows[0].Hwnd;

        // ---- TemplateStore + Matcher ----
        TemplateStore store;
        try
        {
            store = new TemplateStore(config.Paths.Templates, config.Profile, config.Matching.DefaultThreshold);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[e2e error] TemplateStore init failed: {ex.Message}");
            return (int)ExitCode.ConfigOrExecutionFailure;
        }

        using (store)
        {
            var matcher = _matcherFactory is null ? new TemplateMatcher(store) : _matcherFactory(store);

            // ---- step 3-4: capture + match mock_taskbar ----
            Frame frame1;
            try
            {
                frame1 = _capture.Capture(hwnd);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[e2e error] capture before-click failed: {ex.Message}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            try
            {
                Cv2.ImWrite(Path.Combine(e2eDir, "frame_before.png"), frame1.Image);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[e2e warn] write frame_before.png failed: {ex.Message}");
            }

            MatchResult taskbarResult;
            try
            {
                taskbarResult = matcher.Match(frame1.Image, "mock_taskbar", roi: null);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[e2e error] match mock_taskbar threw: {ex.Message}");
                frame1.Image.Dispose();
                return (int)ExitCode.ConfigOrExecutionFailure;
            }
            finally
            {
                frame1.Image.Dispose();
            }

            WriteMatchJson(e2eDir, "match_taskbar.json", taskbarResult);

            if (!taskbarResult.Found)
            {
                Console.Error.WriteLine(
                    $"[e2e FAIL] mock_taskbar not found (score={taskbarResult.Score:F4}, threshold must be ≥ configured)");
                swTotal.Stop();
                Console.WriteLine($"E2E: FAIL  steps=0/{3}  elapsed={swTotal.Elapsed.TotalMilliseconds:F0}ms  evidence={Path.GetFullPath(e2eDir)}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            Console.WriteLine(
                $"[e2e] taskbar found at center=({taskbarResult.Center.X},{taskbarResult.Center.Y}) score={taskbarResult.Score:F4}");

            // ---- step 5: 按钮帧坐标(RJ-S4-01:taskbar 锚点 + scale 推导)----
            // 公式(技术设计 §6.1):scale = frameWidth / layout.Window.Width;
            //                     click = taskbarMeasuredCenter + (buttonLayoutCenter − taskbarLayoutCenter) × scale。
            // 修复前直接把 layout 逻辑坐标当作像素投递,@150% 下 Avalonia 按物理→DIP 换算导致命中 y<180 之外。
            var buttonCenter = ComputeButtonClickPoint(
                frameWidth: frame1.Width,
                layout: layout,
                taskbarMeasuredCenter: taskbarResult.Center);

            // ---- step 6: click ----
            var driver = _driverFactory is null
                ? new PostMessageDriver(config.Input.PostClickDelayMs)
                : _driverFactory(config.Input.PostClickDelayMs);

            ActionResult clickResult;
            try
            {
                clickResult = driver.ClickAsync(hwnd, buttonCenter, ct).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("[e2e error] click cancelled");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[e2e error] click threw: {ex.Message}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            if (!clickResult.Success)
            {
                Console.Error.WriteLine($"[e2e FAIL] click failed: {clickResult.FailReason}");
                swTotal.Stop();
                Console.WriteLine($"E2E: FAIL  steps=1/{3}  elapsed={swTotal.Elapsed.TotalMilliseconds:F0}ms  evidence={Path.GetFullPath(e2eDir)}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            // ---- step 7: 状态轮询(Pathfinding/Arrived,≤5s,§6.1)----
            string? observedState;
            try
            {
                observedState = PollStateToPhase(statePath, "Pathfinding", "Arrived", StatePollTimeoutMs, StatePollIntervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("[e2e error] state poll cancelled");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            if (observedState is null)
            {
                Console.Error.WriteLine("[e2e FAIL] state.json did not reach Pathfinding/Arrived within 5s");
                WriteArtifactState(e2eDir, "state_after_poll_fail.json", statePath);
                swTotal.Stop();
                Console.WriteLine($"E2E: FAIL  steps=2/{3}  elapsed={swTotal.Elapsed.TotalMilliseconds:F0}ms  evidence={Path.GetFullPath(e2eDir)}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            Console.WriteLine($"[e2e] state observed: {observedState}");

            // ---- step 8: 二次 capture + match mock_btn_return ----
            Frame frame2;
            try
            {
                frame2 = _capture.Capture(hwnd);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[e2e error] capture after-click failed: {ex.Message}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            try
            {
                Cv2.ImWrite(Path.Combine(e2eDir, "frame_after.png"), frame2.Image);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[e2e warn] write frame_after.png failed: {ex.Message}");
            }

            WriteArtifactState(e2eDir, "state_after.json", statePath);

            MatchResult btnResult;
            try
            {
                btnResult = matcher.Match(frame2.Image, "mock_btn_return", roi: null);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[e2e error] match mock_btn_return threw: {ex.Message}");
                frame2.Image.Dispose();
                return (int)ExitCode.ConfigOrExecutionFailure;
            }
            finally
            {
                frame2.Image.Dispose();
            }

            WriteMatchJson(e2eDir, "match_btn_return.json", btnResult);

            if (!btnResult.Found)
            {
                Console.Error.WriteLine(
                    $"[e2e FAIL] mock_btn_return not found (score={btnResult.Score:F4}); UI 状态与按钮未一致");
                swTotal.Stop();
                Console.WriteLine($"E2E: FAIL  steps=2/{3}  elapsed={swTotal.Elapsed.TotalMilliseconds:F0}ms  evidence={Path.GetFullPath(e2eDir)}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            // S3 终审约定:按钮判别不做像素中心断言
            Console.WriteLine(
                $"[e2e] mock_btn_return found score={btnResult.Score:F4}(only Found+Score asserted)");

            swTotal.Stop();
            Console.WriteLine($"E2E: PASS  steps=3/3  elapsed={swTotal.Elapsed.TotalMilliseconds:F0}ms  evidence={Path.GetFullPath(e2eDir)}");
            return (int)ExitCode.Success;
        }
    }

    private static bool EnsureIdle(string statePath, out string error)
    {
        error = string.Empty;

        if (!File.Exists(statePath))
        {
            error = $"state.json 不存在: {statePath}(MockGame 未启动或未清理)";
            return false;
        }

        // 等 ≤3s 至 Idle(允许 MockGame 刚启动未写盘);用 Polling.WaitUntilAsync 而非散落 Thread.Sleep(04 §3)
        try
        {
            var ok = Polling.WaitUntilAsync(
                () => Task.FromResult(SafeReadState(statePath) == "Idle"),
                intervalMs: EnsureIdleIntervalMs,
                timeoutMs: EnsureIdleTimeoutMs,
                ct: CancellationToken.None).GetAwaiter().GetResult();
            if (!ok)
            {
                error = $"state.json 未在 {EnsureIdleTimeoutMs}ms 内到达 Idle(当前或不可读)";
            }

            return ok;
        }
        catch (Exception ex)
        {
            error = $"EnsureIdle 异常: {ex.Message}";
            return false;
        }
    }

    private static string? PollStateToPhase(
        string statePath,
        string expectedA,
        string expectedB,
        int timeoutMs,
        int intervalMs,
        CancellationToken ct)
    {
        // 用 Polling.WaitUntilAsync 而非散落 Thread.Sleep(04 §3);sync-over-async 是 IDh2Command.Execute
        // 同步签名所迫,与现有 CaptureCommand 的 Task.Delay.GetAwaiter().GetResult() 一致。
        var reached = Polling.WaitUntilAsync(
            () => Task.FromResult(CheckStateMatch(statePath, expectedA, expectedB)),
            intervalMs: intervalMs,
            timeoutMs: timeoutMs,
            ct: ct).GetAwaiter().GetResult();

        if (!reached)
        {
            return null;
        }

        return SafeReadState(statePath);
    }

    private static bool CheckStateMatch(string statePath, string a, string b)
    {
        var s = SafeReadState(statePath);
        return s == a || s == b;
    }

    private static string? SafeReadState(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("state", out var stateProp)
                ? stateProp.GetString()
                : null;
        }
        catch
        {
            // JSON 中间态(原子写过程)或 IO 抖动 → 返回 null,调用方视为"未满足"
            return null;
        }
    }

    private static void WriteMatchJson(string dir, string name, MatchResult result)
    {
        var payload = new
        {
            found = result.Found,
            score = Math.Round(result.Score, 4),
            location = new[] { result.Location.X, result.Location.Y },
            size = new[] { result.Size.Width, result.Size.Height },
            center = new[] { result.Center.X, result.Center.Y },
        };
        try
        {
            File.WriteAllText(Path.Combine(dir, name), JsonSerializer.Serialize(payload));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[e2e warn] write {name} failed: {ex.Message}");
        }
    }

    private static void WriteArtifactState(string dir, string name, string statePath)
    {
        try
        {
            if (File.Exists(statePath))
            {
                File.Copy(statePath, Path.Combine(dir, name), overwrite: true);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[e2e warn] copy {name} failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 计算按钮的客户区点击坐标(帧像素空间,RJ-S4-01 / 技术设计 §6.1)。
    /// </summary>
    /// <remarks>
    /// <para>公式:<c>scale = frameWidth / layout.Window.Width</c>;点击 = taskbar 实测中心 + (按钮布局中心 − 任务栏布局中心) × scale。</para>
    /// <para>解决 150% DPI 下的坐标空间混用:PostMessage 投递的 (x, y) 由 Avalonia 接收端按物理像素÷缩放系数换算为 DIP;
    /// 若直接用 layout 逻辑坐标 @150% 等同于落点缩到 (x/1.5, y/1.5) DIP,偏出按钮区域。</para>
    /// <para>taskbar 在 layout 中有明确的矩形锚点;以其为参考系把按钮位置先在布局空间算出偏移,
    /// 再乘 scale 投到帧像素空间,加上 taskbar 实测中心即得按钮帧坐标。</para>
    /// <para>由 <c>DH2.Tests</c> 直接调写验证(RJ-S4-01 坐标推导 UT):</para>
    /// <list type="bullet">
    ///   <item>1200×900 帧 + taskbar 实测 (264, 90) → (129, 306)【scale=1.5】</item>
    ///   <item>800×600 帧 + taskbar 实测 (176, 60) → (86, 204)【scale=1.0,退化等同布局中心】</item>
    /// </list>
    /// </remarks>
    /// <param name="frameWidth">当前帧像素宽度(取自 <see cref="Frame.Width"/>,构造期缓存,Dispose 后仍可读)。</param>
    /// <param name="layout">MockGame 布局单一真源(<c>configs/mock-layout.yaml</c>)。</param>
    /// <param name="taskbarMeasuredCenter">任务栏实测命中中心(帧像素空间,来自 <see cref="MatchResult.Center"/>)。</param>
    /// <returns>按钮的帧像素坐标。</returns>
    internal static DH2Point ComputeButtonClickPoint(
        int frameWidth,
        MockLayoutConfig layout,
        DH2Point taskbarMeasuredCenter)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (frameWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameWidth), frameWidth, "frameWidth 必须 > 0");
        }

        if (layout.Window.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "layout.Window.Width",
                layout.Window.Width,
                "layout.Window.Width 必须 > 0");
        }

        var scale = (double)frameWidth / layout.Window.Width;

        var buttonLayoutCenter = new DH2Point(
            layout.Button.X + layout.Button.Width / 2,
            layout.Button.Y + layout.Button.Height / 2);
        var taskbarLayoutCenter = new DH2Point(
            layout.Taskbar.X + layout.Taskbar.Width / 2,
            layout.Taskbar.Y + layout.Taskbar.Height / 2);

        var dx = (buttonLayoutCenter.X - taskbarLayoutCenter.X) * scale;
        var dy = (buttonLayoutCenter.Y - taskbarLayoutCenter.Y) * scale;

        return new DH2Point(
            (int)Math.Round(taskbarMeasuredCenter.X + dx),
            (int)Math.Round(taskbarMeasuredCenter.Y + dy));
    }
}
