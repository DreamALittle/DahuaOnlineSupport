namespace DH2.App.Cli;

/// <summary>
/// dh2ctl 命令行解析器(M0 不引第三方 CLI 库;技术设计 §6)。
/// </summary>
/// <remarks>
/// 支持的语法:
/// <list type="bullet">
///   <item><c>dh2ctl &lt;subcommand&gt; [--config &lt;path&gt;] [--key value]*</c></item>
///   <item><c>dh2ctl --help</c> / <c>dh2ctl -h</c> 触发帮助输出(<see cref="CommandParseException"/> 由宿主 catch 并 exit 0)。</item>
///   <item><c>dh2ctl</c> 无参数:返回 <see cref="ParsedCommand"/> with <c>Subcommand=null</c>。</item>
/// </list>
/// 解析失败抛 <see cref="CommandParseException"/>(宿主以退出码 2 处理)。
/// </remarks>
public static class CommandParser
{
    /// <summary>默认配置文件路径(S2-3 SAC2-3)。</summary>
    public const string DefaultConfigPath = "configs/dev.yaml";

    /// <summary>
    /// 解析 <paramref name="args"/>。
    /// </summary>
    /// <param name="args">命令行参数(<c>args[0]</c> 为子命令起点)。</param>
    /// <returns>解析结果;空 <paramref name="args"/> 时返回 <c>Subcommand=null</c>。</returns>
    /// <exception cref="CommandParseException">参数非法时。</exception>
    public static ParsedCommand Parse(string[]? args)
    {
        if (args is null || args.Length == 0)
        {
            return new ParsedCommand(null, null, new Dictionary<string, string>());
        }

        // 帮助请求
        if (args.Length == 1 && IsHelp(args[0]))
        {
            throw new CommandParseException("help requested", isHelp: true);
        }

        // 第一项 = 子命令
        var subcommand = args[0];
        if (subcommand.StartsWith('-'))
        {
            throw new CommandParseException($"first argument must be a subcommand, got '{subcommand}'");
        }

        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        string? configPath = null;

        var i = 1;
        while (i < args.Length)
        {
            var token = args[i];

            if (token == "--config")
            {
                if (i + 1 >= args.Length)
                {
                    throw new CommandParseException("--config requires a value");
                }
                configPath = args[++i];
            }
            else if (token.StartsWith("--", StringComparison.Ordinal))
            {
                var key = token[2..];
                if (string.IsNullOrEmpty(key))
                {
                    throw new CommandParseException($"empty option name in '{token}'");
                }

                // 下一个 token 是值还是下一个 flag?
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    options[key] = args[++i];
                }
                else
                {
                    options[key] = "true"; // 纯布尔开关
                }
            }
            else if (token.StartsWith('-'))
            {
                throw new CommandParseException($"unsupported short option '{token}'(M0 仅支持 --long)");
            }
            else
            {
                throw new CommandParseException($"unexpected positional argument '{token}' after subcommand");
            }

            i++;
        }

        return new ParsedCommand(subcommand, configPath, options);
    }

    private static bool IsHelp(string token)
        => token is "--help" or "-h" or "/?";
}
