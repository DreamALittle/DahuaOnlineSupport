namespace DH2.Core.Config;

/// <summary>
/// 窗口目标配置:按标题正则 / 进程名过滤可见顶层窗口。
/// </summary>
/// <param name="Name">目标显示名(如 <c>mock</c> / <c>game</c>),仅做日志与展示。</param>
/// <param name="TitlePattern">窗口标题正则;与 <see cref="ProcessName"/> 至少一项非空(由 <see cref="ConfigValidator"/> 强制)。</param>
/// <param name="ProcessName">进程名(不含扩展名);可与 <see cref="TitlePattern"/> 同时存在(取交集)。</param>
public sealed record WindowTargetConfig(string Name, string? TitlePattern, string? ProcessName);
