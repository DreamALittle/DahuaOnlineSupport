// DH2.Tests — L1 单元测试
// 覆盖补强:Models/Contracts 构造与简单 getter,以及 ConfigValidator null-section 分支,
// 用于把 DH2.Core 行覆盖率从 67.92% 推到 ≥70% 门槛(测试设计 §1)。
// 不属于 UT-01~08 主用例矩阵,但作为支撑测试存在。

using DH2.Core.Config;
using DH2.Core.Contracts;
using DH2.Core.Models;
using Xunit;
// 使用 DH2 自有类型,避免与 System.Drawing.Point/Size 冲突(ImplicitUsings 引入 System.Drawing)
using Point = DH2.Core.Models.Point;
using Size = DH2.Core.Models.Size;

namespace DH2.Tests.Unit.Models;

public class ModelAndContractSmokeTests
{
    // ───── Models 构造 + 简单 getter 覆盖 ─────

    [Fact]
    public void ActionResult_Constructor_StoresAllFields()
    {
        var r = new ActionResult(true, 3, "click succeeded");
        Assert.True(r.Success);
        Assert.Equal(3, r.Attempts);
        Assert.Equal("click succeeded", r.FailReason);
    }

    [Fact]
    public void MatchResult_Constructor_StoresAllFieldsAndComputesCenter()
    {
        var loc = new Point(10, 20);
        var size = new Size(100, 50);
        var m = new MatchResult(true, 0.92, loc, size);

        Assert.True(m.Found);
        Assert.Equal(0.92, m.Score);
        Assert.Equal(loc, m.Location);
        Assert.Equal(size, m.Size);
        // Center = Location + Size/2 = (10+50, 20+25) = (60, 45)
        Assert.Equal(new Point(60, 45), m.Center);
    }

    [Fact]
    public void Size_Constructor_StoresWidthAndHeight()
    {
        var s = new Size(800, 600);
        Assert.Equal(800, s.Width);
        Assert.Equal(600, s.Height);
    }

    [Fact]
    public void Win32Window_Constructor_StoresAllFields()
    {
        var rect = new Rect(100, 200, 800, 600);
        var w = new Win32Window(12345L, "DH2.MockGame", "DH2.MockGame", rect);

        Assert.Equal(12345L, w.Hwnd);
        Assert.Equal("DH2.MockGame", w.Title);
        Assert.Equal("DH2.MockGame", w.ProcessName);
        Assert.Equal(rect, w.Bounds);
    }

    [Fact]
    public void Rect_CenterProperty_ComputesFromWidthHeight()
    {
        var r = new Rect(10, 20, 100, 50);
        Assert.Equal(new Point(60, 45), r.Center);
    }

    [Fact]
    public void Rect_CenterProperty_AtOrigin()
    {
        var r = new Rect(0, 0, 100, 50);
        Assert.Equal(new Point(50, 25), r.Center);
    }

    // ───── Contracts 占位 record 构造 ─────

    [Fact]
    public void TemplateEntry_Constructor_StoresKeyAndFile()
    {
        // ClickOffset 为 Point(struct),Roi/Since 为 string,Image 用 null(类型为 OpenCvSharp.Mat,无参默认 ctor)
        var e = new TemplateEntry(
            Key: "mock_taskbar",
            File: "png/mock_taskbar.png",
            Threshold: 0.85,
            ClickOffset: new Point(0, 0),
            Roi: "taskbar",
            Since: "m0",
            Image: null!);

        Assert.Equal("mock_taskbar", e.Key);
        Assert.Equal("png/mock_taskbar.png", e.File);
        Assert.Equal(0.85, e.Threshold);
        Assert.Equal(new Point(0, 0), e.ClickOffset);
        Assert.Equal("taskbar", e.Roi);
        Assert.Equal("m0", e.Since);
    }

    // ───── ConfigValidator null-section 分支覆盖 ─────

    [Fact]
    public void Validate_MatchingSectionNull_AddsError()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = null!, // 强制 null
            Input = new InputConfig("background", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path == "Matching" && e.Message.Contains("required"));
    }

    [Fact]
    public void Validate_InputSectionNull_AddsError()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = null!,
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path == "Input" && e.Message.Contains("required"));
    }

    [Fact]
    public void Validate_PathsSectionNull_AddsError()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = null!,
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("background", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path == "Paths" && e.Message.Contains("required"));
    }

    [Fact]
    public void Validate_PathsTemplatesEmpty_AddsError()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("background", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path.Contains("Templates"));
    }

    [Fact]
    public void Validate_PathsArtifactsEmpty_AddsError()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { new("mock", "^x$", null) },
            Paths = new PathConfig("templates", "  "),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("background", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path.Contains("Artifacts"));
    }

    [Fact]
    public void Validate_TargetsListNull_AddsTargetsError()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = null!, // 强制 null
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("background", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path == "Targets" && e.Message.Contains("at least one"));
    }

    [Fact]
    public void Validate_TargetEntryNull_AddsErrorForIndex()
    {
        var cfg = new DevConfig
        {
            Profile = "mock_800x600",
            Targets = new List<WindowTargetConfig> { null! },
            Paths = new PathConfig("templates", "artifacts"),
            Matching = new MatchingConfig(0.85),
            Input = new InputConfig("background", 50),
        };

        var errors = ConfigValidator.Validate(cfg);
        Assert.Contains(errors, e => e.Path == "Targets[0]" && e.Message.Contains("null"));
    }
}
