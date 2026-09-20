namespace DH2.Core.Config;

/// <summary>
/// 根配置(dev.yaml / game.yaml);启动时由 <see cref="ConfigLoader.Load"/> 反序列化并由 <see cref="ConfigValidator"/> 校验。
/// </summary>
/// <remarks>
/// 字段语义:
/// <list type="bullet">
///   <item><see cref="Profile"/>:当前激活的模板档案名(对应 <c>templates/{Profile}/</c> 目录)。</item>
///   <item><see cref="Targets"/>:窗口目标列表(可多个,如真机五开)。</item>
///   <item><see cref="Paths"/>:路径配置。</item>
///   <item><see cref="Matching"/>:匹配阈值配置。</item>
///   <item><see cref="Input"/>:输入驱动配置。</item>
/// </list>
/// </remarks>
public sealed class DevConfig
{
    /// <summary>当前激活的模板档案名(对应 <c>templates/{Profile}/</c> 目录)。</summary>
    public string Profile { get; init; } = "mock_800x600";

    /// <summary>窗口目标列表(可多个,如真机五开)。</summary>
    public List<WindowTargetConfig> Targets { get; init; } = [];

    /// <summary>路径配置。</summary>
    public PathConfig Paths { get; init; } = new("templates", "artifacts");

    /// <summary>匹配阈值配置。</summary>
    public MatchingConfig Matching { get; init; } = new();

    /// <summary>输入驱动配置。</summary>
    public InputConfig Input { get; init; } = new();
}
