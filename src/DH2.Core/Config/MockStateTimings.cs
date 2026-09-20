namespace DH2.Core.Config;

/// <summary>
/// MockGame 状态机计时参数(技术设计 §7)。
/// </summary>
/// <param name="PathfindingMs">Idle → Pathfinding 持续毫秒;超时后转入 Arrived。</param>
public sealed record MockStateTimings(int PathfindingMs);