using DH2.Core.Config;
using DH2.MockGame.Services;

namespace DH2.MockGame;

/// <summary>
/// 应用级服务容器(MockGame 进程内单例)。
/// 持有 Core 提供的 <see cref="MockLayoutConfig"/>(RJ-S1-04:几何一律来自配置),
/// 以及 MockGame 本地服务 <see cref="StateStore"/> / <see cref="MessagesLog"/>。
/// </summary>
public sealed class AppHost
{
    private static AppHost? _current;

    public static AppHost Current => _current
        ?? throw new InvalidOperationException("AppHost 尚未装配(App.OnFrameworkInitializationCompleted 未运行)。");

    /// <summary>Core 提供的 MockGame 布局配置(从 configs/mock-layout.yaml 解析)。</summary>
    public MockLayoutConfig Layout { get; }

    public StateStore Store { get; }

    public MessagesLog Log { get; }

    public AppHost(MockLayoutConfig layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        // 临时目录展开(Layout 默认含 %TEMP%;这里再次展开以兼容调用方直接传 yaml 文本)
        var tempDir = Environment.ExpandEnvironmentVariables(layout.TempDir);
        Store = new StateStore(tempDir);
        Log = new MessagesLog(tempDir);

        // 启动清理旧文件(技术设计 §7)
        Store.ClearOnStartup();
        Log.ClearOnStartup();
    }

    internal static void Install(AppHost host)
    {
        _current = host;
    }
}
