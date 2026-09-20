// DH2.App — dh2ctl 控制台 CLI 入口
//
// M0-S1 范围:仅交付工程骨架。CLI 子命令在后续 Sprint 渐进接入:
//   S2-3 enumerate / capture / save-template    (Dev A)
//   S3-3 save-template / match                   (Dev A)
//   S4-2 click / e2e / report                    (Dev A)
//
// 当前 Main 仅打印骨架提示,退出码 0。
// 完整退出码约定见技术设计 §6:0=成功 / 2=用法错误 / 3=配置或执行失败。

namespace DH2.App;

internal static class Program
{
    private static int Main(string[] args)
    {
        System.Console.WriteLine("dh2ctl (M0-S1 scaffold). CLI commands arrive in S2~S4.");
        return 0;
    }
}
