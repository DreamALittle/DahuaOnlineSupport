namespace DH2.Core.Util;

/// <summary>
/// 通用轮询等待工具(技术设计 §2.4;编码规范 §3 — 禁止散落手写 <c>Thread.Sleep</c> 循环)。
/// </summary>
public static class Polling
{
    /// <summary>
    /// 异步轮询 <paramref name="predicate"/> 直到返回 <c>true</c> / 超时 / 被取消。
    /// </summary>
    /// <param name="predicate">返回 <see cref="Task{TResult}"/> 的判定函数;不可为 <c>null</c>。不接受 <c>CancellationToken</c>(按调用方约定是否内嵌传递)。</param>
    /// <param name="intervalMs">两次判定之间的间隔(毫秒);&lt; 0 抛 <see cref="ArgumentOutOfRangeException"/>。</param>
    /// <param name="timeoutMs">总超时(毫秒);&lt; 0 抛 <see cref="ArgumentOutOfRangeException"/>。</param>
    /// <param name="ct">外部取消令牌;触发时抛 <see cref="OperationCanceledException"/>。</param>
    /// <returns>
    /// <c>true</c> = 谓词在超时前至少一次返回 <c>true</c>;<c>false</c> = 达到超时仍未满足。
    /// 用户取消与超时通过异常类型区分:用户取消抛 <see cref="OperationCanceledException"/>,
    /// 超时仅返回 <c>false</c>。
    /// </returns>
    /// <remarks>
    /// 实现说明:
    /// <list type="bullet">
    ///   <item>谓词本身不接受 <c>CancellationToken</c>——若需取消长耗谓词,调用方应在谓词内自行检查令牌。</item>
    ///   <item>轮询间隔 <paramref name="intervalMs"/> = 0 退化为"忙等",通常仅用于测试。</item>
    ///   <item>本方法不抛出"未观察异常"(所有异常均沿 Task 树传播)。</item>
    /// </list>
    /// </remarks>
    public static async Task<bool> WaitUntilAsync(
        Func<Task<bool>> predicate,
        int intervalMs,
        int timeoutMs,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (intervalMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalMs), intervalMs, "must be >= 0");
        }
        if (timeoutMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMs), timeoutMs, "must be >= 0");
        }

        // 合并外部取消与超时取消
        using var timeoutCts = new CancellationTokenSource(timeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        while (true)
        {
            // 每次循环开头先检查外部取消
            ct.ThrowIfCancellationRequested();

            // 谓词执行(谓词自身不接 ct,异常直接传播)
            var satisfied = await predicate().ConfigureAwait(false);
            if (satisfied)
            {
                return true;
            }

            try
            {
                await Task.Delay(intervalMs, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // 外部取消 — 抛出
                throw;
            }
            catch (OperationCanceledException)
            {
                // 仅超时 — 不抛,返回 false
                return false;
            }
        }
    }
}
