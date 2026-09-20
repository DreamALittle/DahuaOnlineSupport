using System.Runtime.InteropServices;

namespace DH2.Capture;

/// <summary>
/// DH2.Capture 局部 Win32 P/Invoke(技术设计 §3.1 白名单)。
/// 与 DH2.Input 的 NativeMethods 各自维护(Capture 不应依赖 Input,02 §3 单向)。
/// 仅声明截屏层需要的窗口坐标 API。
/// </summary>
internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
}
