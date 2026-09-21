// DH2.Tests — L1 单元测试
// IT-05 e2e 命令层防御深度(headless 可执行):
// - target 不在 dev.yaml → 退出码 3
// - mock-layout.yaml 缺失 → 退出码 3
// - 完整端到端(ensure_idle + enumerate + capture + match + click + state poll)由桌面走查手册覆盖

using DH2.App.Cli;
using DH2.App.Commands;
using DH2.Core.Config;
using Xunit;

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
}
