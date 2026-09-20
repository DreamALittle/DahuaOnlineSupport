namespace DH2.App.Cli;

/// <summary>
/// 解析后的命令行载荷。
/// </summary>
/// <param name="Subcommand">子命令名(如 <c>enumerate</c> / <c>capture</c>);<c>null</c> 表示无子命令(打印帮助)。</param>
/// <param name="ConfigPath">
/// <c>--config</c> 指定路径;为 <c>null</c> 时按约定使用 <c>configs/dev.yaml</c>(技术设计 §6 + S2-3 SAC2-3)。
/// </param>
/// <param name="Options">其余 <c>--key value</c> / <c>--flag</c> 选项的扁平字典(保持原始大小写)。</param>
public sealed record ParsedCommand(
    string? Subcommand,
    string? ConfigPath,
    IReadOnlyDictionary<string, string> Options);