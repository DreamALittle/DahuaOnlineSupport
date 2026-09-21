using DH2.Core.Models;

namespace DH2.Core.Contracts;

/// <summary>
/// 后台输入驱动抽象(技术设计 §6 "设计成本决策":通过 <c>IInputDriver</c> 抽象,运行时按配置切换)。
/// </summary>
/// <remarks>
/// M0 实现仅 <c>background</c>(<c>DH2.Input.PostMessageDriver</c>,S4-1 由 Dev B 实现);
/// <c>foreground</c>(焦点轮转)M0 未实现,启动即报"未实现"错误(技术设计 §2.3 + RJ-S1-05)。
/// </remarks>
public interface IInputDriver : IDisposable
{
    /// <summary>
    /// 在指定 HWND 客户区坐标处执行一次后台鼠标点击(MOUSEMOVE 可选 → LBUTTONDOWN → PostClickDelayMs → LBUTTONUP)。
    /// </summary>
    /// <param name="hwnd">目标窗口句柄。</param>
    /// <param name="clientX">客户区 X(像素,@100% 缩放 DIP == 像素)。</param>
    /// <param name="clientY">客户区 Y。</param>
    /// <returns><see cref="ActionResult"/>;底层 PostMessage 返回 false 即 <see cref="ActionResult.Success"/>=false + <see cref="ActionResult.FailReason"/>="PostMessage returned false"。</returns>
    ActionResult Click(long hwnd, int clientX, int clientY);
}
