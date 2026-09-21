// DH2.Tests — L1 单元测试
// UT-04 DevConfig 校验:targets 两字段全空 / threshold=1.2 / driver=foreground / 合法
// 参见 docs/iterations/M0/ITER-M0-测试设计.md §1 + DH2.Core.Config.ConfigValidator

using DH2.Core.Config;
using Xunit;

namespace DH2.Tests.Unit.Config;

public class DevConfigValidationTests
{
    // DevConfig 是 class + init 属性,无法 `with`;每个变体用 new 重新构造
    private static DevConfig ValidBase() => new()
    {
        Profile = "mock_800x600",
        Targets = new List<WindowTargetConfig>
        {
            new("mock", "^DH2\\.MockGame$", null),
        },
        Paths = new PathConfig("templates", "artifacts"),
        Matching = new MatchingConfig(0.85),
        Input = new InputConfig("background", 50),
    };

    [Fact]
    public void Validate_LegalConfig_ReturnsNoErrors()
    {
        var errors = ConfigValidator.Validate(ValidBase());
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NullConfig_ReturnsSingleRootError()
    {
        var errors = ConfigValidator.Validate(null);
        Assert.Single(errors);
        Assert.Equal("root", errors[0].Path);
    }

    [Fact]
    public void Validate_TargetsBothFieldsEmpty_AddsErrorForThatTarget()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig>
            {
                new("bad", null, null), // 两个都空
            },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("background", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path == "Targets[0]" &&
            e.Message.Contains("TitlePattern") && e.Message.Contains("ProcessName"));
    }

    [Fact]
    public void Validate_TargetsEmptyList_AddsTargetsError()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig>(),
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("background", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path == "Targets" &&
            e.Message.Contains("at least one"));
    }

    [Fact]
    public void Validate_ThresholdAboveOne_AddsError()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(1.2),
            Input = new InputConfig("background", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path.Contains("DefaultThreshold") &&
            e.Message.Contains("0.5") && e.Message.Contains("1.0"));
    }

    [Fact]
    public void Validate_ThresholdAtBoundary_AddsError()
    {
        // 0.5 与 1.0 是开区间边界,均应拒绝
        var cfgAt05 = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.5),
            Input = new InputConfig("background", 50),
        };
        var cfgAt10 = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(1.0),
            Input = new InputConfig("background", 50),
        };

        Assert.NotEmpty(ConfigValidator.Validate(cfgAt05));
        Assert.NotEmpty(ConfigValidator.Validate(cfgAt10));
    }

    [Fact]
    public void Validate_DriverForeground_AddsNotImplementedError()
    {
        // RJ-S1-05 已修复:SupportedDrivers 现含 "foreground",else-if 分支可达,
        // 消息严格化为技术设计 §2.3 要求的"foreground driver not implemented in M0; use 'background'"。
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("foreground", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path.Contains("Driver") &&
            e.Message.Contains("not implemented") && e.Message.Contains("M0"));
    }

    [Fact]
    public void Validate_DriverUnknown_AddsDriverError()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("banana", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path.Contains("Driver") &&
            e.Message.Contains("background") && e.Message.Contains("foreground"));
    }

    [Fact]
    public void Validate_DriverCaseInsensitive_AcceptsBackgroundUppercase()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("BACKGROUND", 50),
        };

        Assert.Empty(ConfigValidator.Validate(cfg));
    }

    [Fact]
    public void Validate_ProfileEmpty_AddsError()
    {
        var cfg = new DevConfig
        {
            Profile = "   ",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("background", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path == "Profile");
    }

    [Fact]
    public void Validate_PostClickDelayNegative_AddsError()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("background", -5),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path.Contains("PostClickDelayMs"));
    }

    [Fact]
    public void Validate_MultipleProblems_AggregatesAllErrors()
    {
        // 同时多个字段非法 — 应聚合输出所有错误
        var cfg = new DevConfig
        {
            Profile = "",
            Targets = new List<WindowTargetConfig> { new("bad", null, null) },
            Matching = new MatchingConfig(1.5),
            Input = new InputConfig("foreground", -1),
            Paths = new PathConfig("", ""),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.True(errors.Count >= 5,
            $"应聚合多个错误,实际 {errors.Count}: [{string.Join("; ", errors.Select(e => e.Path))}]");
        Assert.Contains(errors, e => e.Path == "Profile");
        Assert.Contains(errors, e => e.Path == "Targets[0]");
        Assert.Contains(errors, e => e.Path.Contains("DefaultThreshold"));
        Assert.Contains(errors, e => e.Path.Contains("Driver"));
        Assert.Contains(errors, e => e.Path.Contains("PostClickDelayMs"));
    }
}
