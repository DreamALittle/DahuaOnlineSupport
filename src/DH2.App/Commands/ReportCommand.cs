using DH2.App.Cli;
using DH2.Core.Config;

namespace DH2.App.Commands;

/// <summary>
/// <c>dh2ctl report --target game</c> —— M0 真机验证向导(技术设计 §6 + S4-4)。
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>仅实现向导:分步输出 V1~V6 操作指引(命令示例 / 通过判据 / 证据要求);</item>
///   <item>创建证据目录 <c>artifacts/m0-report/</c>;</item>
///   <item><b>严禁自行执行任何真实游戏窗口操作</b>(本命令不触发截屏 / 输入 / 匹配 / 点击);</item>
///   <item>用户按向导回填 <c>docs/iterations/M0/ITER-M0-真机验证报告.md</c>(模板见测试设计 §4)。</item>
/// </list>
/// <para>S4-4 SAC4-3 验证:<c>report</c> 向导输出 V1~V6 步骤指引并创建证据目录。</para>
/// </remarks>
public sealed class ReportCommand : IDh2Command
{
    public string Name => "report";

    private const string Target = "game";
    private const string ReportSubdir = "artifacts/m0-report";

    public int Execute(DevConfig config, IReadOnlyDictionary<string, string> options, CancellationToken ct)
    {
        if (!options.TryGetValue("target", out var target) ||
            !string.Equals(target, Target, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"[usage error] --target {Target} (only '{Target}' supported in M0)");
            return (int)ExitCode.UsageError;
        }

        // 创建证据目录(若用户后续放截图/录屏,直接落入此处)
        Directory.CreateDirectory(ReportSubdir);

        PrintBanner();
        PrintPrerequisites();
        PrintSteps();
        PrintEvidenceCollection();
        PrintConclusionChecklist();
        PrintClosing(config);

        return (int)ExitCode.Success;
    }

    private static void PrintBanner()
    {
        Console.WriteLine("================================================================");
        Console.WriteLine(" DH2 M0 真机验证向导 — 目标:game(大话西游2 真机游戏窗口)");
        Console.WriteLine(" 模式:仅向导,不自动执行");
        Console.WriteLine(" 证据目录:" + ReportSubdir);
        Console.WriteLine("================================================================");
    }

    private static void PrintPrerequisites()
    {
        Console.WriteLine();
        Console.WriteLine("[0] 前置条件");
        Console.WriteLine(" - 游戏客户端已进入世界场景(非登录 / 选角 / 大厅)");
        Console.WriteLine(" - 游戏窗口非全屏(窗口化 / 无边框);Windows 显示缩放记录实际值(@100% / @150% / ...)");
        Console.WriteLine(" - 游戏与 dh2ctl 同权限级(均普通 / 均管理员;不一致时 PostMessage 被 UIPI 静默丢弃,记入报告)");
        Console.WriteLine(" - 配置文件已准备:cp configs/local/game.yaml.example configs/local/game.yaml 并按实际窗口标题调整 titlePattern");
    }

    private static void PrintSteps()
    {
        Console.WriteLine();
        Console.WriteLine("[1] V1 — 窗口枚举(enumerate)");
        Console.WriteLine(" 命令: dh2ctl enumerate --config configs/local/game.yaml");
        Console.WriteLine(" 期望: 控制台列出 1 条记录,Hwnd / Title / Process / ClientRect(800×600 @100% 或物理像素 @其他)正确");
        Console.WriteLine(" 证据: 截图控制台输出,保存到 " + ReportSubdir + "/v1-enumerate.png");
        Console.WriteLine();
        Console.WriteLine("[2] V2 — 连续截屏(capture)");
        Console.WriteLine(" 命令: dh2ctl capture --hwnd <上一步拿到的 hwnd> --count 5");
        Console.WriteLine(" 期望: 5 张 PNG,内容是游戏画面(非黑帧 / 非桌面其他区域)");
        Console.WriteLine(" 证据: 5 张 PNG,保存到 " + ReportSubdir + "/v2-capture/");
        Console.WriteLine();
        Console.WriteLine("[3] V3 — 框选任务追踪栏并入库(save-template)");
        Console.WriteLine(" 命令: dh2ctl save-template --hwnd <hwnd> --x 16 --y 16 --w 320 --h 88 --key tracker_bar");
        Console.WriteLine(" 期望: 模板 PNG 内容正确(任务追踪栏视觉特征),manifest.yaml 出现 tracker_bar 条目");
        Console.WriteLine(" 证据: 模板 PNG + templates/mock_800x600/manifest.yaml 文本 + " + ReportSubdir + "/v3-save-template/");
        Console.WriteLine();
        Console.WriteLine("[4] V4 — 定位任务追踪栏(match)");
        Console.WriteLine(" 命令: dh2ctl match --hwnd <hwnd> --key tracker_bar");
        Console.WriteLine(" 期望: found=true 且 score≥0.8(JSON 行 score 字段)");
        Console.WriteLine(" 证据: 控制台输出 JSON 行,保存到 " + ReportSubdir + "/v4-match.json");
        Console.WriteLine();
        Console.WriteLine("[5] V5 — 关键判定:点击任务条目(click)");
        Console.WriteLine(" 命令: dh2ctl click --hwnd <hwnd> --x <任务条目中心 X> --y <任务条目中心 Y>");
        Console.WriteLine("      (<x> <y> 由 V4 match center 与 clickOffset: {x:0,y:0} 推算)");
        Console.WriteLine(" 期望: 角色开始自动寻路移动(此为 PostMessage 主路线是否可行的核心证据)");
        Console.WriteLine(" 证据: 点击前后两张截图(寻路移动开始后的画面)或 15s 录屏,保存到 " + ReportSubdir + "/v5-click/");
        Console.WriteLine();
        Console.WriteLine("[6] V6(可选) — 战斗外其他可点击 UI 重复 V5");
        Console.WriteLine(" 命令: dh2ctl click --hwnd <hwnd> --x ... --y ...(打开任务面板按钮等)");
        Console.WriteLine(" 期望: 面板打开 / 触发对应 UI 行为");
        Console.WriteLine(" 证据: 截图,保存到 " + ReportSubdir + "/v6-optional/");
    }

    private static void PrintEvidenceCollection()
    {
        Console.WriteLine();
        Console.WriteLine("[证据收集要求]");
        Console.WriteLine(" 所有截图/录屏/JSON/控制台输出放入 " + ReportSubdir + "/ 对应子目录");
        Console.WriteLine(" 权限组合必须在报告 " + "(均普通 / 均管理员 / 不一致 + 说明)");
        Console.WriteLine(" Windows 显示缩放必须记录(@100% / @150% / 其他)");
    }

    private static void PrintConclusionChecklist()
    {
        Console.WriteLine();
        Console.WriteLine("[结论勾选 — 回填到 docs/iterations/M0/ITER-M0-真机验证报告.md]");
        Console.WriteLine("  [ ] V5 通过(角色移动):游戏响应后台 PostMessage → 执行层维持后台消息主路线");
        Console.WriteLine("  [ ] V5 不通过但 V2/V3/V4 通过:截屏/匹配可用但点击无响应 → 降级焦点轮转,转架构师处理");
        Console.WriteLine("  [ ] V2 即不通过(截屏不可用):转架构师复议截屏方案(GDI vs WGC vs 其他)");
        Console.WriteLine("  异常现象记录:");
        Console.WriteLine("    (权限 / 缩放 / 焦点 / DPI / 暗色模式 / 高 DPI 缩放 / 多显示器 / ...)");
    }

    private static void PrintClosing(DevConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("[执行约束]");
        Console.WriteLine(" 本命令仅打印向导,严禁自行触发任何真实游戏窗口操作。");
        Console.WriteLine(" 当前配置:" + config.Profile + " / templates=" + config.Paths.Templates + " / artifacts=" + config.Paths.Artifacts);
        Console.WriteLine("================================================================");
        Console.WriteLine(" 向导结束;请按上述步骤执行真机验证并回填 ITER-M0-真机验证报告.md");
        Console.WriteLine("================================================================");
    }
}
