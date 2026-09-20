using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DH2.MockGame.Services;
using DH2.MockGame.ViewModels;

namespace DH2.MockGame.Views;

public partial class MainWindow : Window
{
    private RawMessageLogger? _rawLogger;

    public MainWindow()
    {
        InitializeComponent();

        Opened += OnWindowOpened;
        Closing += OnWindowClosing;

        // UI 语义日志:Window 级 Pointer 事件(100% 缩放下 DIP == 像素)
        PointerPressed += OnWindowPointerPressed;
        PointerReleased += OnWindowPointerReleased;
    }

    private void OnWindowOpened(object? sender, System.EventArgs e)
    {
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero)
        {
            // 调试环境或无平台句柄时跳过子类化(测试断言需真实窗口,非调试)
            return;
        }

        _rawLogger = new RawMessageLogger(hwnd, AppHost.Current.Log);
        var installed = _rawLogger.Install();
        if (!installed)
        {
            _rawLogger.Dispose();
            _rawLogger = null;
        }
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        _rawLogger?.Dispose();
        _rawLogger = null;
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 100% 缩放下 DIP == 像素(技术设计 §10 陷阱#2)
        var p = e.GetPosition(this);
        AppHost.Current.Log.WriteUi("PointerPressed", p.X, p.Y);
    }

    private void OnWindowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var p = e.GetPosition(this);
        AppHost.Current.Log.WriteUi("PointerReleased", p.X, p.Y);
    }

    /// <summary>
    /// Avalonia Button Click 事件(技术设计 §7:按钮逻辑必须由 Button Click 驱动;
    /// 子类化钩子只记录不处理)。
    /// </summary>
    private void OnMainButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.OnButtonClicked();
        }
    }
}
