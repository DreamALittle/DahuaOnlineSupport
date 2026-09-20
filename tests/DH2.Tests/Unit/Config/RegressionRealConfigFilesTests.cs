// DH2.Tests — RJ-S2-01 回归 UT
//
// 目的:防 S1/S2 同类缺陷(DH2.Core.Config.* 仍为 init-only / 位置 record)再次发生。
// 关键约束(架构师指令 RJ-S2-01):
//   - 必须直接读取仓库真实文件 `configs/dev.yaml` 与 `configs/mock-layout.yaml`,
//     不得用内联 YAML 字符串替代。
//   - 任何 DevConfig / MockLayoutConfig 反序列化失败 → 测试失败(直接报 RJ 复发)。
//
// 路径解析:从测试 bin 目录向上遍历,直到找到 `DH2.slnx` 即视为仓库根。
// 这样不依赖硬编码绝对路径,也不依赖 TFM 层级数。

using DH2.Core.Config;
using Xunit;

namespace DH2.Tests.Unit.Config;

public class RegressionRealConfigFilesTests
{
    private const string SlnxMarker = "DH2.slnx";
    private const string DevConfigRelPath = "configs/dev.yaml";
    private const string MockLayoutRelPath = "configs/mock-layout.yaml";

    /// <summary>定位仓库根目录(向上查找 DH2.slnx)。</summary>
    private static string LocateRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, SlnxMarker)))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"Repository root not found: walked up from '{AppContext.BaseDirectory}' without finding '{SlnxMarker}'.");
    }

    [Fact]
    public void Parse_RealDevYaml_ReturnsAllKeyFields()
    {
        // Arrange
        var repoRoot = LocateRepoRoot();
        var devPath = Path.Combine(repoRoot, DevConfigRelPath);
        Assert.True(File.Exists(devPath), $"real dev config must exist: {devPath}");

        // Act
        var cfg = ConfigLoader.Load(devPath);

        // Assert:关键字段(覆盖 Profile / Targets / Paths / Matching / Input 全维度)
        Assert.Equal("mock_800x600", cfg.Profile);
        Assert.NotEmpty(cfg.Targets);
        Assert.Equal("mock", cfg.Targets[0].Name);
        Assert.Equal(@"^DH2\.MockGame$", cfg.Targets[0].TitlePattern);
        Assert.Null(cfg.Targets[0].ProcessName);

        Assert.Equal("templates", cfg.Paths.Templates);
        Assert.Equal("artifacts", cfg.Paths.Artifacts);

        Assert.Equal(0.85, cfg.Matching.DefaultThreshold);

        Assert.Equal("background", cfg.Input.Driver);
        Assert.Equal(50, cfg.Input.PostClickDelayMs);

        // 校验器也应通过(无错误)
        var errors = ConfigValidator.Validate(cfg);
        Assert.Empty(errors);
    }

    [Fact]
    public void Parse_RealMockLayoutYaml_ReturnsAllKeyFields()
    {
        // Arrange
        var repoRoot = LocateRepoRoot();
        var layoutPath = Path.Combine(repoRoot, MockLayoutRelPath);
        Assert.True(File.Exists(layoutPath), $"real mock-layout config must exist: {layoutPath}");

        // Act
        var layout = MockLayoutLoader.Load(layoutPath);

        // Assert:覆盖 Window / Taskbar / Status / Button / StateTimings 全维度,
        // 与 mock-layout.yaml 真实字段对齐(S1-Dev B 版本)。
        Assert.Equal("%TEMP%/dh2-mockgame", layout.TempDir);

        Assert.Equal("DH2.MockGame", layout.Window.Title);
        Assert.Equal(800, layout.Window.Width);
        Assert.Equal(600, layout.Window.Height);

        // 任务追踪栏
        Assert.Equal(16, layout.Taskbar.X);
        Assert.Equal(16, layout.Taskbar.Y);
        Assert.Equal(320, layout.Taskbar.Width);
        Assert.Equal(88, layout.Taskbar.Height);
        Assert.Equal("#1E1E2E", layout.Taskbar.Background);
        Assert.Equal("#555555", layout.Taskbar.BorderColor);
        Assert.Equal(2, layout.Taskbar.BorderThickness);
        Assert.Equal("#FFFFFF", layout.Taskbar.TextColor);
        Assert.Equal(20, layout.Taskbar.FontSize);
        Assert.Equal("师门任务 ({n}/20)", layout.Taskbar.TextTemplate);
        Assert.Equal((176, 60), layout.Taskbar.Center);

        // 状态文本
        Assert.Equal(16, layout.Status.X);
        Assert.Equal(116, layout.Status.Y);
        Assert.Equal(18, layout.Status.FontSize);
        Assert.Equal("待机", layout.Status.Texts.Idle);
        Assert.Equal("寻路中...", layout.Status.Texts.Pathfinding);
        Assert.Equal("已到达目的地", layout.Status.Texts.Arrived);

        // 主按钮
        Assert.Equal(16, layout.Button.X);
        Assert.Equal(180, layout.Button.Y);
        Assert.Equal(140, layout.Button.Width);
        Assert.Equal(48, layout.Button.Height);
        Assert.Equal("#2D5BFF", layout.Button.Background);
        Assert.Equal("前往", layout.Button.Texts.Go);
        Assert.Equal("返回", layout.Button.Texts.Return);
        Assert.Equal((86, 204), layout.Button.Center);

        // 状态机计时
        Assert.Equal(2000, layout.StateTimings.PathfindingMs);
    }
}
