using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DH2.App.Logging;

/// <summary>
/// dh2ctl Serilog 装配(技术设计 §9:Console 简 + <c>artifacts/logs/dh2ctl-{date}.log</c> 全量)。
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>Console: Information 级(用户面)。</item>
///   <item>File: Debug 级(全量,含 scope 属性 <c>hwnd</c> / <c>command</c>)。</item>
///   <item>调用方负责 <c>Log.CloseAndFlush()</c> 退出前落盘。</item>
/// </list>
/// </remarks>
public static class SerilogBootstrap
{
    /// <summary>日志目录相对路径(由调用方 resolve 为绝对路径)。</summary>
    public const string LogsSubdirectory = "artifacts/logs";

    /// <summary>日志文件名前缀。</summary>
    public const string LogFilePrefix = "dh2ctl";

    /// <summary>
    /// 创建 <see cref="Logger"/> 并设为 <see cref="Log.Logger"/>。
    /// </summary>
    /// <param name="commandName">scope 注入的命令名(用于结构化字段)。</param>
    /// <param name="logsDirectory">日志输出目录绝对路径;为 <c>null</c> 时使用 <c>{cwd}/artifacts/logs</c>(自动创建)。</param>
    /// <param name="minimumConsole">Console 接收级别,默认 Information。</param>
    /// <param name="minimumFile">File 接收级别,默认 Debug。</param>
    public static Logger Create(
        string commandName,
        string? logsDirectory = null,
        LogEventLevel minimumConsole = LogEventLevel.Information,
        LogEventLevel minimumFile = LogEventLevel.Debug)
    {
        var dir = logsDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), LogsSubdirectory);
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, $"{LogFilePrefix}-{{date}}-{{time}}.log");

        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("command", commandName)
            .WriteTo.Console(restrictedToMinimumLevel: minimumConsole)
            .WriteTo.File(
                filePath,
                restrictedToMinimumLevel: minimumFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Properties:j} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Logger = logger;
        return logger;
    }
}