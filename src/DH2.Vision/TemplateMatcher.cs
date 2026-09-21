using System.Globalization;
using DH2.Core.Contracts;
using DH2.Core.Models;
using OpenCvSharp;
using DH2Point = DH2.Core.Models.Point;
using DH2Rect = DH2.Core.Models.Rect;
using DH2Size = DH2.Core.Models.Size;
using OpenCvRect = OpenCvSharp.Rect;

namespace DH2.Vision;

/// <summary>
/// 模板匹配实现(技术设计 §5.2)。
/// 算法:<see cref="TemplateMatchModes.CCoeffNormed"/>;灰度转换在边界完成(避免 Vision 内部存 BGR 副本)。
/// ROI 裁剪时:在子图上做匹配,命中位置 + ROI 原点 = 整帧坐标系。
/// 模板大于帧 / ROI 时返回 <see cref="MatchResult.Found"/>=false、<see cref="MatchResult.Score"/>=0。
/// </summary>
public sealed class TemplateMatcher : ITemplateMatcher
{
    private readonly ITemplateStore _store;
    private bool _disposed;

    public TemplateMatcher(ITemplateStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public MatchResult Match(Mat frame, string templateKey, DH2Rect? roi = null)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Empty())
        {
            return NotFound();
        }

        var entry = _store.Get(templateKey);
        var templateSize = new OpenCvSize(entry.Image.Cols, entry.Image.Rows);

        // ROI 决定搜索子图尺寸;模板必须 ≤ 子图
        var searchRect = roi is null
            ? new OpenCvRect(0, 0, frame.Cols, frame.Rows)
            : new OpenCvRect(roi.X, roi.Y, roi.Width, roi.Height);

        // 边界检查:ROI 必须落在 frame 内
        if (searchRect.X < 0 || searchRect.Y < 0
            || searchRect.X + searchRect.Width > frame.Cols
            || searchRect.Y + searchRect.Height > frame.Rows)
        {
            return NotFound();
        }

        if (searchRect.Width < templateSize.Width || searchRect.Height < templateSize.Height)
        {
            return NotFound();
        }

        // 灰度转换:ROI 子图
        using var subRoi = new Mat(frame, searchRect);
        using var subGray = new Mat();
        Cv2.CvtColor(subRoi, subGray, ColorConversionCodes.BGR2GRAY);

        // 模板已是灰度(由 TemplateStore.ImRead Grayscale 加载)

        using var result = new Mat();
        Cv2.MatchTemplate(subGray, entry.Image, result, TemplateMatchModes.CCoeffNormed);

        Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out var maxLoc);

        if (maxVal < entry.Threshold)
        {
            return new MatchResult(
                Found: false,
                Score: maxVal,
                Location: new DH2Point(0, 0),
                Size: new DH2Size(0, 0));
        }

        // ROI 子图坐标系 → 整帧坐标系
        var frameX = maxLoc.X + searchRect.X;
        var frameY = maxLoc.Y + searchRect.Y;

        return new MatchResult(
            Found: true,
            Score: maxVal,
            Location: new DH2Point(frameX, frameY),
            Size: new DH2Size(templateSize.Width, templateSize.Height));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        // 持有 ITemplateStore 引用;模板 Mat 由 TemplateStore 持有并 Dispose
        GC.SuppressFinalize(this);
    }

    private static MatchResult NotFound()
    {
        return new MatchResult(
            Found: false,
            Score: 0,
            Location: new DH2Point(0, 0),
            Size: new DH2Size(0, 0));
    }

    /// <summary>本地别名,避免引入全局 using 别名冲突。</summary>
    private readonly record struct OpenCvSize(int Width, int Height);
}
