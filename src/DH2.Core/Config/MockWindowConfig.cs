namespace DH2.Core.Config;

/// <summary>
/// MockGame 客户区尺寸(@100% 缩放下 1 DIP = 1 px)。
/// </summary>
/// <param name="Width">客户区宽度。</param>
/// <param name="Height">客户区高度。</param>
public sealed record MockWindowConfig(int Width, int Height);