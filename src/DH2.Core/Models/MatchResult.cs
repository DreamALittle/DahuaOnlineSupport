namespace DH2.Core.Models;

/// <summary>
/// 模板匹配结果。<see cref="Found"/> 为 <c>false</c> 时其余字段无意义(零值)。
/// </summary>
/// <param name="Found">匹配是否成功(分值 ≥ 模板阈值)。</param>
/// <param name="Score">归一化匹配分值(TM_CCOEFF_NORMED 范围 [-1, 1],M0 仅使用 ≥ 阈值的情形)。</param>
/// <param name="Location">命中区域左上角坐标(整帧坐标系,若搜索 ROI 则已换算回整帧)。</param>
/// <param name="Size">命中区域尺寸(与模板尺寸一致)。</param>
public sealed record MatchResult(bool Found, double Score, Point Location, Size Size)
{
    /// <summary>命中区域几何中心。</summary>
    public Point Center => new(Location.X + Size.Width / 2, Location.Y + Size.Height / 2);
}
