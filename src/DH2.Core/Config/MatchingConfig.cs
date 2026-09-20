namespace DH2.Core.Config;

/// <summary>
/// 匹配阈值配置。
/// </summary>
/// <remarks>
/// M0-S2 改造(RJ-S2-01 / DEF-S2-01):
/// 从 <c>sealed record MatchingConfig(double DefaultThreshold = 0.85)</c> 改为 <c>sealed class</c> +
/// 无参构造 + settable 属性,以兼容 YamlDotNet 16 反序列化。
/// </remarks>
public sealed class MatchingConfig
{
    /// <summary>默认匹配阈值,应用于 manifest 未显式给出 <c>threshold</c> 的模板;范围 (0.5, 1.0)。</summary>
    public double DefaultThreshold { get; set; } = 0.85;

    /// <summary>无参构造:供 YamlDotNet 实例化。</summary>
    public MatchingConfig()
    {
    }

    /// <summary>位置构造:供产品代码显式构造。</summary>
    public MatchingConfig(double defaultThreshold)
    {
        DefaultThreshold = defaultThreshold;
    }
}
