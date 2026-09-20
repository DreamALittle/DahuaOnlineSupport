namespace DH2.Core.Config;

/// <summary>
/// MockGame 状态文本三态文案(技术设计 §7:状态机)。
/// </summary>
public sealed class MockStatusTexts
{
    /// <summary>Idle 状态显示文本。</summary>
    public string Idle { get; set; } = "待机";

    /// <summary>Pathfinding 状态显示文本。</summary>
    public string Pathfinding { get; set; } = "寻路中...";

    /// <summary>Arrived 状态显示文本。</summary>
    public string Arrived { get; set; } = "已到达目的地";
}
