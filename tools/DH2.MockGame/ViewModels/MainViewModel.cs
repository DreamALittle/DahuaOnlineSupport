using CommunityToolkit.Mvvm.ComponentModel;
using DH2.Core.Config;
using DH2.MockGame.Models;
using DH2.MockGame.Services;

namespace DH2.MockGame.ViewModels;

/// <summary>
/// MockGame 主窗口 ViewModel(技术设计 §2 选型:Avalonia 11 + CommunityToolkit.Mvvm)。
/// 持有状态机;属性绑定:StatusText / ButtonText / TaskbarText / IsButtonEnabled
/// 以及几何属性 TaskbarX/Y/W/H / StatusX/Y / ButtonX/Y/W/H(全部来自 <see cref="MockLayoutConfig"/>, RJ-S1-04)。
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly MockLayoutConfig _layout;
    private readonly MockStateMachine _stateMachine;

    public MainViewModel(MockLayoutConfig layout, StateStore store)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _stateMachine = new MockStateMachine(layout, store);
        StatusText = _stateMachine.GetStatusText();
        ButtonText = _stateMachine.GetButtonText();
        TaskbarText = _stateMachine.GetTaskbarText();
        IsButtonEnabled = _stateMachine.IsButtonEnabled();

        // 几何绑定(RJ-S1-04:删除硬编码,全部来自配置)
        TaskbarX = _layout.Taskbar.X;
        TaskbarY = _layout.Taskbar.Y;
        TaskbarWidth = _layout.Taskbar.Width;
        TaskbarHeight = _layout.Taskbar.Height;

        StatusX = _layout.Status.X;
        StatusY = _layout.Status.Y;

        ButtonX = _layout.Button.X;
        ButtonY = _layout.Button.Y;
        ButtonWidth = _layout.Button.Width;
        ButtonHeight = _layout.Button.Height;
    }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "";

    [ObservableProperty]
    public partial string ButtonText { get; set; } = "";

    [ObservableProperty]
    public partial string TaskbarText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsButtonEnabled { get; set; }

    // --- 几何(RJ-S1-04:来自 mock-layout.yaml) ---
    [ObservableProperty]
    public partial int TaskbarX { get; set; }

    [ObservableProperty]
    public partial int TaskbarY { get; set; }

    [ObservableProperty]
    public partial int TaskbarWidth { get; set; }

    [ObservableProperty]
    public partial int TaskbarHeight { get; set; }

    [ObservableProperty]
    public partial int StatusX { get; set; }

    [ObservableProperty]
    public partial int StatusY { get; set; }

    [ObservableProperty]
    public partial int ButtonX { get; set; }

    [ObservableProperty]
    public partial int ButtonY { get; set; }

    [ObservableProperty]
    public partial int ButtonWidth { get; set; }

    [ObservableProperty]
    public partial int ButtonHeight { get; set; }

    /// <summary>由 Avalonia Button Click 事件调用,驱动状态机并刷新绑定。</summary>
    public void OnButtonClicked()
    {
        _stateMachine.OnButtonClicked();
        Refresh();

        if (_stateMachine.Phase == MockPhase.Pathfinding)
        {
            _ = ScheduleArrivedAsync();
        }
    }

    private async Task ScheduleArrivedAsync()
    {
        await Task.Delay(_stateMachine.PathfindingMs);
        _stateMachine.OnPathfindingElapsed();
        Refresh();
    }

    private void Refresh()
    {
        StatusText = _stateMachine.GetStatusText();
        ButtonText = _stateMachine.GetButtonText();
        TaskbarText = _stateMachine.GetTaskbarText();
        IsButtonEnabled = _stateMachine.IsButtonEnabled();
    }
}
