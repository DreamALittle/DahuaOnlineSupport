namespace DH2.Core.Config;

/// <summary>
/// MockGame 主按钮双态文案(Idle 时显示 <see cref="Go"/>;Arrived 时显示 <see cref="Return"/>)。
/// </summary>
public sealed class MockButtonTexts
{
    /// <summary>Idle 状态按钮文字(<c>前往</c>)。</summary>
    public string Go { get; set; } = "前往";

    /// <summary>Arrived 状态按钮文字(<c>返回</c>)。</summary>
    public string Return { get; set; } = "返回";
}