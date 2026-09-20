namespace DH2.MockGame.Models;

/// <summary>
/// 与 configs/mock-layout.yaml 一一对应的强类型配置(技术设计 §8 字段)。
/// 100% 缩放下坐标 = 像素 = DIP(陷阱 §10.1,§10.2)。
/// </summary>
public sealed class MockLayout
{
    public string TempDir { get; set; } = "%TEMP%/dh2-mockgame";
    public WindowConfig Window { get; set; } = new();
    public TaskbarConfig Taskbar { get; set; } = new();
    public StatusConfig Status { get; set; } = new();
    public ButtonConfig Button { get; set; } = new();
    public StateTimingsConfig StateTimings { get; set; } = new();
}

public sealed class WindowConfig
{
    public string Title { get; set; } = "DH2.MockGame";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
}

public sealed class TaskbarConfig
{
    public int X { get; set; } = 16;
    public int Y { get; set; } = 16;
    public int Width { get; set; } = 320;
    public int Height { get; set; } = 88;
    public string Background { get; set; } = "#1E1E2E";
    public string BorderColor { get; set; } = "#555555";
    public int BorderThickness { get; set; } = 2;
    public string TextColor { get; set; } = "#FFFFFF";
    public int FontSize { get; set; } = 20;
    public string TextTemplate { get; set; } = "师门任务 ({n}/20)";
}

public sealed class StatusConfig
{
    public int X { get; set; } = 16;
    public int Y { get; set; } = 116;
    public string TextColor { get; set; } = "#FFFFFF";
    public int FontSize { get; set; } = 18;
    public StatusTexts Texts { get; set; } = new();
}

public sealed class StatusTexts
{
    public string Idle { get; set; } = "待机";
    public string Pathfinding { get; set; } = "寻路中...";
    public string Arrived { get; set; } = "已到达目的地";
}

public sealed class ButtonConfig
{
    public int X { get; set; } = 16;
    public int Y { get; set; } = 180;
    public int Width { get; set; } = 140;
    public int Height { get; set; } = 48;
    public string Background { get; set; } = "#2D5BFF";
    public string TextColor { get; set; } = "#FFFFFF";
    public int FontSize { get; set; } = 18;
    public ButtonTexts Texts { get; set; } = new();
}

public sealed class ButtonTexts
{
    public string Go { get; set; } = "前往";
    public string Return { get; set; } = "返回";
}

public sealed class StateTimingsConfig
{
    public int PathfindingMs { get; set; } = 2000;
}