using DH2.Core.Contracts;
using DH2.Core.Models;

namespace DH2.Input;

/// <summary>
/// 后台输入驱动(M0 真实实现由 Dev B 负责;本文件为 S4-2 click 命令的临时桩,等待 Dev B 推送 S4-1 后 rebase 替换)。
/// </summary>
/// <remarks>
/// <para>M0-S4(S4-2 click 命令)阶段,Dev A 编写 click 命令需要 <see cref="IInputDriver"/> 引用;
/// Dev B 的 S4-1 (PostMessageDriver)尚未推送(<c>origin/dev-b/m0-s4</c> 不存在)。</para>
/// <para>本桩保留 <see cref="IInputDriver"/> 接口契约 + 一个抛 <see cref="NotImplementedException"/>
/// 的实现,保证 S4-2 click 命令可独立 build/test/format;</para>
/// <para>Dev B 推送 S4-1 后,本文件会被其正式实现覆盖(本分支 rebase 解决冲突即可)。</para>
/// </remarks>
public sealed class PostMessageDriver : IInputDriver
{
    public ActionResult Click(long hwnd, int clientX, int clientY)
    {
        // 临时桩:Dev B S4-1 推送后由真实实现覆盖(WM_MOUSEMOVE 可选 → WM_LBUTTONDOWN → PostClickDelayMs → WM_LBUTTONUP,
        // 经 Win32Coord.ToLParam 编码,失败返回 Success=false + "PostMessage returned false")。
        throw new NotImplementedException(
            "PostMessageDriver.Click awaiting Dev B S4-1 push; see ITER-M0-技术设计.md §3.3");
    }

    public void Dispose()
    {
        // 无持有资源;Dev B 实现按需添加。
    }
}
