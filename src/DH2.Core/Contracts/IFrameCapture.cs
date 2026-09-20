using DH2.Core.Models;

namespace DH2.Core.Contracts;

/// <summary>
/// 单帧截屏抽象。
/// </summary>
/// <remarks>
/// M0 实现: <c>DH2.Capture.GdiCapture</c>(S2,Dev B),基于 <c>Graphics.CopyFromScreen</c>;
/// M1a 将由 <c>DH2.Capture.WgcCapture</c>(Windows.Graphics.Capture)替代,消除遮挡与最小化限制。
/// </remarks>
public interface IFrameCapture : IDisposable
{
    /// <summary>
    /// 截取指定窗口客户区当前帧。
    /// </summary>
    /// <param name="hwnd">目标窗口句柄(Win32 HWND)。</param>
    /// <returns>包含截屏 <c>Mat</c> 的 <see cref="Frame"/>;窗口不可见 / 已销毁时返回包含空 <c>Mat</c> 的占位 <see cref="Frame"/>。</returns>
    Frame Capture(long hwnd);
}
