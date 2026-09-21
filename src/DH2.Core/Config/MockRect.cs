namespace DH2.Core.Config;

/// <summary>
/// MockGame 元素矩形(X, Y, W, H),含几何中心(技术设计 §7)。
/// </summary>
/// <remarks>
/// M0-S2 改造(RJ-S1-03 / DEF-S1-01):
/// <list type="bullet">
///   <item>从 <c>sealed record MockRect(int, int, int, int)</c> 改为 <c>sealed class</c>,同时提供
///         无参构造(YamlDotNet 反序列化所需)与四参构造(UT-06 <c>RectCenter_LargerRect_ComputesCorrectly</c> 与既有调用方)。</item>
///   <item><see cref="Center"/> 维持 tuple 形式,便于纯函数断言。</item>
///   <item>本类不挂载于 <see cref="MockLayoutConfig"/>(<c>Taskbar</c> / <c>Button</c> 用各自的富字段类型);仅作为通用矩形工具与 UT 几何用例的入口。</item>
/// </list>
/// </remarks>
public sealed class MockRect
{
    /// <summary>左上角 X(客户区坐标系)。</summary>
    public int X { get; set; }

    /// <summary>左上角 Y。</summary>
    public int Y { get; set; }

    /// <summary>宽度,默认 0。</summary>
    public int Width { get; set; }

    /// <summary>高度,默认 0。</summary>
    public int Height { get; set; }

    /// <summary>几何中心像素坐标。</summary>
    public (int Cx, int Cy) Center => (X + Width / 2, Y + Height / 2);

    /// <summary>无参构造:供 YamlDotNet 反序列化与默认初始化。</summary>
    public MockRect()
    {
    }

    /// <summary>位置构造:供测试与既有调用方使用。</summary>
    /// <param name="x">左上角 X。</param>
    /// <param name="y">左上角 Y。</param>
    /// <param name="width">宽度。</param>
    /// <param name="height">高度。</param>
    public MockRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
