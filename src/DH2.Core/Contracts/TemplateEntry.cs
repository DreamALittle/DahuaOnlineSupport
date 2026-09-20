using Mat = OpenCvSharp.Mat;
using Point = DH2.Core.Models.Point;

namespace DH2.Core.Contracts;

/// <summary>
/// 模板条目(来自 manifest + PNG 灰度快照)。
/// </summary>
/// <param name="Key">逻辑键。</param>
/// <param name="File">相对模板根目录的图片路径(如 <c>png/mock_taskbar.png</c>)。</param>
/// <param name="Threshold">匹配阈值(0.5, 1.0);缺省取 <c>DevConfig.Matching.DefaultThreshold</c>。</param>
/// <param name="ClickOffset">匹配中心到点击点的偏移(DIP,模板更新或游戏改版时调整)。</param>
/// <param name="Roi">所属 ROI 逻辑分区名(M0 仅记录,不参与裁剪)。</param>
/// <param name="Since">引入版本(游戏版本标记)。</param>
/// <param name="Image">模板灰度图(由 <c>Cv2.ImRead</c> 加载,<see cref="IDisposable"/> 由 Store 持有负责释放)。</param>
public sealed record TemplateEntry(
    string Key,
    string File,
    double Threshold,
    Point ClickOffset,
    string Roi,
    string Since,
    Mat Image);
