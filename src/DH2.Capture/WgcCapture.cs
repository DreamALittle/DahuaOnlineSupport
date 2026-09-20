using DH2.Core.Contracts;
using DH2.Core.Models;

namespace DH2.Capture;

/// <summary>
/// Windows.Graphics.Capture(WGC)截屏占位(技术设计 §4.2 / M1a 实现)。
/// M0 仅占位:构造抛 <see cref="NotImplementedException"/>,以保持 <see cref="IFrameCapture"/> 接口稳定。
/// </summary>
public sealed class WgcCapture : IFrameCapture
{
    public WgcCapture()
    {
        throw new NotImplementedException(
            "WgcCapture 将在 M1a 实现(技术设计 §4.2)。M0 由 GdiCapture 提供基础截屏能力。");
    }

    public Frame Capture(long hwnd)
    {
        // 构造已抛,此处不可达
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        // 无资源
    }
}
