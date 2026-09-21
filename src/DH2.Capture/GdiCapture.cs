using System.Drawing;
using System.Drawing.Imaging;
using DH2.Core.Contracts;
using DH2.Core.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace DH2.Capture;

/// <summary>
/// 基于 GDI 的截屏实现(技术设计 §4.1)。
/// 流程:GetClientRect + ClientToScreen 得屏幕矩形 → Graphics.CopyFromScreen →
/// Bitmap(PixelFormat.Format32bppArgb) → BitmapConverter.ToMat(Bitmap) → <see cref="Mat.Clone()"/> 得独立副本
/// → 包成 <see cref="Frame"/>。
/// 已知限制(写入代码注释与 README):窗口须在屏幕内、可见、未被他窗遮挡;最小化或窗口化被遮时截屏内容不正确。
/// 窗口不可见 / 已销毁时返回包含空 <c>Mat</c> 的占位 <see cref="Frame"/>(S1 偏离裁决 #3)。
/// </summary>
/// <remarks>
/// <para><b>Frame.Image 所有权契约(RJ-S2-03):</b></para>
/// <list type="bullet">
///   <item>本方法返回的 <see cref="Frame.Image"/>(<see cref="Mat"/>)由<b>调用方负责释放</b>;</item>
///   <item>本方法不持有也不释放返回的 Mat;中间的 Bitmap 在方法返回前已 Dispose。</item>
///   <item><see cref="BitmapConverter.ToMat(Bitmap)"/> 在 OpenCvSharp4.Extensions 4.10.x 是<b>共享像素内存</b>(Mat 持有
///         Bitmap 的 <c>Scan0</c> 指针,不复制)。若不 <see cref="Mat.Clone()"/> 就 Dispose Bitmap,
///         后续访问 Mat 会命中已释放内存(0xC0000005)。Clone() 必须发生在 Bitmap Dispose 之前。</item>
/// </list>
/// </remarks>
public sealed class GdiCapture : IFrameCapture
{
    public Frame Capture(long hwnd)
    {
        var hWndPtr = new IntPtr(hwnd);

        if (!NativeMethods.GetClientRect(hWndPtr, out var rect))
        {
            return EmptyFrame(hwnd);
        }

        var origin = new NativeMethods.POINT { X = 0, Y = 0 };
        if (!NativeMethods.ClientToScreen(hWndPtr, ref origin))
        {
            return EmptyFrame(hwnd);
        }

        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return EmptyFrame(hwnd);
        }

        // BitmapConverter.ToMat(Bitmap) 是共享内存(指针别名,不复制像素);
        // 必须在 Bitmap Dispose 前 Clone() 取得独立副本,否则 Mat 悬垂。
        // 任何中间步骤异常都退化为空帧(满足 S1 偏离裁决 #3)。
        try
        {
            using var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(
                origin.X,
                origin.Y,
                0,
                0,
                new System.Drawing.Size(rect.Width, rect.Height));

            using var sharedMat = BitmapConverter.ToMat(bitmap);
            var independent = sharedMat.Clone();
            return new Frame(hwnd, DateTime.UtcNow, independent);
        }
        catch
        {
            // 任何 GDI / OpenCv 异常(句柄失效 / 设备丢失 / ToMat 失败)退化为空帧
            return EmptyFrame(hwnd);
        }
    }

    public void Dispose()
    {
        // GdiCapture 无持有句柄;句柄全部为每帧局部变量,GC 自然释放
    }

    /// <summary>空帧占位(窗口不可见 / 已销毁 / 中途异常)。Mat 为空矩阵。</summary>
    private static Frame EmptyFrame(long hwnd)
    {
        return new Frame(hwnd, DateTime.UtcNow, new Mat());
    }
}
