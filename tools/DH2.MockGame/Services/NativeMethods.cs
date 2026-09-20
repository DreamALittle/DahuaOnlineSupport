using System.Runtime.InteropServices;

namespace DH2.MockGame.Services;

/// <summary>
/// Win32 P/Invoke 静态声明(技术设计 §3.1 白名单外 Win32 API 须审核)。
/// 本类仅声明 comctl32 子类化与必要常量,与红线清单无关:
/// - 不读进程内存、不挂钩全局、不注入 DLL、不拦截网络封包;
/// - SetWindowSubclass 仅作用于本进程、本窗口(见 §7 设计,非全局钩子)。
/// </summary>
internal static partial class NativeMethods
{
    // ---------- 消息常量 ----------
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;

    // ---------- comctl32 子类化 ----------
    public delegate IntPtr SubclassProc(
        IntPtr hWnd,
        uint uMsg,
        IntPtr wParam,
        IntPtr lParam,
        IntPtr uIdSubclass,
        IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true, EntryPoint = "#410")]
    public static extern bool SetWindowSubclass(
        IntPtr hWnd,
        SubclassProc pfnSubclass,
        IntPtr uIdSubclass,
        IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true, EntryPoint = "#412")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RemoveWindowSubclass(
        IntPtr hWnd,
        SubclassProc pfnSubclass,
        IntPtr uIdSubclass);

    [DllImport("comctl32.dll", SetLastError = true, EntryPoint = "#413")]
    public static extern IntPtr DefSubclassProc(
        IntPtr hWnd,
        uint uMsg,
        IntPtr wParam,
        IntPtr lParam);

    // ---------- 解码 lParam ----------
    // PostMessage lParam 编码:y 高 16 位 / x 低 16 位,均为有符号 16 位(0..65535 正向)。
    public static (ushort X, ushort Y) DecodeLParam(IntPtr lParam)
    {
        var raw = lParam.ToInt32();
        var x = (ushort)(raw & 0xFFFF);
        var y = (ushort)((raw >> 16) & 0xFFFF);
        return (x, y);
    }
}