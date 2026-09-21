using DH2.Core.Config;
using DH2.Core.Models;

namespace DH2.App.Commands;

/// <summary>
/// dh2ctl 子命令契约(S2-4 起)。
/// </summary>
/// <remarks>
/// 命令实现位于 <see cref="DH2.App.Commands"/>;Program.cs 派发器按子命令名解析到具体实现。
/// 命令失败语义:本接口不抛业务异常(结果对象风格),仅返回退出码;
/// 基础设施错误由 Program.cs 顶层 try/catch 兜底为退出码 3。
/// </remarks>
public interface IDh2Command
{
    /// <summary>子命令名(如 <c>enumerate</c> / <c>capture</c>),小写。</summary>
    string Name { get; }

    /// <summary>执行命令;返回 CLI 退出码。</summary>
    /// <param name="config">已加载并校验通过的 <see cref="DevConfig"/>。</param>
    /// <param name="options">解析后的选项字典(子命令专属键值)。</param>
    /// <param name="ct">取消令牌(可选)。</param>
    /// <returns>0 成功 / 2 用法错误 / 3 配置或执行失败。</returns>
    int Execute(DevConfig config, IReadOnlyDictionary<string, string> options, CancellationToken ct);
}
