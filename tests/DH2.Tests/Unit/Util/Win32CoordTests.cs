// DH2.Tests — L1 单元测试
// UT-01 Win32Coord.ToLParam/FromLParam 编码往返 + 负坐标防御
// UT-02 Win32Coord.ClientToScreen 客户区→屏幕纯函数
// 参见 docs/iterations/M0/ITER-M0-测试设计.md §1

using DH2.Core.Models;
using DH2.Core.Util;
using Xunit;

namespace DH2.Tests.Unit.Util;

public class Win32CoordTests
{
    // ───── UT-01 ToLParam / FromLParam 编码往返 ─────

    [Fact]
    public void ToLParam_Origin_EncodesAsZero()
    {
        var lp = Win32Coord.ToLParam(new Point(0, 0));
        Assert.Equal(0, lp);
    }

    [Fact]
    public void ToLParam_HundredFifty_EncodesAsLow16XHigh16Y()
    {
        // (100, 50) → (50 << 16) | 100 = 3276900
        var lp = Win32Coord.ToLParam(new Point(100, 50));
        Assert.Equal((50 << 16) | 100, lp);
        Assert.Equal(3_276_900, lp);
    }

    [Fact]
    public void ToLParam_MaxCoordinates_RoundTripPreservesPoint()
    {
        // (65535, 65535) → 0xFFFFFFFF = -1 as int(unchecked);FromLParam 必须按位解析回原值
        var p = new Point(65535, 65535);
        var lp = Win32Coord.ToLParam(p);
        Assert.Equal(-1, lp); // int 重解释
        var back = Win32Coord.FromLParam(lp);
        Assert.Equal(p, back);
    }

    [Fact]
    public void ToLParam_NegativeX_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Win32Coord.ToLParam(new Point(-1, 0)));
    }

    [Fact]
    public void ToLParam_NegativeY_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Win32Coord.ToLParam(new Point(0, -1)));
    }

    [Fact]
    public void ToLParam_BothNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Win32Coord.ToLParam(new Point(-5, -7)));
    }

    [Fact]
    public void FromLParam_LowSixteenBits_AreX()
    {
        // X = 0x1234, Y = 0 → 0x00001234
        var p = Win32Coord.FromLParam(0x1234);
        Assert.Equal(new Point(0x1234, 0), p);
    }

    [Fact]
    public void FromLParam_HighSixteenBits_AreY()
    {
        // X = 0, Y = 0xABCD → 0xABCD0000 = -1412628480 as int
        var p = Win32Coord.FromLParam(unchecked((int)0xABCD0000));
        Assert.Equal(new Point(0, 0xABCD), p);
    }

    [Fact]
    public void ToFromLParam_RoundTrip_PreservesArbitraryPoint()
    {
        // 综合往返:多组非边界点
        var points = new[]
        {
            new Point(0, 0),
            new Point(1, 1),
            new Point(255, 16),
            new Point(1024, 768),
            new Point(32768, 16384),
            new Point(65535, 65535),
        };
        foreach (var p in points)
        {
            var lp = Win32Coord.ToLParam(p);
            var back = Win32Coord.FromLParam(lp);
            Assert.Equal(p, back);
        }
    }

    // ───── UT-02 ClientToScreen 纯函数 ─────

    [Fact]
    public void ClientToScreen_OffsetAtOrigin_ReturnsClientPoint()
    {
        // 原点 (0,0),客户点 (50,80) → 屏幕 (50,80)
        var screen = Win32Coord.ClientToScreen(new Rect(0, 0, 800, 600), new Point(50, 80));
        Assert.Equal(new Point(50, 80), screen);
    }

    [Fact]
    public void ClientToScreen_OffsetOrigin_AddsCoordinates()
    {
        // 客户区左上角屏幕坐标 (100,200),客户点 (16,16) → 屏幕 (116,216)
        var screen = Win32Coord.ClientToScreen(new Rect(100, 200, 800, 600), new Point(16, 16));
        Assert.Equal(new Point(116, 216), screen);
    }

    [Fact]
    public void ClientToScreen_LargeOrigin_PreservesAll()
    {
        // 大坐标 + 客户点 (0,0) → 屏幕 = 原点
        var screen = Win32Coord.ClientToScreen(new Rect(1920, 1080, 800, 600), new Point(0, 0));
        Assert.Equal(new Point(1920, 1080), screen);
    }

    [Fact]
    public void ClientToScreen_FarOriginAndClient_AddsBoth()
    {
        // 远端原点 (5000, 5000),客户点 (123, 456) → (5123, 5456)
        var screen = Win32Coord.ClientToScreen(new Rect(5000, 5000, 800, 600), new Point(123, 456));
        Assert.Equal(new Point(5123, 5456), screen);
    }
}
