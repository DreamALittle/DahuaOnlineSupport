using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DH2.Core.Config;
using DH2.MockGame.ViewModels;
using DH2.MockGame.Views;

namespace DH2.MockGame;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // RJ-S1-04:启动时读取布局配置,几何一律来自 yaml,删除硬编码
            var layout = LoadLayoutOrFail();

            // 装配应用级服务
            AppHost.Install(new AppHost(layout));

            // 创建 ViewModel + Window
            var viewModel = new MainViewModel(layout, AppHost.Current.Store);
            var window = new MainWindow
            {
                DataContext = viewModel,
                Width = layout.Window.Width,
                Height = layout.Window.Height,
                CanResize = false,
                WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterScreen,
            };
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 加载 mock-layout.yaml:RJ-S1-04 要求 MockGame 启动时读取,删除硬编码。
    /// 候选路径:工作目录下 configs/mock-layout.yaml(便于 dotnet run),
    /// 或可执行文件并列的 Configs/mock-layout.yaml(csproj CopyToOutputDirectory)。
    /// </summary>
    private static MockLayoutConfig LoadLayoutOrFail()
    {
        var candidates = new[]
        {
            System.IO.Path.Combine(Directory.GetCurrentDirectory(), "configs", "mock-layout.yaml"),
            System.IO.Path.Combine(AppContext.BaseDirectory, "Configs", "mock-layout.yaml"),
        };
        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return MockLayoutLoader.Load(path);
            }
        }

        throw new FileNotFoundException(
            "mock-layout.yaml 未找到,已搜索: " + string.Join(", ", candidates));
    }
}
