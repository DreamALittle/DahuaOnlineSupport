using System.Text;
using DH2.App.Cli;
using DH2.App.Logging;
using DH2.Core.Config;
using Serilog;

namespace DH2.App;

/// <summary>
/// dh2ctl 入口(S2-3 CLI 骨架 + 渐进接入 S2-4/S3/S4 命令)。
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>退出码:0 成功 / 2 用法错误 / 3 配置或执行失败(技术设计 §6)。</item>
///   <item>解析 / 配置 / 校验 全部失败 → 列出错误后退出。</item>
///   <item>S2-3 仅交付骨架;enumerate / capture 命令在 S2-4 接入(消费 Dev B 的 IWindowLocator / IFrameCapture)。</item>
/// </list>
/// </remarks>
internal static class Program
{
    private const string ToolName = "dh2ctl";
    private const string ToolVersion = "M0-S2";

    private static int Main(string[] args)
    {
        // 第一步:解析参数;帮助请求立即退出 0
        ParsedCommand parsed;
        try
        {
            parsed = CommandParser.Parse(args);
        }
        catch (CommandParseException ex) when (ex.IsHelp)
        {
            PrintHelp();
            return (int)ExitCode.Success;
        }
        catch (CommandParseException ex)
        {
            Console.Error.WriteLine($"[usage error] {ex.Message}");
            PrintUsage();
            return (int)ExitCode.UsageError;
        }

        var commandName = parsed.Subcommand ?? "(none)";

        // 第二步:Serilog 装配(Console + File)
        using var logger = SerilogBootstrap.Create(commandName);

        try
        {
            // 无子命令:打印帮助并返回 0(类 Unix 工具惯例)
            if (parsed.Subcommand is null)
            {
                PrintHelp();
                return (int)ExitCode.Success;
            }

            // 第三步:加载并校验配置
            var configPath = parsed.ConfigPath ?? CommandParser.DefaultConfigPath;
            Log.Information("Loading config from {ConfigPath}", configPath);

            DevConfig config;
            try
            {
                config = ConfigLoader.Load(configPath);
            }
            catch (FileNotFoundException ex)
            {
                Log.Error(ex, "Config file not found");
                Console.Error.WriteLine($"[config error] {ex.Message}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }
            catch (InvalidDataException ex)
            {
                Log.Error(ex, "Config YAML parse failed");
                Console.Error.WriteLine($"[config error] {ex.Message}");
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            var validationErrors = ConfigValidator.Validate(config);
            if (validationErrors.Count > 0)
            {
                Log.Error("Config validation failed with {Count} error(s)", validationErrors.Count);
                foreach (var err in validationErrors)
                {
                    Console.Error.WriteLine($"  - {err.Path}: {err.Message}");
                }
                return (int)ExitCode.ConfigOrExecutionFailure;
            }

            // 第四步:派发到子命令(M0-S2 仅骨架;具体命令 S2-4/S3/S4 接入)
            return DispatchSubcommand(parsed, config);
        }
        catch (Exception ex)
        {
            // 基础设施兜底:任何运行期异常 → 退出码 3 + 错误日志
            Log.Error(ex, "Unhandled exception");
            Console.Error.WriteLine($"[fatal] {ex.Message}");
            return (int)ExitCode.ConfigOrExecutionFailure;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// 子命令派发。M0-S2 骨架仅占位:S2-4 / S3 / S4 命令尚未实现时返回退出码 2。
    /// </summary>
    private static int DispatchSubcommand(ParsedCommand parsed, DevConfig config)
    {
        var sub = parsed.Subcommand!;
        switch (sub)
        {
            case "enumerate":
            case "capture":
                // S2-4 由 Dev A 在 dev-a/m0-s2 后续提交接入;本轮仅骨架 → 提示并退出 2。
                Console.Error.WriteLine($"[not implemented] '{sub}' arrives in S2-4.");
                return (int)ExitCode.UsageError;

            default:
                Console.Error.WriteLine($"[usage error] unknown subcommand '{sub}'");
                PrintUsage();
                return (int)ExitCode.UsageError;
        }
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine($"Usage: {ToolName} <subcommand> [--config <path>] [--key value]...");
        Console.Error.WriteLine("Subcommands:");
        Console.Error.WriteLine("  enumerate    list windows matching config (S2-4)");
        Console.Error.WriteLine("  capture      capture N frames of a window (S2-4)");
        Console.Error.WriteLine("  save-template (S3)");
        Console.Error.WriteLine("  match         (S3)");
        Console.Error.WriteLine("  click         (S4)");
        Console.Error.WriteLine("  e2e           (S4)");
        Console.Error.WriteLine("  report        (S4)");
        Console.Error.WriteLine($"Default --config: {CommandParser.DefaultConfigPath}");
        Console.Error.WriteLine("Use --help to print full help.");
    }

    private static void PrintHelp()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{ToolName} {ToolVersion} — 大话西游2五开辅助 CLI 控制台");
        sb.AppendLine();
        sb.AppendLine("Usage:");
        sb.AppendLine($"  {ToolName} <subcommand> [--config <path>] [--key value]...");
        sb.AppendLine();
        sb.AppendLine("Subcommands (Sprint 节奏渐进接入):");
        sb.AppendLine("  enumerate       列出匹配 dev.yaml 的目标窗口              [S2-4]");
        sb.AppendLine("  capture         连续截屏指定窗口客户区                  [S2-4]");
        sb.AppendLine("  save-template   截屏并登记为模板                         [S3]");
        sb.AppendLine("  match           在新截屏上定位已登记模板                  [S3]");
        sb.AppendLine("  click           后台 PostMessage 点击客户区坐标          [S4]");
        sb.AppendLine("  e2e             MockGame 端到端闭环(任务栏→按钮→状态)   [S4]");
        sb.AppendLine("  report          真机验证向导                              [S4]");
        sb.AppendLine();
        sb.AppendLine("Common flags:");
        sb.AppendLine($"  --config <path> YAML 配置文件路径(默认 {CommandParser.DefaultConfigPath})");
        sb.AppendLine("  --hwnd <n>      目标窗口句柄(S2-4 起)");
        sb.AppendLine("  --help / -h     打印本帮助");
        sb.AppendLine();
        sb.AppendLine("Exit codes: 0 success | 2 usage error | 3 config/exec failure");
        Console.WriteLine(sb.ToString());
    }
}