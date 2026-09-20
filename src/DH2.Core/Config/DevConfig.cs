namespace DH2.Core.Config;

/// <summary>
/// 根配置(dev.yaml / game.yaml);启动时由 <see cref="ConfigLoader.Load"/> 反序列化并由 <see cref="ConfigValidator"/> 校验。
/// </summary>
/// <remarks>
/// M0-S2 改造(RJ-S2-01 / DEF-S2-01):
/// 所有属性从 <c>{ get; init; }</c> 改为 <c>{ get; set; }</c>,以兼容 YamlDotNet 16 反序列化
/// (init-only 写入会被运行时抛异常;原因同 S1 DEF-S1-01,被 RJ-S1-03 漏修)。
/// </remarks>
public sealed class DevConfig
{
    /// <summary>当前激活的模板档案名(对应 <c>templates/{Profile}/</c> 目录)。</summary>
    public string Profile { get; set; } = "mock_800x600";

    /// <summary>窗口目标列表(可多个,如真机五开)。</summary>
    public List<WindowTargetConfig> Targets { get; set; } = [];

    /// <summary>路径配置。</summary>
    public PathConfig Paths { get; set; } = new();

    /// <summary>匹配阈值配置。</summary>
    public MatchingConfig Matching { get; set; } = new();

    /// <summary>输入驱动配置。</summary>
    public InputConfig Input { get; set; } = new();
}
