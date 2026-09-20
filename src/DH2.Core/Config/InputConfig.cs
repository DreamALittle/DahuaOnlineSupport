namespace DH2.Core.Config;

/// <summary>
/// 输入驱动配置。
/// </summary>
/// <param name="Driver">
/// 输入驱动:<c>background</c>=后台 PostMessage(M0 实现);<c>foreground</c>=焦点轮转(M0 未实现,启动即报"未实现"错误)。
/// </param>
/// <param name="PostClickDelayMs">点击序列中 <c>WM_LBUTTONDOWN</c> 与 <c>WM_LBUTTONUP</c> 之间的延迟(毫秒)。</param>
public sealed record InputConfig(string Driver = "background", int PostClickDelayMs = 50);