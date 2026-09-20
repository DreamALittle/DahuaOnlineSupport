namespace DH2.App.Cli;

/// <summary>
/// dh2ctl 进程退出码(技术设计 §6)。
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><see cref="Success"/> = 0:命令正常完成。</item>
///   <item><see cref="UsageError"/> = 2:参数解析失败、未知子命令、缺关键参数。</item>
///   <item><see cref="ConfigOrExecutionFailure"/> = 3:配置文件缺失/校验失败/YAML 解析错误/运行期异常(基础设施错误)。</item>
/// </list>
/// </remarks>
public enum ExitCode
{
    /// <summary>成功。</summary>
    Success = 0,

    /// <summary>用法错误(参数 / 子命令 / 缺关键标志)。</summary>
    UsageError = 2,

    /// <summary>配置或执行失败(文件缺失 / 校验错误 / 运行时异常)。</summary>
    ConfigOrExecutionFailure = 3,
}
