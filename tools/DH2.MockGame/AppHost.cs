using DH2.MockGame.Models;
using DH2.MockGame.Services;

namespace DH2.MockGame;

/// <summary>
/// 应用级服务容器(MockGame 进程内单例)。
/// Avalonia 11 无强 DI 容器,采用静态单例注入 MockLayout / StateStore / MessagesLog,
/// 由 App.OnFrameworkInitializationCompleted 装配,MainWindow 通过 AppHost.Current 访问。
/// </summary>
public sealed class AppHost
{
    private static AppHost? _current;

    public static AppHost Current => _current
        ?? throw new InvalidOperationException("AppHost 尚未装配(App.OnFrameworkInitializationCompleted 未运行)。");

    public MockLayout Layout { get; }

    public StateStore Store { get; }

    public MessagesLog Log { get; }

    public AppHost(MockLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Store = new StateStore(layout.TempDir);
        Log = new MessagesLog(layout.TempDir);

        // 启动清理旧文件(技术设计 §7:启动时清理旧日志/状态)
        Store.ClearOnStartup();
        Log.ClearOnStartup();
    }

    internal static void Install(AppHost host)
    {
        _current = host;
    }
}