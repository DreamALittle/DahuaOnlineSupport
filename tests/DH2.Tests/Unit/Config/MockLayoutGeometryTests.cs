// DH2.Tests — L1 单元测试
// UT-06 mock-layout 解析与几何:任务栏中心 (176,60)、按钮中心 (86,204)
// 参见 docs/iterations/M0/ITER-M0-测试设计.md §1 + DH2.Core.Config.MockLayoutConfig/Loader
//
// 历史:DEF-S1-01 上报 MockLayoutConfig 是 sealed record 无 parameterless ctor,
// YamlDotNet 无法实例化。RJ-S1-03 已修复(record → class),本测试恢复真实 YAML 解析期望。

using DH2.Core.Config;
using Xunit;

namespace DH2.Tests.Unit.Config;

public class MockLayoutGeometryTests
{
    private const string CanonicalYaml = """
        tempDir: "%TEMP%/dh2-mockgame"
        window:
          title: "DH2.MockGame"
          width: 800
          height: 600
        taskbar:
          x: 16
          y: 16
          width: 320
          height: 88
        status:
          x: 16
          y: 116
        button:
          x: 16
          y: 180
          width: 140
          height: 48
        stateTimings:
          pathfindingMs: 2000
        """;

    // ───── RJ-S1-03 修复后:真实 YAML 解析可工作 ─────

    [Fact]
    public void Parse_ValidYaml_ReturnsAllSections()
    {
        // 严格期望:RJ-S1-03 修复后,MockLayoutConfig 已改为带无参构造的 class,YamlDotNet 可实例化
        var cfg = MockLayoutLoader.Parse(CanonicalYaml);

        Assert.NotNull(cfg);
        Assert.Equal(800, cfg.Window.Width);
        Assert.Equal(600, cfg.Window.Height);
        Assert.Equal("DH2.MockGame", cfg.Window.Title);
        Assert.Equal(16, cfg.Taskbar.X);
        Assert.Equal(16, cfg.Taskbar.Y);
        Assert.Equal(320, cfg.Taskbar.Width);
        Assert.Equal(88, cfg.Taskbar.Height);
        Assert.Equal(16, cfg.Button.X);
        Assert.Equal(180, cfg.Button.Y);
        Assert.Equal(140, cfg.Button.Width);
        Assert.Equal(48, cfg.Button.Height);
        Assert.Equal(2000, cfg.StateTimings.PathfindingMs);
        Assert.Equal("%TEMP%/dh2-mockgame", cfg.TempDir);
    }

    [Fact]
    public void Parse_MinimalYaml_AppliesAllDefaults()
    {
        // 空对象 — YamlDotNet 应返回带所有默认值的实例
        var cfg = MockLayoutLoader.Parse("{}");
        Assert.NotNull(cfg);
        Assert.Equal(800, cfg.Window.Width);
        Assert.Equal(600, cfg.Window.Height);
        Assert.Equal("DH2.MockGame", cfg.Window.Title);
        Assert.Equal(16, cfg.Taskbar.X);
        Assert.Equal(320, cfg.Taskbar.Width);
        Assert.Equal(88, cfg.Taskbar.Height);
        Assert.Equal(16, cfg.Button.X);
        Assert.Equal(180, cfg.Button.Y);
        Assert.Equal(140, cfg.Button.Width);
        Assert.Equal(48, cfg.Button.Height);
        Assert.Equal(2000, cfg.StateTimings.PathfindingMs);
    }

    [Fact]
    public void Parse_NullYaml_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() => MockLayoutLoader.Parse(null));
    }

    [Fact]
    public void Parse_EmptyString_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() => MockLayoutLoader.Parse(""));
    }

    [Fact]
    public void Parse_UnknownProperty_IsIgnored()
    {
        // 配置演进的容错性:IgnoreUnmatchedProperties 已启用
        var yaml = """
            tempDir: "."
            unknownField: "should be ignored"
            window:
              width: 1024
              height: 768
              brandNewField: 42
            """;
        var cfg = MockLayoutLoader.Parse(yaml);
        Assert.Equal(1024, cfg.Window.Width);
        Assert.Equal(768, cfg.Window.Height);
    }

    [Fact]
    public void Load_FromExistingRepoFile_ReturnsConfig()
    {
        // 加载仓库内真实 configs/mock-layout.yaml
        var repoLayout = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "configs", "mock-layout.yaml");
        if (!File.Exists(repoLayout))
        {
            // 工作目录可能在测试输出路径,跳过
            return;
        }

        var cfg = MockLayoutLoader.Load(repoLayout);
        Assert.NotNull(cfg);
        Assert.Equal(800, cfg.Window.Width);
        Assert.Equal(600, cfg.Window.Height);
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => MockLayoutLoader.Load(@"C:\nonexistent\layout.yaml"));
    }

    // ───── 几何中心断言(测试设计 §1 UT-06 真值) ─────

    [Fact]
    public void TaskbarCenter_FromCanonicalYaml_Is176By60()
    {
        // (16 + 320/2, 16 + 88/2) = (176, 60)
        var cfg = MockLayoutLoader.Parse(CanonicalYaml);
        var center = (cfg.Taskbar.X + cfg.Taskbar.Width / 2, cfg.Taskbar.Y + cfg.Taskbar.Height / 2);
        Assert.Equal((176, 60), center);
    }

    [Fact]
    public void ButtonCenter_FromCanonicalYaml_Is86By204()
    {
        // (16 + 140/2, 180 + 48/2) = (86, 204)
        var cfg = MockLayoutLoader.Parse(CanonicalYaml);
        var center = (cfg.Button.X + cfg.Button.Width / 2, cfg.Button.Y + cfg.Button.Height / 2);
        Assert.Equal((86, 204), center);
    }

    [Fact]
    public void MockRect_CenterProperty_LargerRect()
    {
        var r = new MockRect(10, 20, 100, 50);
        Assert.Equal((60, 45), r.Center); // (10+50, 20+25)
    }
}
