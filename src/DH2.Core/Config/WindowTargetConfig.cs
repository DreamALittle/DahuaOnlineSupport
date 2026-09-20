namespace DH2.Core.Config;

/// <summary>
/// 窗口目标配置:按标题正则 / 进程名过滤可见顶层窗口。
/// </summary>
/// <remarks>
/// M0-S2 改造(RJ-S2-01 / DEF-S2-01):
/// 从 <c>sealed record WindowTargetConfig(string Name, string? TitlePattern, string? ProcessName)</c>
/// 改为 <c>sealed class</c> + 无参构造 + settable 属性,以兼容 YamlDotNet 16 反序列化。
/// </remarks>
public sealed class WindowTargetConfig
{
    /// <summary>目标显示名(如 <c>mock</c> / <c>game</c>),仅做日志与展示。</summary>
    public string Name { get; set; } = "";

    /// <summary>窗口标题正则;与 <see cref="ProcessName"/> 至少一项非空(由 <see cref="ConfigValidator"/> 强制)。</summary>
    public string? TitlePattern { get; set; }

    /// <summary>进程名(不含扩展名);可与 <see cref="TitlePattern"/> 同时存在(取交集)。</summary>
    public string? ProcessName { get; set; }

    /// <summary>无参构造:供 YamlDotNet 实例化。</summary>
    public WindowTargetConfig()
    {
    }

    /// <summary>位置构造:供产品代码显式构造。</summary>
    public WindowTargetConfig(string name, string? titlePattern, string? processName)
    {
        Name = name;
        TitlePattern = titlePattern;
        ProcessName = processName;
    }
}
