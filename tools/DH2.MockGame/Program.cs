// DH2.MockGame — Avalonia 模拟游戏窗体入口
//
// M0-S1 范围:仅交付骨架(空 800x600 窗口)。
// 完整实现(按 mock-layout.yaml 渲染 + 状态机 + 日志双路)由 Dev B 在 S1-3 实施。
//
// 当前 Program 仅初始化 Avalonia 与空窗口,DPI 感知由 Avalonia 默认提供(PerMonitorV2)。

using Avalonia;

namespace DH2.MockGame;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();
}