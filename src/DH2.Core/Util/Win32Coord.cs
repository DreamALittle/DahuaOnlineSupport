using DH2.Core.Models;

namespace DH2.Core.Util;

/// <summary>
/// Win32 客户区 ↔ 屏幕 ↔ PostMessage <c>lParam</c> 坐标编码工具(纯函数,无副作用)。
/// </summary>
/// <remarks>
/// 全部方法线程安全;无字段状态。所有计算均在客户区坐标系或纯整数运算内完成,
/// 与 DPI 无关(@100% 缩放下 1 DIP = 1 px;非 100% 缩放的环境坐标误差不作为缺陷,见技术设计 §10.2)。
/// </remarks>
public static class Win32Coord
{
    /// <summary>
    /// 将客户区坐标编码为 <c>PostMessage</c> 的 <c>lParam</c>(低 16 位 X,高 16 位 Y)。
    /// </summary>
    /// <param name="clientPoint">客户区坐标。</param>
    /// <returns>
    /// 编码后的 32 位整数;X / Y 任一为负时抛 <see cref="ArgumentOutOfRangeException"/>。
    /// </returns>
    public static int ToLParam(Point clientPoint)
    {
        if (clientPoint.X < 0 || clientPoint.Y < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(clientPoint),
                $"coordinates must be non-negative; got ({clientPoint.X}, {clientPoint.Y})");
        }

        return unchecked((clientPoint.Y << 16) | (clientPoint.X & 0xFFFF));
    }

    /// <summary>
    /// 从 <c>lParam</c> 还原客户区坐标(X = 低 16 位,Y = 高 16 位,均按无符号解析)。
    /// </summary>
    /// <param name="lParam">从 <c>PostMessage</c> 收到的 32 位整数(可为负;按位模式解析)。</param>
    /// <returns>还原后的客户区坐标,合法范围 [0, 65535]。</returns>
    public static Point FromLParam(int lParam)
    {
        var x = lParam & 0xFFFF;
        var y = (lParam >> 16) & 0xFFFF;
        return new Point(x, y);
    }

    /// <summary>
    /// 客户区坐标 → 屏幕坐标(纯函数)。测试环境用,不依赖 <c>ClientToScreen</c> Win32 API。
    /// </summary>
    /// <param name="windowClientScreenOrigin">窗口客户区左上角的屏幕坐标(即 <see cref="DH2.Core.Models.Win32Window.Bounds"/>.X / .Y)。</param>
    /// <param name="clientPoint">客户区坐标。</param>
    /// <returns>屏幕坐标 = 原点 + 客户点。</returns>
    public static Point ClientToScreen(Rect windowClientScreenOrigin, Point clientPoint)
    {
        return new Point(
            windowClientScreenOrigin.X + clientPoint.X,
            windowClientScreenOrigin.Y + clientPoint.Y);
    }
}