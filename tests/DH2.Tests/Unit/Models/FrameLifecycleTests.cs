// DH2.Tests — RJ-S2-02 回归 UT(Mat 生命周期)
//
// 目的:锁死 Frame 在 Image 已 Dispose 之后仍可安全读 Width/Height 的契约,
// 同时验证空 Mat(EmptyFrame 占位)可重复使用,防止再次发生 0xC0000005 AV。
// 架构师明令:本测试类为本 RJ 例外产物。

using DH2.Core.Models;
using OpenCvSharp;
using Xunit;

namespace DH2.Tests.Unit.Models;

public class FrameLifecycleTests
{
    /// <summary>真实 Mat 工厂:用于构造非空 Frame。</summary>
    private static Mat MakeRealMat(int width, int height)
    {
        var mat = new Mat(height, width, MatType.CV_8UC3, Scalar.All(128));
        return mat;
    }

    [Fact]
    public void Frame_WidthAndHeight_AreReadSafe_AfterImageDispose()
    {
        // Arrange:构造 Frame(非空 Mat)
        var mat = MakeRealMat(640, 480);
        var frame = new Frame(12345L, DateTime.UtcNow, mat);

        // Act:Dispose 后读取 Width / Height(原 bug 触发 0xC0000005)
        var widthBeforeDispose = frame.Width;
        var heightBeforeDispose = frame.Height;
        mat.Dispose();
        var widthAfterDispose = frame.Width;
        var heightAfterDispose = frame.Height;

        // Assert:Dispose 前后值一致,且均等于 Mat 真实尺寸
        Assert.Equal(640, widthBeforeDispose);
        Assert.Equal(480, heightBeforeDispose);
        Assert.Equal(widthBeforeDispose, widthAfterDispose);
        Assert.Equal(heightBeforeDispose, heightAfterDispose);
    }

    [Fact]
    public void Frame_EmptyMat_CachedDimensions_AreZero()
    {
        // Arrange:空 Mat(模拟 GdiCapture.EmptyFrame 行为)
        var emptyMat = new Mat();
        var frame = new Frame(0L, DateTime.UtcNow, emptyMat);

        // Act & Assert:Width/Height 在构造期缓存为 0
        Assert.Equal(0, frame.Width);
        Assert.Equal(0, frame.Height);

        // Dispose 后仍可读(回归 #1 防)
        emptyMat.Dispose();
        Assert.Equal(0, frame.Width);
        Assert.Equal(0, frame.Height);
    }

    [Fact]
    public void Frame_EmptyMatPlaceholder_IsReusable()
    {
        // Arrange:连续构造多个含空 Mat 的 Frame,模拟 GdiCapture.EmptyFrame 按需 new 的契约。
        // 架构师 RJ-S2-02 要求"空帧占位可重复使用",验证不会因共享/泄漏导致后续 AV。
        for (var i = 0; i < 5; i++)
        {
            var mat = new Mat();
            var frame = new Frame(i, DateTime.UtcNow, mat);

            Assert.Equal(0, frame.Width);
            Assert.Equal(0, frame.Height);
            Assert.True(mat.Empty(), $"frame {i} must report Mat.Empty()=true");

            // 释放空 Mat 不抛异常
            mat.Dispose();

            // 释放后再读 Frame.Width / Height 仍安全
            Assert.Equal(0, frame.Width);
            Assert.Equal(0, frame.Height);
        }
    }

    [Fact]
    public void Frame_RealMat_ReleaseThenNewFrame_DoesNotCorrupt()
    {
        // Arrange:第一个 Frame
        var firstMat = MakeRealMat(800, 600);
        var firstFrame = new Frame(1L, DateTime.UtcNow, firstMat);
        firstMat.Dispose();

        // Act:第二个 Frame 使用新的 Mat(模拟 capture --count 2 的第二次迭代)
        var secondMat = MakeRealMat(1024, 768);
        var secondFrame = new Frame(2L, DateTime.UtcNow, secondMat);

        // Assert:两个 Frame 互相独立;第一个的缓存值不受 Mat 释放影响;第二个读自己的真实尺寸
        Assert.Equal(800, firstFrame.Width);
        Assert.Equal(600, firstFrame.Height);
        Assert.Equal(1024, secondFrame.Width);
        Assert.Equal(768, secondFrame.Height);

        secondMat.Dispose();

        // Dispose 后再读 — 缓存值仍正确(回归 #1 防)
        Assert.Equal(800, firstFrame.Width);
        Assert.Equal(600, firstFrame.Height);
        Assert.Equal(1024, secondFrame.Width);
        Assert.Equal(768, secondFrame.Height);
    }

    [Fact]
    public void Mat_EmptyMethod_ReturnsTrue_OnAliveEmptyMat()
    {
        // Arrange:OpenCvSharp 的 Mat.Empty() 是 Func<bool> lambda 属性(需 () 调用);
        // 验证 alive 空 Mat 报告 Empty()==true(CaptureCommand 据此跳过 ImWrite)。
        var mat = new Mat();
        Assert.True(mat.Empty());

        // Dispose 后调用空 Mat 的 Empty() 会抛 ObjectDisposedException(Mat 继承 DisposableObject.ThrowIfDisposed);
        // 本测试仅覆盖 alive 路径 — CaptureCommand 在 Dispose 前已决策。
    }
}
