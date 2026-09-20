namespace DH2.Core.Config;

/// <summary>
/// MockGame 任务追踪栏矩形 + 样式 + 文本模板(技术设计 §7:任务追踪栏)。
/// </summary>
/// <remarks>
/// M0-S2 引入(RJ-S1-03 / DEF-S1-01 修复):
/// <list type="bullet">
///   <item>替换原 MockRect 子记录,扩展为承载 Background / BorderColor / BorderThickness / TextColor / FontSize / TextTemplate 的独立 <c>class</c>。</item>
///   <item>提供参数化无参构造以兼容 YamlDotNet 反序列化(避开 <c>sealed record</c> 缺无参构造的坑)。</item>
///   <item><see cref="Center"/> 维持 tuple 形式,UT-06 期望 (176, 60)。</item>
/// </list>
/// </remarks>
public sealed class MockTaskbarConfig
{
    /// <summary>任务追踪栏矩形 X。</summary>
    public int X { get; set; } = 16;

    /// <summary>任务追踪栏矩形 Y。</summary>
    public int Y { get; set; } = 16;

    /// <summary>任务追踪栏宽度。</summary>
    public int Width { get; set; } = 320;

    /// <summary>任务追踪栏高度。</summary>
    public int Height { get; set; } = 88;

    /// <summary>背景色(HTML 十六进制)。</summary>
    public string Background { get; set; } = "#1E1E2E";

    /// <summary>边框颜色。</summary>
    public string BorderColor { get; set; } = "#555555";

    /// <summary>边框厚度(像素)。</summary>
    public int BorderThickness { get; set; } = 2;

    /// <summary>文字颜色。</summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>字体大小。</summary>
    public int FontSize { get; set; } = 20;

    /// <summary>文本模板(<c>{n}</c> 占位 = 任务计数)。</summary>
    public string TextTemplate { get; set; } = "师门任务 ({n}/20)";

    /// <summary>几何中心(UT-06 期望 (176, 60))。</summary>
    public (int Cx, int Cy) Center => (X + Width / 2, Y + Height / 2);
}
