using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DH2.MockGame.Models;
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
            // 加载布局配置(失败抛,进程退出码由运行时兜底)
            var layout = MockLayoutLoader.Load();

            // 装配应用级服务(构造内清理旧日志/状态)
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
}