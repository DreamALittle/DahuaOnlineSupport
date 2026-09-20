namespace DH2.Core.Config;

/// <summary>
/// MockGame 状态文本锚点 + 样式(技术设计 §7:状态)。
/// </summary>
/// <remarks>
/// M0-S2 改造:从 <c>sealed record</c> 改为 <c>class</c>,以兼容 YamlDotNet 反序列化(RJ-S1-03 / DEF-S1-01)。
/// </remarks>
public sealed class MockStatusPosition
{
    /// <summary>状态文本锚点 X。</summary>
    public int X { get; set; } = 16;

    /// <summary>状态文本锚点 Y。</summary>
    public int Y { get; set; } = 116;

    /// <summary>文字颜色(HTML 十六进制,如 <c>#FFFFFF</c>)。</summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>字体大小(像素)。</summary>
    public int FontSize { get; set; } = 18;

    /// <summary>各状态对应显示文本(idle/pathfinding/arrived)。</summary>
    public MockStatusTexts Texts { get; set; } = new();
}
