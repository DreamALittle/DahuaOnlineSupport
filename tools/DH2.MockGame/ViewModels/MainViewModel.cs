using CommunityToolkit.Mvvm.ComponentModel;
using DH2.MockGame.Models;
using DH2.MockGame.Services;

namespace DH2.MockGame.ViewModels;

/// <summary>
/// MockGame 主窗口 ViewModel(技术设计 §2 选型:Avalonia 11 + CommunityToolkit.Mvvm)。
/// 持有状态机;属性绑定:StatusText / ButtonText / TaskbarText / IsButtonEnabled;
/// 命令:OnButtonPressed(由 Avalonia Button.Click 触发,不是 PostMessage 直接驱动)。
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly MockLayout _layout;
    private readonly MockStateMachine _stateMachine;

    public MainViewModel(MockLayout layout, StateStore store)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _stateMachine = new MockStateMachine(layout, store);
        StatusText = _stateMachine.GetStatusText();
        ButtonText = _stateMachine.GetButtonText();
        TaskbarText = _stateMachine.GetTaskbarText();
        IsButtonEnabled = _stateMachine.IsButtonEnabled();
    }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "";

    [ObservableProperty]
    public partial string ButtonText { get; set; } = "";

    [ObservableProperty]
    public partial string TaskbarText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsButtonEnabled { get; set; }

    /// <summary>
    /// 由 Avalonia Button Click 事件调用,驱动状态机并刷新绑定。
    /// Pathfinding 期间调度异步转移;异步到达 Arrived 后再次刷新。
    /// </summary>
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
        await Task.Delay(_layout.StateTimings.PathfindingMs);
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