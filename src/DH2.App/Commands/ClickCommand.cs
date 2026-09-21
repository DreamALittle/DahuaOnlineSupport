using System.Text.Json;
using DH2.App.Cli;
using DH2.Core.Config;
using DH2.Core.Contracts;
using DH2.Core.Models;
using DH2.Input;

namespace DH2.App.Commands;

/// <summary>
/// <c>dh2ctl click</c> —— 后台 PostMessage 点击客户区坐标(技术设计 §6 + S4-2 / IT-04 / IT-07)。
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>参数:<c>--hwnd &lt;n&gt; --x --y</c>(客户区像素,@100% 缩放 DIP == 像素)。</item>
///   <item>消费 <see cref="IInputDriver"/>;默认 <c>new PostMessageDriver()</c>(Dev B S4-1 推送后即生效)。</item>
///   <item>输出 JSON 行:<c>{"success":true,"attempts":1,"failReason":""}</c>(技术设计 §3.3 + Core.Models.ActionResult)。</item>
///   <item>用法错误 → 退出码 2;坐标非法(<see cref="InvalidDataException"/>)→ 退出码 3;成功 → 退出码 0。</item>
/// </list>
/// <para>RJ-S2-03(Dev B GdiCapture Mat 生命周期)保证 raw 坐标与本命令点击坐标一致
/// (BitmapConverter.ToMat 共享内存 → Clone 独立副本,避免 read-after-write)。</para>
/// </remarks>
public sealed class ClickCommand : IDh2Command
{
    public string Name => "click";

    private readonly IInputDriver _driver;

    public ClickCommand(IInputDriver? driver = null)
    {
        _driver = driver ?? new PostMessageDriver();
    }

    public int Execute(DevConfig config, IReadOnlyDictionary<string, string> options, CancellationToken ct)
    {
        if (!options.TryGetValue("hwnd", out var hwndStr)
            || !long.TryParse(hwndStr, out var hwnd) || hwnd <= 0)
        {
            Console.Error.WriteLine("[usage error] --hwnd <n> required (positive long)");
            return (int)ExitCode.UsageError;
        }

        if (!options.TryGetValue("x", out var xStr) || !int.TryParse(xStr, out var x) || x < 0)
        {
            Console.Error.WriteLine("[usage error] --x <int> required (non-negative)");
            return (int)ExitCode.UsageError;
        }

        if (!options.TryGetValue("y", out var yStr) || !int.TryParse(yStr, out var y) || y < 0)
        {
            Console.Error.WriteLine("[usage error] --y <int> required (non-negative)");
            return (int)ExitCode.UsageError;
        }

        ActionResult result;
        try
        {
            result = _driver.Click(hwnd, x, y);
        }
        catch (NotImplementedException ex)
        {
            // Dev B S4-1 未推送期间的桥接:桩抛 NotImplementedException → 视为配置/执行失败(退出码 3)。
            Console.Error.WriteLine($"[click error] {ex.Message}");
            return (int)ExitCode.ConfigOrExecutionFailure;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[click error] {ex.Message}");
            return (int)ExitCode.ConfigOrExecutionFailure;
        }

        // JSON 行输出(技术设计 §6 命令输出格式 + Core.Models.ActionResult)
        var payload = new
        {
            success = result.Success,
            attempts = result.Attempts,
            failReason = result.FailReason ?? string.Empty,
        };
        Console.WriteLine(JsonSerializer.Serialize(payload));

        // PostMessage 失败但已结构化返回(非异常)→ 退出码 0(Success 字段表达失败,符合规范 "业务失败不抛异常")
        // 真机用户从 JSON 读 success=false 即可,不依赖进程退出码。
        return (int)ExitCode.Success;
    }
}
