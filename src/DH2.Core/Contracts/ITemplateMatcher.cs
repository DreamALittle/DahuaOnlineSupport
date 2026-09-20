using DH2.Core.Models;
using Mat = OpenCvSharp.Mat;
using Rect = DH2.Core.Models.Rect;

namespace DH2.Core.Contracts;

/// <summary>
/// 模板匹配抽象。
/// </summary>
/// <remarks>
/// M0 实现: <c>DH2.Vision.TemplateMatcher</c>(S3,Dev B),算法 <c>Cv2.MatchTemplate(TM_CCOEFF_NORMED)</c>。
/// 单模板无 mask;ROI 裁剪时返回坐标换算回整帧坐标系。
/// </remarks>
public interface ITemplateMatcher : IDisposable
{
    /// <summary>
    /// 在 <paramref name="frame"/> 的指定 ROI 内查找 <paramref name="templateKey"/> 对应模板。
    /// </summary>
    /// <param name="frame">整帧图像(通常来自 <see cref="IFrameCapture.Capture"/> 的 <c>Image</c> 字段)。</param>
    /// <param name="templateKey">模板逻辑键(由 <see cref="ITemplateStore"/> 管理)。</param>
    /// <param name="roi">搜索区域(整帧坐标系);为 <c>null</c> 表示全图搜索。</param>
    /// <returns>
    /// 命中结果:<see cref="MatchResult.Found"/>=<c>true</c> 表示分值 ≥ 该模板阈值;
    /// 模板大于帧 / ROI 时返回 <see cref="MatchResult.Found"/>=<c>false</c>、<see cref="MatchResult.Score"/>=0。
    /// </returns>
    MatchResult Match(Mat frame, string templateKey, Rect? roi = null);
}