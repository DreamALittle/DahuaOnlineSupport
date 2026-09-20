namespace DH2.Core.Config;

/// <summary>
/// MockGame 布局单一真源(<c>configs/mock-layout.yaml</c>,技术设计 §7 / §8)。
/// </summary>
/// <remarks>
/// M0 字段默认值与 §7 布局表一致;缺省值使 MockGame 在配置缺失时仍能启动(S1-3 由 Dev B 实现)。
/// </remarks>
public sealed class MockLayoutConfig
{
    /// <summary>档案名(与 <see cref="DevConfig.Profile"/> 对齐)。</summary>
    public string Profile { get; init; } = "mock_800x600";

    /// <summary>客户区尺寸。</summary>
    public MockWindowConfig Window { get; init; } = new(800, 600);

    /// <summary>任务追踪栏矩形。</summary>
    public MockRect Taskbar { get; init; } = new(16, 16, 320, 88);

    /// <summary>状态文本位置。</summary>
    public MockStatusPosition Status { get; init; } = new(16, 116);

    /// <summary>主按钮矩形。</summary>
    public MockRect Button { get; init; } = new(16, 180, 140, 48);

    /// <summary>状态机计时。</summary>
    public MockStateTimings StateTimings { get; init; } = new(2000);

    /// <summary>MockGame 临时目录(可含 <c>%TEMP%</c> 等环境变量;由调用方展开)。</summary>
    public string TempDir { get; init; } = "%TEMP%/dh2-mockgame";
}