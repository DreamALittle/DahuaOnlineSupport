using DH2.App.Cli;
using DH2.Core.Config;
using DH2.Core.Contracts;
using DH2.Core.Models;
using DH2.Input;

namespace DH2.App.Commands;

/// <summary>
/// <c>dh2ctl enumerate</c> —— 按 <see cref="DevConfig.Targets"/> 枚举可见顶层窗口(技术设计 §6 + S2-4 SAC2-1)。
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>输出:控制台 TSV(列:Hwnd / Title / Process / X / Y / Width / Height);标题行 + 数据行。</item>
///   <item>多 Target 时按顺序拼接,各 Target 之间空行分隔。</item>
///   <item>无目标匹配 → 输出空表 + 退出码 0(非错误)。</item>
/// </list>
/// </remarks>
public sealed class EnumerateCommand : IDh2Command
{
    public string Name => "enumerate";

    private readonly IWindowLocator _locator;

    public EnumerateCommand(IWindowLocator? locator = null)
    {
        _locator = locator ?? new WindowEnumerator();
    }

    public int Execute(DevConfig config, IReadOnlyDictionary<string, string> options, CancellationToken ct)
    {
        if (config.Targets.Count == 0)
        {
            // 已在 ConfigValidator 校验失败,本路径不应触发
            return (int)ExitCode.ConfigOrExecutionFailure;
        }

        var first = true;
        foreach (var target in config.Targets)
        {
            ct.ThrowIfCancellationRequested();

            if (!first)
            {
                Console.WriteLine();
            }
            first = false;

            Console.WriteLine($"# target: {target.Name}");
            IReadOnlyList<Win32Window> windows;
            try
            {
                windows = _locator.Enumerate(target);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[enumerate error] target '{target.Name}': {ex.Message}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            var rows = windows.Select(w => new[]
            {
                w.Hwnd.ToString(),
                w.Title,
                w.ProcessName,
                w.Bounds.X.ToString(),
                w.Bounds.Y.ToString(),
                w.Bounds.Width.ToString(),
                w.Bounds.Height.ToString(),
            });

            ConsoleTable.PrintTabbed(
                new[] { "Hwnd", "Title", "Process", "X", "Y", "Width", "Height" },
                rows);
        }

        return (int)ExitCode.Success;
    }
}
