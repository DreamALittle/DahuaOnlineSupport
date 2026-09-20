namespace DH2.Core.Models;

/// <summary>
/// 矩形区域(左上角原点 + 宽高,客户区坐标系)。
/// </summary>
/// <param name="X">左上角 X。</param>
/// <param name="Y">左上角 Y。</param>
/// <param name="Width">宽度,必须 ≥ 0。</param>
/// <param name="Height">高度,必须 ≥ 0。</param>
public sealed record Rect(int X, int Y, int Width, int Height)
{
    /// <summary>矩形几何中心。</summary>
    public Point Center => new(X + Width / 2, Y + Height / 2);
}