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
///   <item>无 --hwnd → 退出码 2(用法错误);窗口不可见 / 已销毁 → 返回空帧但仍写占位 PNG + 退出码 0(S1 偏离裁决 #3)。</item>
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

        var count = options.TryGetValue("count", out var countStr) && int.TryParse(countStr, out var c) && c > 0
            ? c
            : DefaultCount;
        var intervalMs = options.TryGetValue("interval-ms", out var ivStr) && int.TryParse(ivStr, out var iv) && iv >= 0
            ? iv
            : DefaultIntervalMs;

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

            var fileName = $"frame_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{i:D3}.png";
            var filePath = Path.Combine(outDir, fileName);

            try
            {
                // 空帧(Image 为空 Mat)按裁决 #3:仍写占位 PNG(0×0)便于调试断言不抛异常
                Cv2.ImWrite(filePath, frame.Image);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[capture error] write frame {i}: {ex.Message}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }
            finally
            {
                frame.Image.Dispose();
            }

            sw.Stop();
            frameDurationsMs[i] = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"frame {i + 1}/{count}  hwnd=0x{hwnd:X}  size={frame.Width}x{frame.Height}  duration={frameDurationsMs[i]:F1}ms  -> {fileName}");

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
