namespace DH2.Core.Config;

/// <summary>
/// MockGame 元素矩形(X, Y, W, H)。用于任务栏与主按钮。
/// </summary>
/// <param name="X">左上角 X(客户区坐标系)。</param>
/// <param name="Y">左上角 Y。</param>
/// <param name="Width">宽度,必须 ≥ 0。</param>
/// <param name="Height">高度,必须 ≥ 0。</param>
public sealed record MockRect(int X, int Y, int Width, int Height)
{
    /// <summary>几何中心像素坐标。</summary>
    public (int Cx, int Cy) Center => (X + Width / 2, Y + Height / 2);
}