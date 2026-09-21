using System.Runtime.InteropServices;

namespace DH2.Input;

/// <summary>
/// Win32 P/Invoke 静态声明(技术设计 §3.1 白名单)。
/// 严格白名单内的 API,白名单之外的 Win32 声明须审核(技术设计 §3.1)。
/// 与红线清单(01 §3)无关:不读进程内存、不挂钩全局、不注入 DLL、不拦截网络封包。
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>EnumWindows 回调委托(返回 true 继续枚举,false 中止)。</summary>
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    /// <summary>
    /// 枚举所有顶级窗口(包含子窗口关系中的顶级)。由回调决定过滤。
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    /// <summary>判定窗口是否可见。</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>判定窗口是否仍存在(即使句柄已不在主线程也可查)。</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);

    /// <summary>
    /// 取窗口标题。返回字符串长度(不含 null 终止符);buffer 不足返回 nSize。
    /// 标题为空视为不可枚举(避免枚举隐藏辅助窗口)。
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetWindowTextW")]
    public static extern int GetWindowTextW(IntPtr hWnd, [Out] char[] lpString, int nMaxCount);

    /// <summary>取窗口类名(用于辅助过滤)。</summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetClassNameW")]
    public static extern int GetClassNameW(IntPtr hWnd, [Out] char[] lpString, int nMaxCount);

    /// <summary>取创建该窗口的线程 + 进程 id(pdwProcessId 为输出)。</summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>取窗口客户区矩形(左上角客户区原点为 (0,0),Right/Bottom 为宽高)。</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>客户区坐标 → 屏幕坐标。</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    /// <summary>
    /// 后台消息投递(技术设计 §3.3 + §6)。把消息放入目标窗口线程队列,不需前台焦点。
    /// M0 用于 <see cref="PostMessageDriver"/> 实现鼠标点击序列。
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // ---------- 鼠标消息常量(技术设计 §3.3) ----------
    public const uint WM_MOUSEMOVE = 0x0200;
    public const uint WM_LBUTTONDOWN = 0x0201;
    public const uint WM_LBUTTONUP = 0x0202;
    public const int MK_LBUTTON = 0x0001;

    /// <summary>RECT 结构(Win32 原生)。</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        /// <summary>宽 = Right - Left,高 = Bottom - Top。</summary>
        public int Width => Right - Left;

        /// <summary>高。</summary>
        public int Height => Bottom - Top;
    }

    /// <summary>POINT 结构(Win32 原生)。</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }
}
