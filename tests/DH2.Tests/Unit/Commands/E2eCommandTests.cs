// DH2.Tests — L1 单元测试
// IT-05 e2e 命令层防御深度(headless 可执行):
// - target 不在 dev.yaml → 退出码 3
// - mock-layout.yaml 缺失 → 退出码 3
// - 完整端到端(ensure_idle + enumerate + capture + match + click + state poll)由桌面走查手册覆盖
//
// RJ-S4-01 坐标推导 UT:
// - 1200×900 帧 + taskbar 实测中心 (264, 90) → 点击 (129, 306)【scale=1.5】
// - 800×600  帧 + taskbar 实测中心 (176, 60) → 点击 (86, 204)【scale=1.0,退化等同布局中心】

using DH2.App.Cli;
using DH2.App.Commands;
using DH2.Core.Config;
using DH2.Core.Models;
using Xunit;
using DH2Point = DH2.Core.Models.Point;

namespace DH2.Tests.Unit.Commands;

[Collection("ConsoleRedirection")]
public class E2eCommandTests
{
    private readonly ConsoleRedirectionFixture _fixture;

    public E2eCommandTests(ConsoleRedirectionFixture fixture)
    {
        _fixture = fixture;
    }

    private object ConsoleLock => _fixture.Lock;
    private static DevConfig NewDevConfig() => new DevConfig
    {
        Profile = "mock_800x600",
        Targets = new List<WindowTargetConfig>
        {
            new() { Name = "mock", TitlePattern = "^DH2\\.MockGame$" },
        },
        Paths = new PathConfig { Templates = "templates", Artifacts = "artifacts" },
        Matching = new MatchingConfig { DefaultThreshold = 0.85 },
        Input = new InputConfig { Driver = "background", PostClickDelayMs = 50 },
    };

    private (string stdout, string stderr, int exit) Run(
        E2eCommand cmd,
        Dictionary<string, string> options)
    {
        lock (ConsoleLock)
        {
            var stdout = new System.IO.StringWriter();
            var stderr = new System.IO.StringWriter();
            var origOut = Console.Out;
            var origErr = Console.Error;
            Console.SetOut(stdout);
            Console.SetError(stderr);
            try
            {
                var exit = cmd.Execute(NewDevConfig(), options, CancellationToken.None);
                return (stdout.ToString(), stderr.ToString(), exit);
            }
            finally
            {
                Console.SetOut(origOut);
                Console.SetError(origErr);
            }
        }
    }

    [Fact]
    public void E2e_TargetNotInDevYaml_Exit3ConfigFailure()
    {
        var cmd = new E2eCommand();
        var (_, stderr, exit) = Run(cmd, new Dictionary<string, string>
        {
            ["target"] = "nonexistent",
        });

        Assert.Equal((int)ExitCode.ConfigOrExecutionFailure, exit);
        Assert.Contains("[e2e error]", stderr);
        Assert.Contains("target 'nonexistent' not in dev.yaml", stderr);
    }

    [Fact]
    public void E2e_DefaultTarget_ConfigFailureAtEnumerateOrMatch()
    {
        // default target = "mock"(存在于 dev.yaml 中)
        // 后续步骤在 headless 无 MockGame 会失败;但本测试只关心"命令进入完整流程"的契约:
        // 应不立刻退码 2(不是 usage error),而是 3(配置/执行失败)。
        var cmd = new E2eCommand();
        var (_, stderr, exit) = Run(cmd, new Dictionary<string, string>());

        Assert.Equal((int)ExitCode.ConfigOrExecutionFailure, exit);
        // 任何 e2e error 都接受(可能在 enumerate / ensure_idle / match 任一阶段失败)
        Assert.Contains("[e2e error]", stderr);
    }

    // ───── RJ-S4-01 坐标推导 UT ─────

    private static MockLayoutConfig NewMockLayout() => new MockLayoutConfig
    {
        Window = new MockWindowConfig { Width = 800, Height = 600 },
        Taskbar = new MockTaskbarConfig { X = 16, Y = 16, Width = 320, Height = 88 },   // 中心 (176, 60)
        Button = new MockButtonConfig { X = 16, Y = 180, Width = 140, Height = 48 },   // 中心 (86, 204)
    };

    [Fact]
    public void E2e_ComputeClickPoint_Scale150PercentFrame_ReturnsFrameSpaceCoords()
    {
        // 150% 缩放下:layout 800×600 → 帧 1200×900;scale = 1.5。
        // mock-layout.yaml 按钮布局中心 (86, 204)、任务栏布局中心 (176, 60),布局空间偏移 (-90, 144)。
        // 帧空间偏移 = (-90, 144) × 1.5 = (-135, 216);
        // 实测任务栏中心 (264, 90)(150% 物理空间,见 qa/evidence/M0-S4-l2/match_taskbar.json)→
        // 点击 = (264 + (-135), 90 + 216) = (129, 306)。
        var layout = NewMockLayout();
        var taskbarCenter = new DH2Point(264, 90);

        var click = E2eCommand.ComputeButtonClickPoint(
            frameWidth: 1200,
            layout: layout,
            taskbarMeasuredCenter: taskbarCenter);

        Assert.Equal(new DH2Point(129, 306), click);
    }

    [Fact]
    public void E2e_ComputeClickPoint_Scale100PercentFrame_ReturnsLayoutCoords()
    {
        // 100% 缩放下:scale = 1.0,公式退化为 click = taskbarMeasuredCenter + (buttonLayoutCenter − taskbarLayoutCenter)。
        // 实测任务栏中心 = 布局中心 (176, 60)(物理像素等于 DIP,无换算)→ 点击 = 布局按钮中心 (86, 204)。
        var layout = NewMockLayout();
        var taskbarCenter = new DH2Point(176, 60);

        var click = E2eCommand.ComputeButtonClickPoint(
            frameWidth: 800,
            layout: layout,
            taskbarMeasuredCenter: taskbarCenter);

        Assert.Equal(new DH2Point(86, 204), click);
    }

    [Fact]
    public void E2e_ComputeClickPoint_LayoutWindowZero_Throws()
    {
        // 防御:layout.Window.Width <= 0(配置缺失或被改坏)→ 显式异常,而不是静默退化为 0/NaN。
        var layout = NewMockLayout();
        layout.Window.Width = 0;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            E2eCommand.ComputeButtonClickPoint(
                frameWidth: 800,
                layout: layout,
                taskbarMeasuredCenter: new DH2Point(176, 60)));
    }
}
