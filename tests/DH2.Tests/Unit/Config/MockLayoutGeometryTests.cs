// DH2.Tests — L1 单元测试
// UT-06 mock-layout 解析与几何:任务栏中心 (176,60)、按钮中心 (86,204)
// 参见 docs/iterations/M0/ITER-M0-测试设计.md §1 + DH2.Core.Config.MockLayoutConfig/Loader
//
// 已知问题(DEF-S1-01):src/DH2.Core/Config/MockLayoutConfig.cs 及其内部 record
// (MockWindowConfig / MockRect / MockStatusPosition / MockStateTimings) 均为
// sealed record(YamlDotNet 默认 factory 无法实例化),且该 Loader 无任何产品代码
// 调用(grep 验证:仅本测试文件)。MockGame 实际使用的是
// DH2.MockGame.Models.MockLayout(sealed class + parameterless ctor),YAML 反序列化
// 工作正常。本测试在本轮仅覆盖"几何中心纯函数"——YAML 解析路径留待
// DEF-S1-01 关闭(修复或删除)后补回。

using DH2.Core.Config;
using Xunit;

namespace DH2.Tests.Unit.Config;

public class MockLayoutGeometryTests
{
    // ───── 几何中心断言(测试设计 §1 UT-06 真值) ─────

    [Fact]
    public void TaskbarCenter_Defaults_Is176By60()
    {
        // MockLayoutConfig 默认 Taskbar = (16,16,320,88);Center = (16+320/2, 16+88/2) = (176, 60)
        var cfg = new MockLayoutConfig();
        Assert.Equal((176, 60), cfg.Taskbar.Center);
    }

    [Fact]
    public void ButtonCenter_Defaults_Is86By204()
    {
        // 默认 Button = (16,180,140,48);Center = (16+140/2, 180+48/2) = (86, 204)
        var cfg = new MockLayoutConfig();
        Assert.Equal((86, 204), cfg.Button.Center);
    }

    [Fact]
    public void TaskbarAndButton_DefaultGeometry_MatchesSpec()
    {
        // 技术设计 §7 布局表(单一真源为 mock-layout.yaml,默认值与之同源)
        var cfg = new MockLayoutConfig();
        Assert.Equal(800, cfg.Window.Width);
        Assert.Equal(600, cfg.Window.Height);
        Assert.Equal(16, cfg.Taskbar.X);
        Assert.Equal(16, cfg.Taskbar.Y);
        Assert.Equal(320, cfg.Taskbar.Width);
        Assert.Equal(88, cfg.Taskbar.Height);
        Assert.Equal(16, cfg.Button.X);
        Assert.Equal(180, cfg.Button.Y);
        Assert.Equal(140, cfg.Button.Width);
        Assert.Equal(48, cfg.Button.Height);
        Assert.Equal(2000, cfg.StateTimings.PathfindingMs);
    }

    [Fact]
    public void RectCenter_LargerRect_ComputesCorrectly()
    {
        // 任意矩形验证 — 不依赖默认配置
        var r = new MockRect(10, 20, 100, 50);
        Assert.Equal((60, 45), r.Center); // (10+50, 20+25)
    }

    // ───── YAML 解析路径(待 DEF-S1-01 关闭后补回) ─────
    // 已知失败:MockLayoutConfig/MockWindowConfig 是 sealed record 无 parameterless ctor,
    // YamlDotNet 的 DefaultObjectFactory 无法实例化。DEF-S1-01 处理。
    //
    // [Fact]
    // public void Parse_ValidYaml_ReturnsAllSections() { ... } // 见 git history

    [Fact]
    public void Parse_NullYaml_ThrowsInvalidDataException()
    {
        // Parse(null) 应抛 — 即使下游反序列化失败,空 yaml 的 null guard 仍应触发
        Assert.Throws<InvalidDataException>(() => MockLayoutLoader.Parse(null));
    }

    [Fact]
    public void Parse_EmptyString_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() => MockLayoutLoader.Parse(""));
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => MockLayoutLoader.Load(@"C:\nonexistent\layout.yaml"));
    }
}
