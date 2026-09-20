namespace DH2.Core.Config;

/// <summary>
/// MockGame 客户区尺寸与窗口标题(@100% 缩放下 1 DIP = 1 px,技术设计 §10.2)。
/// </summary>
/// <remarks>
/// M0-S2 改造:从 <c>sealed record MockWindowConfig(int, int)</c> 改为可空无参构造的 <c>class</c>,
/// 以兼容 YamlDotNet 反序列化(RJ-S1-03 / DEF-S1-01)。
/// </remarks>
public sealed class MockWindowConfig
{
    /// <summary>窗口标题(用于日志与展示)。</summary>
    public string Title { get; set; } = "DH2.MockGame";

    /// <summary>客户区宽度。</summary>
    public int Width { get; set; } = 800;

    /// <summary>客户区高度。</summary>
    public int Height { get; set; } = 600;
}