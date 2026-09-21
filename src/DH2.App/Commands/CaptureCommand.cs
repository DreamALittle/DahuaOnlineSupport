using System.Diagnostics;
using DH2.App.Cli;
using DH2.Capture;
using DH2.Core.Config;
using DH2.Core.Contracts;
using DH2.Core.Models;
using OpenCvSharp;

namespace DH2.App.Commands;

/// <summary>
/// <c>dh2ctl capture</c> —— 对指定 HWND 客户区连续截屏 <c>--count</c> 帧(技术设计 §6 + S2-4 SAC2-2)。
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>参数:--hwnd &lt;n&gt; / --count &lt;10&gt; / --interval-ms &lt;500&gt; / --out &lt;dir&gt;。</item>
///   <item>输出:每帧 PNG 文件名 <c>frame_{yyyyMMddHHmmssfff}_{i}.png</c> 落到 <c>--out</c>(默认 <c>artifacts/capture-{ts}</c>)。</item>
///   <item>结束打印均值 / p95 耗时(ms)。</item>
///   <item>无 --hwnd → 退出码 2(用法错误);窗口不可见 / 已销毁 → 返回空帧但跳过 ImWrite + 退出码 0(S1 偏离裁决 #3,RJ-S2-02 加固)。</item>
/// </list>
/// <para>M0-S2 改造(RJ-S2-02 / DEF-S2-02):</para>
/// <list type="bullet">
///   <item>WriteLine 之前把 <c>Width</c> / <c>Height</c> 缓存到局部变量(防御深度,Frame.Width/Height 也已构造期缓存);</item>
///   <item>空 Mat(<c>Image.Empty</c>)不再尝试 <c>Cv2.ImWrite</c>(避免 OpenCV 抛"找不到匹配 writer"),按 S1 裁决 #3 视为占位帧、跳过写盘;</item>
///   <item><c>frame.Image</c> 在 try/finally 中保证 <c>Dispose</c> 一次(空帧与非空帧均走同一释放路径);</item>
///   <item><c>sw.Stop()</c> 移到 WriteLine 之前,维持原有"duration = capture + write"测量语义。</item>
/// </list>
/// </remarks>
public sealed class CaptureCommand : IDh2Command
{
    public string Name => "capture";

    private const int DefaultCount = 10;
    private const int DefaultIntervalMs = 500;

    private readonly IFrameCapture _capture;

    public CaptureCommand(IFrameCapture? capture = null)
    {
        _capture = capture ?? new GdiCapture();
    }

    public int Execute(DevConfig config, IReadOnlyDictionary<string, string> options, CancellationToken ct)
    {
        if (!options.TryGetValue("hwnd", out var hwndStr) || !long.TryParse(hwndStr, out var hwnd) || hwnd <= 0)
        {
            Console.Error.WriteLine("[usage error] --hwnd <n> required (positive long)");
            return (int)ExitCode.UsageError;
        }

        // RJ-S2-04: --count 与 --interval-ms 非法值不再静默回退默认值,
        // 而是与 --hwnd 一致报 usage error 并退出码 2。
        var count = DefaultCount;
        if (options.TryGetValue("count", out var countStr))
        {
            if (!int.TryParse(countStr, out var c))
            {
                Console.Error.WriteLine($"[usage error] --count must be an integer; got '{countStr}'");
                return (int)ExitCode.UsageError;
            }
            if (c <= 0)
            {
                Console.Error.WriteLine($"[usage error] --count must be >= 1; got {c}");
                return (int)ExitCode.UsageError;
            }
            count = c;
        }

        var intervalMs = DefaultIntervalMs;
        if (options.TryGetValue("interval-ms", out var ivStr))
        {
            if (!int.TryParse(ivStr, out var iv))
            {
                Console.Error.WriteLine($"[usage error] --interval-ms must be an integer; got '{ivStr}'");
                return (int)ExitCode.UsageError;
            }
            if (iv < 0)
            {
                Console.Error.WriteLine($"[usage error] --interval-ms must be >= 0; got {iv}");
                return (int)ExitCode.UsageError;
            }
            intervalMs = iv;
        }

        var outDir = options.TryGetValue("out", out var outStr) && !string.IsNullOrWhiteSpace(outStr)
            ? outStr
            : Path.Combine("artifacts", $"capture-{DateTime.UtcNow:yyyyMMddHHmmss}");

        Directory.CreateDirectory(outDir);

        var sw = new Stopwatch();
        var frameDurationsMs = new double[count];

        for (var i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();

            sw.Restart();
            Frame frame;
            try
            {
                frame = _capture.Capture(hwnd);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[capture error] frame {i}: {ex.Message}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            // RJ-S2-02 修复:Dispose 前缓存 Width/Height,确保 WriteLine 安全读取。
            var frameWidth = frame.Width;
            var frameHeight = frame.Height;
            var fileName = $"frame_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{i:D3}.png";
            var filePath = Path.Combine(outDir, fileName);

            // RJ-S2-02 修复:空 Mat 不尝试 ImWrite,避免 OpenCV 抛"找不到匹配 writer"。
            // 注意:OpenCvSharp 的 Mat.Empty 是 Func<bool> lambda 属性,需 () 调用。
            var emptyFrame = frame.Image.Empty();
            var writeError = false;
            if (!emptyFrame)
            {
                try
                {
                    Cv2.ImWrite(filePath, frame.Image);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[capture error] write frame {i}: {ex.Message}");
                    writeError = true;
                }
            }

            // 一次性释放(空帧与非空帧均走同一路径;防止双重释放与访问违例)。
            frame.Image.Dispose();

            sw.Stop();
            frameDurationsMs[i] = sw.Elapsed.TotalMilliseconds;

            if (writeError)
            {
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            var status = emptyFrame ? "(empty frame, skipped)" : ("-> " + fileName);
            Console.WriteLine($"frame {i + 1}/{count}  hwnd=0x{hwnd:X}  size={frameWidth}x{frameHeight}  duration={frameDurationsMs[i]:F1}ms  {status}");

            if (i + 1 < count && intervalMs > 0)
            {
                try
                {
                    Task.Delay(intervalMs, ct).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    return (int)ExitCode.Success;
                }
            }
        }

        // 统计
        if (frameDurationsMs.Length > 0)
        {
            Array.Sort(frameDurationsMs);
            var mean = frameDurationsMs.Average();
            var p95Index = (int)Math.Ceiling(0.95 * frameDurationsMs.Length) - 1;
            if (p95Index < 0) p95Index = 0;
            if (p95Index >= frameDurationsMs.Length) p95Index = frameDurationsMs.Length - 1;
            var p95 = frameDurationsMs[p95Index];
            Console.WriteLine($"stats: mean={mean:F1}ms  p95={p95:F1}ms  out={Path.GetFullPath(outDir)}");
        }

        return (int)ExitCode.Success;
    }
}
