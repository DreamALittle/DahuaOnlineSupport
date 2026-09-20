using System.Globalization;
using System.IO;
using System.Text;

namespace DH2.MockGame.Services;

/// <summary>
/// 共享消息日志文件(raw + ui 双路同写一处,技术设计 §7)。
/// 行格式:{unixms}|{kind}|{msgOrEvent}|x={x}|y={y}。
/// 启动时清理旧文件;后续 append,跨线程用 lock 串行化。
/// </summary>
public sealed class MessagesLog : IDisposable
{
    private readonly string _path;
    private readonly object _gate = new();

    public MessagesLog(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = System.IO.Path.Combine(directory, "messages.log");
    }

    public string Path => _path;

    /// <summary>启动时清理旧日志(技术设计 §7)。</summary>
    public void ClearOnStartup()
    {
        lock (_gate)
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }

    /// <summary>raw 消息(WM_*):{unixms}|raw|0x{msg:X}|x={x}|y={y}</summary>
    public void WriteRaw(int msg, ushort x, ushort y)
    {
        var line = string.Format(
            CultureInfo.InvariantCulture,
            "{0}|raw|0x{1:X4}|x={3}|y={4}",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            msg,
            0,
            x,
            y);
        AppendLine(line);
    }

    /// <summary>ui 事件(PointerPressed/Released):{unixms}|ui|{event}|x={x}|y={y}</summary>
    public void WriteUi(string evt, double x, double y)
    {
        var line = string.Format(
            CultureInfo.InvariantCulture,
            "{0}|ui|{1}|x={2:F0}|y={3:F0}",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            evt,
            x,
            y);
        AppendLine(line);
    }

    private void AppendLine(string line)
    {
        lock (_gate)
        {
            File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
        }
    }

    public void Dispose()
    {
        // 文件句柄由 File.AppendAllText 内部托管,无需额外释放;lock 仍可被合法使用。
    }
}