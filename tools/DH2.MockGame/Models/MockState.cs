namespace DH2.MockGame.Models;

/// <summary>
/// MockGame 状态机状态(技术设计 §7:Idle / Pathfinding / Arrived)。
/// </summary>
public enum MockPhase
{
    Idle,
    Pathfinding,
    Arrived,
}

/// <summary>
/// state.json 序列化形态(技术设计 §7:{"state","counter","ts"})。
/// </summary>
public sealed class MockStatePayload
{
    public string State { get; set; } = "Idle";
    public int Counter { get; set; }
    public string Ts { get; set; } = "";
}