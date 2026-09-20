namespace DH2.Core.Models;

/// <summary>
/// 二维整数尺寸(宽 × 高,像素 / DIP)。
/// </summary>
public readonly record struct Size(int Width, int Height);
