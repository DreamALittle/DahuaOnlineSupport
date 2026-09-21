using DH2.Core.Config;
using DH2.MockGame.Models;

namespace DH2.MockGame.Services;

/// <summary>
/// MockGame 状态机(技术设计 §7):
///   Idle --点击按钮--> Pathfinding(按钮禁用,文本="寻路中...",PathfindingMs)--> Arrived
///   Arrived --点击按钮--> Idle(counter+1,文本="待机")
/// 几何与计时来自 Core.MockLayoutConfig(RJ-S1-04);文本采用 §7 标准值(本地常量 <see cref="MockGameTexts"/>)。
/// 状态转移同步触发 StateStore.Write,保证 state.json 与 UI 同步。
/// </summary>
public sealed class MockStateMachine
{
    private readonly MockLayoutConfig _layout;
    private readonly StateStore _store;

    public MockStateMachine(MockLayoutConfig layout, StateStore store)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Counter = 0;
        Phase = MockPhase.Idle;
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
                Phase = MockPhase.Pathfinding;
                break;
            case MockPhase.Arrived:
                Phase = MockPhase.Idle;
                Counter++;
                break;
            case MockPhase.Pathfinding:
                // Pathfinding 期间按钮禁用,不应到达此处
                break;
        }

        _store.Write(Phase, Counter);
        return Phase;
    }

    /// <summary>由定时器到达 <see cref="MockLayoutConfig.StateTimings"/>.PathfindingMs 后调用。</summary>
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

    public int PathfindingMs => _layout.StateTimings.PathfindingMs;

    /// <summary>当前状态文本(用于 UI 状态文本)。</summary>
    public string GetStatusText() => Phase switch
    {
        MockPhase.Idle => MockGameTexts.StatusIdle,
        MockPhase.Pathfinding => MockGameTexts.StatusPathfinding,
        MockPhase.Arrived => MockGameTexts.StatusArrived,
        _ => string.Empty,
    };

    /// <summary>当前按钮文本。</summary>
    public string GetButtonText() => Phase switch
    {
        MockPhase.Idle => MockGameTexts.ButtonGo,
        MockPhase.Arrived => MockGameTexts.ButtonReturn,
        // Pathfinding 期间按钮禁用,文本仍为 Go 以避免 UI 抖动
        _ => MockGameTexts.ButtonGo,
    };

    /// <summary>当前按钮是否可点击。</summary>
    public bool IsButtonEnabled() => Phase != MockPhase.Pathfinding;

    /// <summary>任务栏文本(n=counter)。</summary>
    public string GetTaskbarText() => string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        MockGameTexts.TaskbarTemplate,
        Counter);
}

/// <summary>
/// MockGame 文本/样式常量(技术设计 §7 标准值;不在 Core 字段中,内部常量化避免 yaml 噪声)。
/// </summary>
public static class MockGameTexts
{
    public const string StatusIdle = "待机";
    public const string StatusPathfinding = "寻路中...";
    public const string StatusArrived = "已到达目的地";
    public const string ButtonGo = "前往";
    public const string ButtonReturn = "返回";
    public const string TaskbarTemplate = "师门任务 ({0}/20)";
}
