namespace DH2.MockGame.Models;

/// <summary>
/// MockGame 状态机状态(技术设计 §7:Idle / Pathfinding / Arrived)。
/// 本地 MockGame 内部枚举,不属 Core 域。
/// </summary>
public enum MockPhase
{
    Idle,
    Pathfinding,
    Arrived,
}
