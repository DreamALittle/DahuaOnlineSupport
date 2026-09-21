namespace DH2.Core.Models;

/// <summary>
/// 动作执行结果(点击 / 匹配 / 原子操作 等)。
/// </summary>
/// <param name="Success">是否成功。</param>
/// <param name="Attempts">本动作已尝试次数(无重试时为 1)。</param>
/// <param name="FailReason">失败原因简述;成功时为 <see cref="string.Empty"/>。</param>
public sealed record ActionResult(bool Success, int Attempts, string FailReason);
