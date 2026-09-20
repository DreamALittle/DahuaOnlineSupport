using OpenCvSharp;

namespace DH2.Core.Models;

/// <summary>
/// 一帧截屏。<see cref="Image"/> 由调用方负责释放(OpenCvSharp <c>Mat</c> 实现了 <c>IDisposable</c>)。
/// </summary>
/// <remarks>
/// 实现偏离:技术设计 §2.1 描述字段类型为 <c>Bitmap</c>。Core 不引用
/// <c>System.Drawing.Common</c>(只承载 OpenCvSharp4 类型层依赖),故此处使用 <c>Mat</c>;
/// Capture/Vision 层在边界用 <c>OpenCvSharp.Extensions.BitmapConverter</c> 完成 Bitmap↔Mat 转换。
/// </remarks>
public sealed record Frame(long Hwnd, DateTime Timestamp, Mat Image)
{
    /// <summary>帧宽度(像素)。</summary>
    public int Width => Image.Width;

    /// <summary>帧高度(像素)。</summary>
    public int Height => Image.Height;
}
