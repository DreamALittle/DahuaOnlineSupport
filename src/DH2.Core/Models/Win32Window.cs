namespace DH2.Core.Models;

/// <summary>
/// 可见顶层窗口的元数据快照。
/// </summary>
/// <param name="Hwnd">Win32 窗口句柄(以 <see cref="long"/> 存储以利序列化与跨平台稳定)。</param>
/// <param name="Title">窗口标题(GetWindowTextW 结果)。</param>
/// <param name="ProcessName">拥有该窗口的进程名(由 PID 解析得到;不依赖 <c>OpenProcess</c>)。</param>
/// <param name="Bounds">客户区在屏幕坐标系下的矩形(由 GetClientRect + ClientToScreen 拼出)。</param>
public sealed record Win32Window(long Hwnd, string Title, string ProcessName, Rect Bounds);