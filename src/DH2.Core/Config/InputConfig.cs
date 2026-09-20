namespace DH2.Core.Config;

/// <summary>
/// 输入驱动配置。
/// </summary>
/// <remarks>
/// M0-S2 改造(RJ-S2-01 / DEF-S2-01):
/// 从 <c>sealed record InputConfig(string Driver = "background", int PostClickDelayMs = 50)</c>
/// 改为 <c>sealed class</c> + 无参构造 + settable 属性,以兼容 YamlDotNet 16 反序列化。
/// </remarks>
public sealed class InputConfig
{
    /// <summary>
    /// 输入驱动:<c>background</c>=后台 PostMessage(M0 实现);<c>foreground</c>=焦点轮转(M0 未实现,启动即报"未实现"错误)。
    /// </summary>
    public string Driver { get; set; } = "background";

    /// <summary>点击序列中 <c>WM_LBUTTONDOWN</c> 与 <c>WM_LBUTTONUP</c> 之间的延迟(毫秒)。</summary>
    public int PostClickDelayMs { get; set; } = 50;

    /// <summary>无参构造:供 YamlDotNet 实例化。</summary>
    public InputConfig()
    {
    }

    /// <summary>位置构造:供产品代码显式构造。</summary>
    public InputConfig(string driver, int postClickDelayMs)
    {
        Driver = driver;
        PostClickDelayMs = postClickDelayMs;
    }
}
