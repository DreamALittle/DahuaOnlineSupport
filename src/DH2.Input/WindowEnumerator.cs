using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DH2.Core.Config;
using DH2.Core.Contracts;
using DH2.Core.Models;

namespace DH2.Input;

/// <summary>
/// Win32 窗口枚举器(技术设计 §3.2)。
/// 流程:EnumWindows → IsWindowVisible &amp;&amp; 标题非空 → GetWindowTextW →
///   GetWindowThreadProcessId → Process.GetProcessById(pid).ProcessName(框架内部 OpenProcess,代码层不直接声明) →
///   按 TitlePattern(正则)与 ProcessName 过滤 → GetClientRect + ClientToScreen 得客户区屏幕矩形。
/// 异常窗口(已销毁 / 权限不足)跳过不抛。
/// </summary>
public sealed class WindowEnumerator : IWindowLocator
{
    private const int TextBufferSize = 512;

    public IReadOnlyList<Win32Window> Enumerate(WindowTargetConfig target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (string.IsNullOrEmpty(target.TitlePattern) && string.IsNullOrEmpty(target.ProcessName))
        {
            // 与 ConfigValidator 重复校验一致;在此处兜底以避免直接构造非法 target
            throw new ArgumentException(
                "WindowTargetConfig 必须有 TitlePattern 或 ProcessName 至少一项非空",
                nameof(target));
        }

        Regex? titleRegex = null;
        if (!string.IsNullOrEmpty(target.TitlePattern))
        {
            titleRegex = new Regex(target.TitlePattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        var results = new List<Win32Window>();
        // 用 GCHandle 持有 results 引用,以便回调可写入;枚举结束释放
        var handle = GCHandle.Alloc(results);
        try
        {
            NativeMethods.EnumWindows(
                (hWnd, _) =>
                {
                    try
                    {
                        if (!NativeMethods.IsWindowVisible(hWnd))
                        {
                            return true; // 跳过不可见窗口
                        }

                        var title = ReadWindowText(hWnd);
                        if (string.IsNullOrEmpty(title))
                        {
                            return true; // 标题为空视为不可枚举(隐藏辅助窗口)
                        }

                        var processName = ResolveProcessName(hWnd);
                        if (processName is null)
                        {
                            return true; // 进程已退出 / 无权限,跳过
                        }

                        // 过滤
                        if (titleRegex is not null && !titleRegex.IsMatch(title))
                        {
                            return true;
                        }

                        if (!string.IsNullOrEmpty(target.ProcessName)
                            && !string.Equals(processName, target.ProcessName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        var bounds = ResolveClientBounds(hWnd);
                        if (bounds is null)
                        {
                            return true;
                        }

                        results.Add(new Win32Window((long)hWnd, title, processName, bounds));
                        return true;
                    }
                    catch
                    {
                        // 任何单窗口异常不污染枚举
                        return true;
                    }
                },
                IntPtr.Zero);
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }

        return results;
    }

    /// <summary>读窗口标题;空时返回 null。</summary>
    private static string? ReadWindowText(IntPtr hWnd)
    {
        var buffer = new char[TextBufferSize];
        var len = NativeMethods.GetWindowTextW(hWnd, buffer, buffer.Length);
        if (len <= 0)
        {
            return null;
        }

        return new string(buffer, 0, len);
    }

    /// <summary>
    /// 经 PID 取进程名(用 <see cref="Process.GetProcessById(int)"/>;框架内部封装 OpenProcess,
    /// 代码层不直接声明 Win32 OpenProcess,符合 01 §3 红线)。
    /// 进程已退出 / 无权限时返回 null(枚举跳过该窗口)。
    /// </summary>
    private static string? ResolveProcessName(IntPtr hWnd)
    {
        _ = NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
        if (pid == 0)
        {
            return null;
        }

        try
        {
            using var p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch (ArgumentException)
        {
            // 进程已退出
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // 拒绝访问 / 其他系统错误
            return null;
        }
    }

    /// <summary>客户区在屏幕坐标系下的矩形;窗口无效时返回 null。</summary>
    private static Rect? ResolveClientBounds(IntPtr hWnd)
    {
        if (!NativeMethods.GetClientRect(hWnd, out var rect))
        {
            return null;
        }

        // 用左上角客户点 (0,0) ClientToScreen 得客户区左上角的屏幕坐标
        var origin = new NativeMethods.POINT { X = 0, Y = 0 };
        if (!NativeMethods.ClientToScreen(hWnd, ref origin))
        {
            return null;
        }

        return new Rect(origin.X, origin.Y, rect.Width, rect.Height);
    }
}
