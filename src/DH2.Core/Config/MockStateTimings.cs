namespace DH2.Core.Config;

/// <summary>
/// MockGame 状态机计时参数(技术设计 §7)。
/// </summary>
/// <remarks>
/// M0-S2 改造:从 <c>sealed record</c> 改为 <c>class</c>,以兼容 YamlDotNet 反序列化(RJ-S1-03 / DEF-S1-01)。
/// </remarks>
public sealed class MockStateTimings
{
    /// <summary>Idle → Pathfinding 持续毫秒;超时后转入 Arrived。</summary>
    public int PathfindingMs { get; set; } = 2000;
}