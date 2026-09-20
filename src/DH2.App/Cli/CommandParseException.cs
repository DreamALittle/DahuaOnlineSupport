namespace DH2.App.Cli;

/// <summary>
/// 命令行解析异常(<see cref="CommandParser"/> 抛出,宿主以退出码 2 处理;帮助请求 isHelp=true 时退出码 0)。
/// </summary>
public sealed class CommandParseException : Exception
{
    /// <summary>是否帮助请求(--help / -h)。</summary>
    public bool IsHelp { get; }

    public CommandParseException(string message, bool isHelp = false) : base(message)
    {
        IsHelp = isHelp;
    }
}
