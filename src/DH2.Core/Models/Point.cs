namespace DH2.Core.Models;

/// <summary>
/// 二维整数坐标点(像素 / DIP,@100% 缩放下两者一致)。
/// </summary>
/// <remarks>
/// 作为 <see cref="Rect"/> / <see cref="MatchResult"/> 等结构的分量;
/// 坐标原点在客户区左上角(0, 0),向右为 X 正向,向下为 Y 正向。
/// </remarks>
public readonly record struct Point(int X, int Y);
