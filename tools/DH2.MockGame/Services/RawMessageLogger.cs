using System.Runtime.InteropServices;

namespace DH2.MockGame.Services;

/// <summary>
/// SetWindowSubclass 原始消息日志(技术设计 §7)。
/// 子类化作用于调用方提供的单窗口 HWND(本进程内,非全局钩子),
/// 仅记录 WM_MOUSEMOVE / WM_LBUTTONDOWN / WM_LBUTTONUP,不解码为"点击"。
/// 钩子必须返回 DefSubclassProc 以保持默认消息流(技术设计 §10 陷阱提示:消息流被吞则按钮 Click 不触发)。
/// </summary>
public sealed class RawMessageLogger : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly MessagesLog _log;
    private readonly NativeMethods.SubclassProc _proc;
    private readonly IntPtr _subclassId = new(1);
    private bool _installed;
    private bool _disposed;

    public RawMessageLogger(IntPtr hwnd, MessagesLog log)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("HWND 不能为零", nameof(hwnd));
        }

        _hwnd = hwnd;
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _proc = SubclassCallback; // 防止 GC 回收委托
    }

    /// <summary>
    /// 安装子类化。返回 true 表示成功(comctl32 v6 manifest 已启用)。
    /// </summary>
    public bool Install()
    {
        if (_installed)
        {
            return true;
        }

        _installed = NativeMethods.SetWindowSubclass(_hwnd, _proc, _subclassId, IntPtr.Zero);
        return _installed;
    }

    private IntPtr SubclassCallback(
        IntPtr hWnd,
        uint uMsg,
        IntPtr wParam,
        IntPtr lParam,
        IntPtr uIdSubclass,
        IntPtr dwRefData)
    {
        // 仅记录目标消息,其余透明转发(子类化钩子只记录不处理)。
        if (uMsg is NativeMethods.WM_MOUSEMOVE
            or NativeMethods.WM_LBUTTONDOWN
            or NativeMethods.WM_LBUTTONUP)
        {
            var (x, y) = NativeMethods.DecodeLParam(lParam);
            _log.WriteRaw((int)uMsg, x, y);
        }

        return NativeMethods.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_installed)
        {
            NativeMethods.RemoveWindowSubclass(_hwnd, _proc, _subclassId);
            _installed = false;
        }

        GC.SuppressFinalize(this);
    }
}