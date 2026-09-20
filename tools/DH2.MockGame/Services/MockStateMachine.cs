using DH2.MockGame.Models;

namespace DH2.MockGame.Services;

/// <summary>
/// MockGame 状态机(技术设计 §7):
///   Idle --点击按钮--> Pathfinding(按钮禁用,文本="寻路中...",2000ms)--> Arrived
///   Arrived --点击按钮--> Idle(counter+1,文本="待机")
/// 状态转移同步触发 StateStore.Write,保证 state.json 与 UI 同步。
/// </summary>
public sealed class MockStateMachine
{
    private readonly Models.MockLayout _layout;
    private readonly StateStore _store;

    public MockStateMachine(Models.MockLayout layout, StateStore store)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Counter = 0;
        Phase = MockPhase.Idle;
        // 启动时落盘 Idle(counter=0)
        _store.Write(Phase, Counter);
    }

    public MockPhase Phase { get; private set; }

    public int Counter { get; private set; }

    /// <summary>由 Avalonia Button.Click 事件驱动。返回新 phase。</summary>
    public MockPhase OnButtonClicked()
    {
        switch (Phase)
        {
            case MockPhase.Idle:
                EnterPathfinding();
                break;
            case MockPhase.Arrived:
                ReturnToIdle();
                break;
            case MockPhase.Pathfinding:
                // Pathfinding 期间按钮禁用,不应到达此处
                break;
        }

        _store.Write(Phase, Counter);
        return Phase;
    }

    /// <summary>由定时器到达 PathfindingMs 后调用。返回是否真发生转移。</summary>
    public bool OnPathfindingElapsed()
    {
        if (Phase != MockPhase.Pathfinding)
        {
            return false;
        }

        Phase = MockPhase.Arrived;
        _store.Write(Phase, Counter);
        return true;
    }

    private void EnterPathfinding()
    {
        Phase = MockPhase.Pathfinding;
    }

    private void ReturnToIdle()
    {
        Phase = MockPhase.Idle;
        Counter++;
    }

    /// <summary>当前状态文本(用于 UI 状态文本)。</summary>
    public string GetStatusText() => Phase switch
    {
        MockPhase.Idle => _layout.Status.Texts.Idle,
        MockPhase.Pathfinding => _layout.Status.Texts.Pathfinding,
        MockPhase.Arrived => _layout.Status.Texts.Arrived,
        _ => string.Empty,
    };

    /// <summary>当前按钮文本。</summary>
    public string GetButtonText() => Phase switch
    {
        MockPhase.Idle => _layout.Button.Texts.Go,
        MockPhase.Arrived => _layout.Button.Texts.Return,
        // Pathfinding 期间按钮禁用,文本仍为 Go 以避免 UI 抖动(测试断言按状态机切换而非文本)
        _ => _layout.Button.Texts.Go,
    };

    /// <summary>当前按钮是否可点击。</summary>
    public bool IsButtonEnabled() => Phase != MockPhase.Pathfinding;

    /// <summary>任务栏文本(n=counter)。</summary>
    public string GetTaskbarText() => string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        _layout.Taskbar.TextTemplate,
        Counter);
}
