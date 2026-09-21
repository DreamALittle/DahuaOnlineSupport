using DH2.Core.Contracts;
using DH2.Core.Models;
using DH2.Core.Util;

namespace DH2.Input;

/// <summary>
/// 后台 PostMessage 鼠标点击驱动(技术设计 §3.3 + S4-1 Dev B 真实现)。
/// 点击序列:WM_MOUSEMOVE → WM_LBUTTONDOWN(MK_LBUTTON=0x0001) →
///   <c>await Task.Delay(PostClickDelayMs)</c> → WM_LBUTTONUP(0)。
/// 客户区坐标经 <see cref="Win32Coord.ToLParam"/> 编码。
/// 返回 <see cref="ActionResult"/>:PostMessage 返回 false 即 <c>Success=false</c>,
///   <c>FailReason="PostMessage returned false (xxx)"</c>。M0 不做重试(行为层职责)。
/// DPI:100% 缩放下客户区坐标 = 像素 = DIP;PostMessage 天然免疫 DPI(由目标窗口线程消费 lParam)。
/// </summary>
/// <remarks>
/// <para>S4 QA 集成接缝(<c>qa/s4-merge</c>):本类接 <see cref="IInputDriver"/> 接口
/// —— Dev B 的真实现原签名 <c>ClickAsync(...)</c> 异步;S4-2 ClickCommand(<c>Dev A</c>)
/// 通过 <see cref="IInputDriver"/> 同步 <c>Click(...)</c> 契约消费。
/// QA 集成补一个同步 <c>Click</c> 包装 <c>ClickAsync(...).GetAwaiter().GetResult()</c>(阻塞等待),
/// 不引入额外线程,保持 M0 单线程行为。</para>
/// </remarks>
public sealed class PostMessageDriver : IInputDriver
{
    private readonly int _postClickDelayMs;

    public PostMessageDriver(int postClickDelayMs = 50)
    {
        if (postClickDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(postClickDelayMs), postClickDelayMs, "must be >= 0");
        }

        _postClickDelayMs = postClickDelayMs;
    }

    /// <summary>
    /// 同步语义实现 <see cref="IInputDriver.Click"/> ——阻塞等待异步版本。
    /// 失败保护:捕获 <see cref="OperationCanceledException"/> 后返回 <c>Success=false</c>(而非抛),
    /// 符合 IInputDriver 接口契约(不抛异常)。
    /// </summary>
    public ActionResult Click(long hwnd, int clientX, int clientY)
    {
        try
        {
            return ClickAsync(hwnd, clientX, clientY, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            return new ActionResult(false, 0, "cancelled");
        }
    }

    public void Dispose()
    {
        // 无持有资源;按需添加(NativeMethods 资源由系统管理)。
    }

    /// <summary>异步语义主实现:返回 <see cref="Task{TResult}"/>,由调用方按上下文选择 await / 阻塞 / GetResult。</summary>
    public Task<ActionResult> ClickAsync(long hwnd, int clientX, int clientY, CancellationToken ct = default)
    {
        // 入口防御:失败立即返回,不作冲门
        if (hwnd <= 0)
        {
            return Task.FromResult(new ActionResult(false, 1, "hwnd must be positive"));
        }

        if (clientX < 0 || clientY < 0)
        {
            return Task.FromResult(new ActionResult(false, 1, "client coordinates must be non-negative"));
        }

        return ClickInternalAsync(new IntPtr(hwnd), clientX, clientY, ct);
    }

    public Task<ActionResult> ClickAsync(long hwnd, Point clientPoint, CancellationToken ct = default)
        => ClickAsync(hwnd, clientPoint.X, clientPoint.Y, ct);

    private async Task<ActionResult> ClickInternalAsync(IntPtr hWndPtr, int clientX, int clientY, CancellationToken ct)
    {
        var lParam = new IntPtr(Win32Coord.ToLParam(new Point(clientX, clientY)));

        // 1) MOVE(可选;§3.3 设计为可选,M0 保留以模拟真实序列)
        if (!NativeMethods.PostMessageW(hWndPtr, NativeMethods.WM_MOUSEMOVE, IntPtr.Zero, lParam))
        {
            return new ActionResult(false, 1, "PostMessage returned false (MOUSEMOVE)");
        }

        // 2) DOWN
        if (!NativeMethods.PostMessageW(
                hWndPtr,
                NativeMethods.WM_LBUTTONDOWN,
                new IntPtr(NativeMethods.MK_LBUTTON),
                lParam))
        {
            return new ActionResult(false, 1, "PostMessage returned false (LBUTTONDOWN)");
        }

        // 3) 延迟(可取消)
        await Task.Delay(_postClickDelayMs, ct).ConfigureAwait(false);

        // 4) UP
        if (!NativeMethods.PostMessageW(hWndPtr, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, lParam))
        {
            return new ActionResult(false, 1, "PostMessage returned false (LBUTTONUP)");
        }

        return new ActionResult(true, 1, string.Empty);
    }
}
