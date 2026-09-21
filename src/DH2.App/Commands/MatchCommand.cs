using System.Text.Json;
using DH2.App.Cli;
using DH2.Capture;
using DH2.Core.Config;
using DH2.Core.Contracts;
using DH2.Core.Models;
using DH2.Vision;

namespace DH2.App.Commands;

/// <summary>
/// <c>dh2ctl match</c> —— 在最新截屏上定位已登记模板(技术设计 §6 + S3-3)。
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>参数:--hwnd &lt;n&gt; --key &lt;name&gt; [--config]。</item>
///   <item>流程:Capture frame → <see cref="ITemplateMatcher.Match"/>(整帧 / ROI 全图)→ 输出 JSON 行。</item>
///   <item>JSON 行格式(技术设计 §6):<c>{"found":true,"score":0.97,"location":[x,y],"size":[w,h],"center":[cx,cy]}</c>。Found=false 时 location/size/center 为零值;score=0。</item>
///   <item>用法错误 → 退出码 2;模板未登记 / 帧空 / 截屏失败 → 退出码 3;成功 → 退出码 0。</item>
/// </list>
/// </remarks>
public sealed class MatchCommand : IDh2Command
{
    public string Name => "match";

    private readonly IFrameCapture _capture;
    private readonly Func<ITemplateStore, ITemplateMatcher>? _matcherFactory;

    public MatchCommand(
        IFrameCapture? capture = null,
        Func<ITemplateStore, ITemplateMatcher>? matcherFactory = null)
    {
        _capture = capture ?? new GdiCapture();
        _matcherFactory = matcherFactory;
    }

    public int Execute(DevConfig config, IReadOnlyDictionary<string, string> options, CancellationToken ct)
    {
        if (!options.TryGetValue("hwnd", out var hwndStr)
            || !long.TryParse(hwndStr, out var hwnd) || hwnd <= 0)
        {
            Console.Error.WriteLine("[usage error] --hwnd <n> required (positive long)");
            return (int)ExitCode.UsageError;
        }

        if (!options.TryGetValue("key", out var key) || string.IsNullOrWhiteSpace(key))
        {
            Console.Error.WriteLine("[usage error] --key <name> required (non-empty)");
            return (int)ExitCode.UsageError;
        }

        // 截屏
        Frame frame;
        try
        {
            frame = _capture.Capture(hwnd);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[capture error] {ex.Message}");
            return (int)ExitCode.ConfigOrExecutionFailure;
        }

        // 构造 TemplateStore + TemplateMatcher
        var templatesRoot = config.Paths.Templates;
        var profile = config.Profile;
        var defaultThreshold = config.Matching.DefaultThreshold;

        TemplateStore store;
        try
        {
            store = new TemplateStore(templatesRoot, profile, defaultThreshold);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[match error] TemplateStore init failed: {ex.Message}");
            frame.Image.Dispose();
            return (int)ExitCode.ConfigOrExecutionFailure;
        }

        using (store)
        {
            var matcher = _matcherFactory is null
                ? new TemplateMatcher(store)
                : _matcherFactory(store);

            MatchResult result;
            try
            {
                result = matcher.Match(frame.Image, key, roi: null);
            }
            catch (KeyNotFoundException ex)
            {
                Console.Error.WriteLine($"[match error] template '{key}' not registered");
                Console.Error.WriteLine($"  detail: {ex.Message}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[match error] {ex.Message}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }
            finally
            {
                frame.Image.Dispose();
            }

            // JSON 行输出
            var payload = new
            {
                found = result.Found,
                score = Math.Round(result.Score, 4),
                location = new[] { result.Location.X, result.Location.Y },
                size = new[] { result.Size.Width, result.Size.Height },
                center = new[] { result.Center.X, result.Center.Y },
            };
            Console.WriteLine(JsonSerializer.Serialize(payload));

            return (int)ExitCode.Success;
        }
    }
}
