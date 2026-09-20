namespace DH2.Core.Config;

/// <summary>
/// MockGame 布局单一真源(<c>configs/mock-layout.yaml</c>,技术设计 §7 / §8)——Core 域契约。
/// </summary>
/// <remarks>
/// M0-S2 改造(RJ-S1-03 / DEF-S1-01):
/// <list type="bullet">
///   <item>扩展为承载 Dev B 真实 mock-layout.yaml 的全部字段(tempDir / window / taskbar / status / button / stateTimings)。</item>
///   <item>类成员改为带默认值的 <c>{ get; set; }</c>,保证 YamlDotNet 可经无参构造实例化后再填值。</item>
///   <item>原 <c>Profile</c> 字段移除(Dev B 真实 YAML 不含此键;Profile 概念归 <see cref="DevConfig"/>)。</item>
/// </list>
/// dh2ctl 在 S2 的 <c>enumerate</c> / S4 的 <c>e2e</c> 等命令中按需读取本配置的几何/样式字段。
/// </remarks>
public sealed class MockLayoutConfig
{
    /// <summary>MockGame 临时目录(可含 <c>%TEMP%</c> 等环境变量;由调用方展开)。</summary>
    public string TempDir { get; set; } = "%TEMP%/dh2-mockgame";

    /// <summary>窗口(标题 + 客户区尺寸)。</summary>
    public MockWindowConfig Window { get; set; } = new();

    /// <summary>任务追踪栏矩形 + 样式。</summary>
    public MockTaskbarConfig Taskbar { get; set; } = new();

    /// <summary>状态文本位置 + 样式 + 三态文案。</summary>
    public MockStatusPosition Status { get; set; } = new();

    /// <summary>主按钮矩形 + 样式 + 双态文案。</summary>
    public MockButtonConfig Button { get; set; } = new();

    /// <summary>状态机计时。</summary>
    public MockStateTimings StateTimings { get; set; } = new();
}