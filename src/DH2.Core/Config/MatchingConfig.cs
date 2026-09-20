namespace DH2.Core.Config;

/// <summary>
/// 匹配阈值配置。
/// </summary>
/// <param name="DefaultThreshold">默认匹配阈值,应用于 manifest 未显式给出 <c>threshold</c> 的模板;范围 (0.5, 1.0)。</param>
public sealed record MatchingConfig(double DefaultThreshold = 0.85);