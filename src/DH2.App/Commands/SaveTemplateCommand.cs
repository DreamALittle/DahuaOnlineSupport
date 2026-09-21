using System.Text.Json;
using DH2.App.Cli;
using DH2.Capture;
using DH2.Core.Config;
using DH2.Core.Contracts;
using DH2.Core.Models;
using DH2.Vision;
using OpenCvSharp;
using OpenCvRect = OpenCvSharp.Rect;

namespace DH2.App.Commands;

/// <summary>
/// <c>dh2ctl save-template</c> —— 截屏裁剪入库 + manifest 登记(技术设计 §6 + S3-3)。
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>参数:--hwnd &lt;n&gt; --x --y --w --h --key &lt;name&gt; [--config]。</item>
///   <item>流程:Capture frame → 裁剪 (x,y,w,h) → 写 PNG 到 <c>templates/{profile}/png/{key}.png</c> → 更新 <c>manifest.yaml</c>(同 key 幂等覆盖)→ 触发 <see cref="ITemplateStore.Reload"/> 立即生效。</item>
///   <item>用法错误 → 退出码 2;窗口不可见/已销毁/ROI 越界 → 退出码 3;成功 → 退出码 0 + 一行 JSON 输出(含模板条目概要)。</item>
///   <item>输出 JSON:<c>{"saved":true,"key":"...","file":"...png","threshold":0.85,"size":[w,h]}</c>(SAC3-3 断言参考)。</item>
/// </list>
/// <para>M0-S3 实现:</para>
/// <list type="bullet">
///   <item>manifest 写入采用与 <c>DH2.Vision.TemplateManifestConfig</c> 兼容的 DTO(同字段名 / 同 ClickOffset <c>{x,y}</c> 结构);避免因 schema 不一致导致 <see cref="TemplateStore.Reload"/> 失败。</item>
///   <item>不修改 <c>DH2.Vision</c> 命名空间内 <c>internal</c> 类型(我域外);manifest 读写在本命令内自封闭。</item>
/// </list>
/// </remarks>
public sealed class SaveTemplateCommand : IDh2Command
{
    public string Name => "save-template";

    private readonly IFrameCapture _capture;

    public SaveTemplateCommand(IFrameCapture? capture = null)
    {
        _capture = capture ?? new GdiCapture();
    }

    public int Execute(DevConfig config, IReadOnlyDictionary<string, string> options, CancellationToken ct)
    {
        // 1. 参数校验(全部用法错误 → 退出码 2)
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

        if (!TryParseNonNegativeInt(options, "x", out var x, out var err))
        {
            Console.Error.WriteLine(err);
            return (int)ExitCode.UsageError;
        }
        if (!TryParseNonNegativeInt(options, "y", out var y, out err))
        {
            Console.Error.WriteLine(err);
            return (int)ExitCode.UsageError;
        }
        if (!TryParsePositiveInt(options, "w", out var width, out err))
        {
            Console.Error.WriteLine(err);
            return (int)ExitCode.UsageError;
        }
        if (!TryParsePositiveInt(options, "h", out var height, out err))
        {
            Console.Error.WriteLine(err);
            return (int)ExitCode.UsageError;
        }

        // 2. 截屏
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

        // 3. ROI 越界检查(读 frame.Width/Height 是构造期缓存值;RJ-S2-02 防御深度)
        if (frame.Image.Empty())
        {
            Console.Error.WriteLine($"[capture error] empty frame; window may be invisible/destroyed (hwnd=0x{hwnd:X})");
            frame.Image.Dispose();
            return (int)ExitCode.ConfigOrExecutionFailure;
        }
        if (x + width > frame.Width || y + height > frame.Height)
        {
            Console.Error.WriteLine(
                $"[usage error] ROI ({x},{y},{width},{height}) out of frame bounds ({frame.Width}x{frame.Height})");
            frame.Image.Dispose();
            return (int)ExitCode.UsageError;
        }

        // 4. 裁剪(ROI 子图;与源 Mat 共享内存,需谨慎释放 — 这里不主动释放 frame.Image,
        //    因 frame.Image.Dispose() 会令 ROI 失效;改在帧生命周期结束时释放)
        Mat roiMat = new Mat(frame.Image, new OpenCvRect(x, y, width, height));

        try
        {
            // 5. 路径解析 + 写 PNG
            var templatesRoot = config.Paths.Templates;
            var profileDir = Path.Combine(templatesRoot, config.Profile);
            var pngDir = Path.Combine(profileDir, "png");
            Directory.CreateDirectory(pngDir);

            var pngRelPath = $"png/{key}.png";
            var pngAbsPath = Path.Combine(pngDir, key + ".png");
            Cv2.ImWrite(pngAbsPath, roiMat);

            // 6. 更新 manifest.yaml(幂等覆盖同 key)
            var manifestPath = Path.Combine(profileDir, "manifest.yaml");
            ManifestWriter.UpsertEntry(
                manifestPath,
                profile: config.Profile,
                key: key,
                file: pngRelPath,
                threshold: config.Matching.DefaultThreshold);

            // 7. 触发 TemplateStore 热重载(若实例化成功)
            try
            {
                using var store = new TemplateStore(templatesRoot, config.Profile, config.Matching.DefaultThreshold);
                store.Reload();
            }
            catch (Exception ex)
            {
                // Reload 失败(例如 manifest 字段非法)不阻塞本次保存:PNG 已落盘,manifest 已写
                // 仅提示用户 Reload 失败。
                Console.Error.WriteLine($"[warn] TemplateStore.Reload failed: {ex.Message}");
            }

            // 8. 输出 JSON
            var payload = new
            {
                saved = true,
                key,
                file = pngRelPath,
                threshold = config.Matching.DefaultThreshold,
                size = new[] { width, height },
            };
            Console.WriteLine(JsonSerializer.Serialize(payload));

            return (int)ExitCode.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[save-template error] {ex.Message}");
            return (int)ExitCode.ConfigOrExecutionFailure;
        }
        finally
        {
            roiMat.Dispose();
            frame.Image.Dispose();
        }
    }

    private static bool TryParseNonNegativeInt(
        IReadOnlyDictionary<string, string> opts, string key, out int value, out string error)
    {
        value = 0;
        error = "";
        if (!opts.TryGetValue(key, out var s))
        {
            error = $"[usage error] --{key} <int> required";
            return false;
        }
        if (!int.TryParse(s, out var v) || v < 0)
        {
            error = $"[usage error] --{key} must be non-negative integer; got '{s}'";
            return false;
        }
        value = v;
        return true;
    }

    private static bool TryParsePositiveInt(
        IReadOnlyDictionary<string, string> opts, string key, out int value, out string error)
    {
        value = 0;
        error = "";
        if (!opts.TryGetValue(key, out var s))
        {
            error = $"[usage error] --{key} <int> required";
            return false;
        }
        if (!int.TryParse(s, out var v) || v <= 0)
        {
            error = $"[usage error] --{key} must be positive integer; got '{s}'";
            return false;
        }
        value = v;
        return true;
    }
}
