// DH2.Tests — L1 单元测试
// IT-07 PostMessageDriver 异常路径防御(参数校验层,不需真窗口):
// - 负 hwnd / 零 hwnd → ActionResult(false, ...)
// - 负 x / 负 y → ActionResult(false, ...)
// - 合法参数 → 调 PostMessageW(0x201/0x202);真实路径需 MockGame 窗口(headless 跳过;
//   这部分由 IT-04 真机走查手册覆盖)

using DH2.Core.Models;
using DH2.Input;
using Xunit;

namespace DH2.Tests.Unit.Input;

public class PostMessageDriverTests
{
    [Fact]
    public void Click_ZeroHwnd_ReturnsFailureWithPositiveHwndMessage()
    {
        var driver = new PostMessageDriver(postClickDelayMs: 10);
        var result = driver.Click(0, 100, 200);

        Assert.False(result.Success);
        Assert.Contains("hwnd", result.FailReason);
        Assert.Contains("positive", result.FailReason);
    }

    [Fact]
    public void Click_NegativeHwnd_ReturnsFailureWithPositiveHwndMessage()
    {
        var driver = new PostMessageDriver(postClickDelayMs: 10);
        var result = driver.Click(-1, 100, 200);

        Assert.False(result.Success);
        Assert.Contains("hwnd", result.FailReason);
    }

    [Fact]
    public void Click_NegativeX_ReturnsFailureWithNonNegativeMessage()
    {
        var driver = new PostMessageDriver(postClickDelayMs: 10);
        var result = driver.Click(12345, -1, 200);

        Assert.False(result.Success);
        Assert.Contains("coordinates", result.FailReason);
        Assert.Contains("non-negative", result.FailReason);
    }

    [Fact]
    public void Click_NegativeY_ReturnsFailureWithNonNegativeMessage()
    {
        var driver = new PostMessageDriver(postClickDelayMs: 10);
        var result = driver.Click(12345, 100, -1);

        Assert.False(result.Success);
        Assert.Contains("coordinates", result.FailReason);
    }

    [Fact]
    public async Task ClickAsync_ZeroHwnd_ReturnsFailureAwaitable()
    {
        // 异步入口同样防御
        var driver = new PostMessageDriver(postClickDelayMs: 10);
        var result = await driver.ClickAsync(0, 100, 200);

        Assert.False(result.Success);
        Assert.Equal(1, result.Attempts);
    }

    [Fact]
    public async Task ClickAsync_PointOverload_NegativeX_Failure()
    {
        var driver = new PostMessageDriver(postClickDelayMs: 10);
        var result = await driver.ClickAsync(12345, new Point(-1, 200));

        Assert.False(result.Success);
        Assert.Contains("coordinates", result.FailReason);
    }

    [Fact]
    public void Constructor_NegativePostClickDelayMs_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PostMessageDriver(postClickDelayMs: -1));
    }

    [Fact]
    public void Constructor_ZeroPostClickDelayMs_Allowed()
    {
        // 0 ms 合法(立即 UP,无延迟);某些场景(单元测试 / 性能测试)会传 0
        var driver = new PostMessageDriver(postClickDelayMs: 0);
        Assert.NotNull(driver);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var driver = new PostMessageDriver();
        driver.Dispose();
        driver.Dispose(); // 第二次不抛
    }

    [Fact]
    public void Click_DriverReturnsActionResultWithAttempts1()
    {
        // 即使返 false,attempts 仍为 1(无重试)
        var driver = new PostMessageDriver(postClickDelayMs: 10);
        var result = driver.Click(0, 100, 200);

        Assert.Equal(1, result.Attempts);
    }
}
