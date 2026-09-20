using DH2.Core.Config;
using DH2.Core.Models;

namespace DH2.Core.Contracts;

/// <summary>
/// 窗口枚举绑定抽象。
/// </summary>
/// <remarks>
/// M0 实现位于 <c>DH2.Input</c>(S2,Dev B):
/// <c>EnumWindows</c> → 可见性过滤 → 标题/进程名匹配 → <c>GetClientRect + ClientToScreen</c>。
/// </remarks>
public interface IWindowLocator
{
    /// <summary>
    /// 枚举可见顶层窗口并按 <paramref name="target"/> 过滤。
    /// </summary>
    /// <param name="target">窗口目标配置(标题正则 / 进程名至少一项非空)。</param>
    /// <returns>命中列表(可能为空;绝不返回 <c>null</c>)。</returns>
    IReadOnlyList<Win32Window> Enumerate(WindowTargetConfig target);
}
