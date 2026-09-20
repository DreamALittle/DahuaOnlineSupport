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
/// Bitmap(PixelFormat.Format32bppArgb) → 包成 <see cref="Frame"/>(Mat 形态)。
/// 已知限制(写入 README):窗口须在屏幕内、可见、未被他窗遮挡;最小化或窗口化被遮时截屏内容不正确。
/// 窗口不可见 / 已销毁时返回包含空 <c>Mat</c> 的占位 <see cref="Frame"/>(S1 偏离裁决 #3)。
/// </summary>
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

        Bitmap? bitmap = null;
        Graphics? graphics = null;
        try
        {
            bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(
                origin.X,
                origin.Y,
                0,
                0,
                new System.Drawing.Size(rect.Width, rect.Height));

            // Bitmap → Mat(由调用方负责 Mat 释放)
            var mat = BitmapConverter.ToMat(bitmap);
            return new Frame(hwnd, DateTime.UtcNow, mat);
        }
        catch
        {
            // 任何 GDI 异常(句柄失效 / 设备丢失)均退化为空帧
            return EmptyFrame(hwnd);
        }
        finally
        {
            graphics?.Dispose();
            bitmap?.Dispose();
        }
    }

    public void Dispose()
    {
        // GdiCapture 无持有句柄;句柄全部为每帧局部变量,GC 自然释放
    }

    /// <summary>空帧占位(窗口不可见 / 已销毁)。Mat 为空矩阵。</summary>
    private static Frame EmptyFrame(long hwnd)
    {
        return new Frame(hwnd, DateTime.UtcNow, new Mat());
    }
}
