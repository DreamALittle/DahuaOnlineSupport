// DH2.Tests — L1 单元测试
// IT-07(异常输入防御)—— dh2ctl click 命令层防御深度:
// - 缺参 / 负值 → usage error 退出码 2
// - Mock IInputDriver 返 ActionResult(false,...) → JSON 行 {success:false} + 退出码 0(结构化返回)
// - Mock IInputDriver 抛 InvalidDataException → 退出码 3
// - Mock IInputDriver 抛 NotImplementedException(S4-2 临时桩兼容) → 退出码 3
// - Mock IInputDriver 抛 OperationCanceledException(IT-07 防御) → 退出码 3

using System.Text.Json;
using DH2.App.Cli;
using DH2.App.Commands;
using DH2.Core.Config;
using DH2.Core.Contracts;
using DH2.Core.Models;
using Xunit;

namespace DH2.Tests.Unit.Commands;

[Collection("ConsoleRedirection")]
public class ClickCommandTests : IDisposable
{
    private readonly ConsoleRedirectionFixture _fixture;

    public ClickCommandTests(ConsoleRedirectionFixture fixture)
    {
        _fixture = fixture;
    }

    private sealed class MockInputDriver : IInputDriver
    {
        public Func<long, int, int, ActionResult>? OnClick { get; set; }

        public ActionResult Click(long hwnd, int clientX, int clientY)
        {
            return OnClick is null
                ? new ActionResult(true, 1, string.Empty)
                : OnClick(hwnd, clientX, clientY);
        }

        public void Dispose() { /* no-op */ }
    }

    private static DevConfig NewDevConfig() => new DevConfig
    {
        Profile = "mock_800x600",
        Paths = new PathConfig { Templates = "templates", Artifacts = "artifacts" },
        Matching = new MatchingConfig { DefaultThreshold = 0.85 },
        Input = new InputConfig { Driver = "background", PostClickDelayMs = 50 },
    };

    // xUnit 并发跑不同测试类时,Console.Out/Error 是进程单例。
    // 用跨测试类共享 lock + try/finally 串行化重定向,防止跨测试串输出。
    private object ConsoleLock => _fixture.Lock;

    private (string stdout, string stderr, int exit) Run(
        MockInputDriver driver,
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
                var cmd = new ClickCommand(driver);
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

    public void Dispose() { /* no-op */ }

    // ───── IT-07 异常输入防御 ─────

    [Fact]
    public void Click_MissingHwnd_UsageErrorExit2()
    {
        var driver = new MockInputDriver();
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["x"] = "100",
            ["y"] = "200",
        });
        Assert.Equal((int)ExitCode.UsageError, exit);
        Assert.Contains("--hwnd", stderr);
    }

    [Fact]
    public void Click_NegativeHwnd_UsageErrorExit2()
    {
        var driver = new MockInputDriver();
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "-1",
            ["x"] = "100",
            ["y"] = "200",
        });
        Assert.Equal((int)ExitCode.UsageError, exit);
        Assert.Contains("--hwnd", stderr);
    }

    [Fact]
    public void Click_ZeroHwnd_UsageErrorExit2()
    {
        var driver = new MockInputDriver();
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "0",
            ["x"] = "100",
            ["y"] = "200",
        });
        Assert.Equal((int)ExitCode.UsageError, exit);
        Assert.Contains("--hwnd", stderr);
    }

    [Fact]
    public void Click_MissingX_UsageErrorExit2()
    {
        var driver = new MockInputDriver();
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["y"] = "200",
        });
        Assert.Equal((int)ExitCode.UsageError, exit);
        Assert.Contains("--x", stderr);
    }

    [Fact]
    public void Click_MissingY_UsageErrorExit2()
    {
        var driver = new MockInputDriver();
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["x"] = "100",
        });
        Assert.Equal((int)ExitCode.UsageError, exit);
        Assert.Contains("--y", stderr);
    }

    [Fact]
    public void Click_NegativeX_UsageErrorExit2()
    {
        var driver = new MockInputDriver();
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["x"] = "-1",
            ["y"] = "200",
        });
        Assert.Equal((int)ExitCode.UsageError, exit);
        Assert.Contains("--x", stderr);
    }

    [Fact]
    public void Click_NegativeY_UsageErrorExit2()
    {
        var driver = new MockInputDriver();
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["x"] = "100",
            ["y"] = "-1",
        });
        Assert.Equal((int)ExitCode.UsageError, exit);
        Assert.Contains("--y", stderr);
    }

    [Fact]
    public void Click_HwndNonNumeric_UsageErrorExit2()
    {
        var driver = new MockInputDriver();
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "abc",
            ["x"] = "100",
            ["y"] = "200",
        });
        Assert.Equal((int)ExitCode.UsageError, exit);
        Assert.Contains("--hwnd", stderr);
    }

    [Fact]
    public void Click_DriverReturnsSuccessTrue_Exit0WithJsonSuccess()
    {
        // 真机正常路径(mock IInputDriver 返 success)
        var driver = new MockInputDriver
        {
            OnClick = (hwnd, x, y) => new ActionResult(true, 1, string.Empty),
        };
        var (stdout, _, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["x"] = "86",
            ["y"] = "204",
        });

        Assert.Equal((int)ExitCode.Success, exit);
        Assert.Contains("\"success\":true", stdout);
        Assert.Contains("\"attempts\":1", stdout);
        Assert.Contains("\"failReason\":\"\"", stdout);
    }

    [Fact]
    public void Click_DriverReturnsSuccessFalse_Exit0WithJsonSuccessFalse()
    {
        // 真实路径下 PostMessage 返回 false → IInputDriver 返 ActionResult(false,1,"PostMessage returned false")
        // ClickCommand 不抛,JSON 显示 success=false,退出码 0(结构化返回)
        var driver = new MockInputDriver
        {
            OnClick = (hwnd, x, y) => new ActionResult(false, 1, "PostMessage returned false (LBUTTONUP)"),
        };
        var (stdout, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["x"] = "86",
            ["y"] = "204",
        });

        Assert.Equal((int)ExitCode.Success, exit);
        Assert.Contains("\"success\":false", stdout);
        Assert.Contains("PostMessage returned false", stdout);
        Assert.Empty(stderr); // 不写错误流,业务失败走 JSON
    }

    [Fact]
    public void Click_DriverThrowsNotImplemented_Exit3ConfigFailure()
    {
        // 兼容 S4-2 临时桩:Dev B S4-1 推送前 Dev A 的桩抛 NotImplementedException
        var driver = new MockInputDriver
        {
            OnClick = (hwnd, x, y) => throw new NotImplementedException("PostMessageDriver awaiting Dev B S4-1"),
        };
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["x"] = "86",
            ["y"] = "204",
        });

        Assert.Equal((int)ExitCode.ConfigOrExecutionFailure, exit);
        Assert.Contains("[click error]", stderr);
        Assert.Contains("awaiting Dev B S4-1", stderr);
    }

    [Fact]
    public void Click_DriverThrowsInvalidDataException_Exit3ConfigFailure()
    {
        // 业务层防御:InputDriver 抛 InvalidDataException → 退出码 3
        var driver = new MockInputDriver
        {
            OnClick = (hwnd, x, y) => throw new InvalidDataException("invalid client coordinates"),
        };
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["x"] = "86",
            ["y"] = "204",
        });

        Assert.Equal((int)ExitCode.ConfigOrExecutionFailure, exit);
        Assert.Contains("[click error]", stderr);
        Assert.Contains("invalid client coordinates", stderr);
    }

    [Fact]
    public void Click_DriverThrowsOperationCanceled_Exit3ConfigFailure()
    {
        // 操作被取消 → 退出码 3
        var driver = new MockInputDriver
        {
            OnClick = (hwnd, x, y) => throw new OperationCanceledException("cancelled by user"),
        };
        var (_, stderr, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["x"] = "86",
            ["y"] = "204",
        });

        Assert.Equal((int)ExitCode.ConfigOrExecutionFailure, exit);
        Assert.Contains("[click error]", stderr);
    }

    [Fact]
    public void Click_DriverReceivesCorrectCoordinates()
    {
        // 防御坐标误传:验证 driver 收到的 hwnd/x/y 与入参一致
        long receivedHwnd = 0;
        int receivedX = -1, receivedY = -1;

        var driver = new MockInputDriver
        {
            OnClick = (hwnd, x, y) =>
            {
                receivedHwnd = hwnd;
                receivedX = x;
                receivedY = y;
                return new ActionResult(true, 1, string.Empty);
            },
        };
        var (_, _, exit) = Run(driver, new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["x"] = "86",
            ["y"] = "204",
        });

        Assert.Equal((int)ExitCode.Success, exit);
        Assert.Equal(12345L, receivedHwnd);
        Assert.Equal(86, receivedX);
        Assert.Equal(204, receivedY);
    }
}
