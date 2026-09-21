// DH2.Tests — L1 单元测试
// UT-08 Polling.WaitUntilAsync 三个语义:立即真 / 超时假 / 中途取消
// 参见 docs/iterations/M0/ITER-M0-测试设计.md §1 + DH2.Core.Util.Polling.cs

using DH2.Core.Util;
using Xunit;

namespace DH2.Tests.Unit.Util;

public class PollingTests
{
    [Fact]
    public async Task WaitUntilAsync_ImmediatelyTrue_ReturnsTrueQuickly()
    {
        // 谓词首次即返回 true — 整个等待应快速返回 true,远小于 timeoutMs
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await Polling.WaitUntilAsync(
            predicate: () => Task.FromResult(true),
            intervalMs: 50,
            timeoutMs: 5000,
            ct: CancellationToken.None);
        sw.Stop();

        Assert.True(result);
        Assert.True(sw.ElapsedMilliseconds < 1000, $"应快速返回,实测 {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task WaitUntilAsync_AlwaysFalse_TimesOutAndReturnsFalse()
    {
        // 谓词始终 false — 等待应在 timeoutMs 附近返回 false(不抛)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await Polling.WaitUntilAsync(
            predicate: () => Task.FromResult(false),
            intervalMs: 20,
            timeoutMs: 100,
            ct: CancellationToken.None);
        sw.Stop();

        Assert.False(result);
        // 允许一定抖动,但不应严重超时
        Assert.True(sw.ElapsedMilliseconds < 1000, $"超时返回过快/慢,实测 {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task WaitUntilAsync_CancelledMidway_ThrowsOperationCanceledException()
    {
        // 中途取消 — 必须抛 OCE,且不吞未观察异常
        using var cts = new CancellationTokenSource();

        // 安排:第一次谓词后取消,Task.Delay 期间触发
        var callCount = 0;
        var task = Polling.WaitUntilAsync(
            predicate: () =>
            {
                Interlocked.Increment(ref callCount);
                // 第一次调用时安排取消
                if (callCount == 1)
                {
                    cts.CancelAfter(10);
                }
                return Task.FromResult(false);
            },
            intervalMs: 20,
            timeoutMs: 5000,
            ct: cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
    }

    [Fact]
    public async Task WaitUntilAsync_PredicateThrows_PropagatesException()
    {
        // 谓词自身抛异常 — 必须沿 Task 树传播,不吞
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await Polling.WaitUntilAsync(
                predicate: () => throw new InvalidOperationException("predicate boom"),
                intervalMs: 10,
                timeoutMs: 500,
                ct: CancellationToken.None));
        Assert.Equal("predicate boom", ex.Message);
    }

    [Fact]
    public async Task WaitUntilAsync_NullPredicate_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await Polling.WaitUntilAsync(null!, 10, 100, CancellationToken.None));
    }

    [Fact]
    public async Task WaitUntilAsync_NegativeInterval_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await Polling.WaitUntilAsync(() => Task.FromResult(true), -1, 100, CancellationToken.None));
    }

    [Fact]
    public async Task WaitUntilAsync_NegativeTimeout_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await Polling.WaitUntilAsync(() => Task.FromResult(true), 10, -1, CancellationToken.None));
    }

    [Fact]
    public async Task WaitUntilAsync_BecomesTrueAfterSomeIterations_ReturnsTrue()
    {
        // 第 3 次迭代后变 true — 必须返回 true 且耗时约 2 个 interval
        var calls = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await Polling.WaitUntilAsync(
            predicate: () =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(calls >= 3);
            },
            intervalMs: 30,
            timeoutMs: 5000,
            ct: CancellationToken.None);
        sw.Stop();

        Assert.True(result);
        Assert.True(calls >= 3);
        // 至少 2 个间隔(60ms),远小于 1s
        Assert.True(sw.ElapsedMilliseconds >= 30, $"应至少等 1 个 interval,实测 {sw.ElapsedMilliseconds}ms");
    }
}
