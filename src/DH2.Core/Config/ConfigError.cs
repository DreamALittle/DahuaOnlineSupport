namespace DH2.Core.Config;

/// <summary>
/// 配置校验错误描述。聚合后由宿主统一打印并以退出码 3 退出(技术设计 §2.3)。
/// </summary>
/// <param name="Path">错误所在配置路径(如 <c>Matching.DefaultThreshold</c> / <c>Targets[1].TitlePattern</c>)。</param>
/// <param name="Message">人类可读的失败原因。</param>
public sealed record ConfigError(string Path, string Message);