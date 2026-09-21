using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using DH2.MockGame.Services;
using DH2.MockGame.ViewModels;

namespace DH2.MockGame.Views;

public partial class MainWindow : Window
{
    private RawMessageLogger? _rawLogger;

    /// <summary>§7 按钮蓝底 RGB;RJ-S4-04 修复:用代码强设 SolidColorBrush 而非 XAML 字面量。</summary>
    private static readonly Color ButtonBgColor = Color.FromRgb(0x2D, 0x5B, 0xFF);

    public MainWindow()
    {
        InitializeComponent();

        Opened += OnWindowOpened;
        Closing += OnWindowClosing;

        // UI 语义日志:Window 级 Pointer 事件(100% 缩放下 DIP == 像素)
        PointerPressed += OnWindowPointerPressed;
        PointerReleased += OnWindowPointerReleased;

        // RJ-S4-04:监听 Button IsEnabledProperty 变化 — Avalonia 11 FluentTheme Button 在
        // IsEnabled 切换时(Idle→Pathfinding→Arrived→Idle)会覆盖字面 Background="#2D5BFF"
        // 为"disabled 透明"或"hover 变体";为保证 Idle/Arrived 两态按钮恒有蓝底,
        // 每次 IsEnabled 变化时强制设回 SolidColorBrush。
        MainButton.PropertyChanged += OnMainButtonPropertyChanged;
    }

    private void OnMainButtonPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Button.IsEnabledProperty)
        {
            // 强制重设蓝底(无视 Theme 覆盖)
            MainButton.Background = new SolidColorBrush(ButtonBgColor);
        }
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

        // RJ-S4-04:启动期强制设蓝底(防止 XAML 字面 Background 被任何 Theme override 吃掉)
        MainButton.Background = new SolidColorBrush(ButtonBgColor);
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
