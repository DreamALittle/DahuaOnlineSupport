namespace DH2.Core.Config;

/// <summary>
/// MockGame 主按钮矩形 + 样式 + 文案(技术设计 §7:主按钮)。
/// </summary>
/// <remarks>
/// M0-S2 改造:替换原 <c>sealed record MockRect MockButtonConfig(...)</c> 为独立 <c>class</c>,
/// 以承载背景色 / 文字色 / 字号 / 双态文案 等字段(RJ-S1-03)。
/// </remarks>
public sealed class MockButtonConfig
{
    /// <summary>按钮矩形 X。</summary>
    public int X { get; set; } = 16;

    /// <summary>按钮矩形 Y。</summary>
    public int Y { get; set; } = 180;

    /// <summary>按钮宽度。</summary>
    public int Width { get; set; } = 140;

    /// <summary>按钮高度。</summary>
    public int Height { get; set; } = 48;

    /// <summary>背景色(HTML 十六进制,如 <c>#2D5BFF</c>)。</summary>
    public string Background { get; set; } = "#2D5BFF";

    /// <summary>文字颜色。</summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>字体大小。</summary>
    public int FontSize { get; set; } = 18;

    /// <summary>双态文案(go = 前往 / return = 返回)。</summary>
    public MockButtonTexts Texts { get; set; } = new();

    /// <summary>几何中心(UT-06 期望 (86, 204))。</summary>
    public (int Cx, int Cy) Center => (X + Width / 2, Y + Height / 2);
}
