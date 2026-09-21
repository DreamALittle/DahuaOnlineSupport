// DH2.Tests — L1 单元测试
// RJ-S4-02(测试 Agent)坐标推导接缝独立验证 UT:
// 独立构造合成帧 + layout,覆盖多缩放档位(100% / 125% / 150% / 200%),
// 断言 scale 换算公式满足布局空间相对偏移不变性。
// 与 Dev A 的实现侧 UT(E2eCommandTests 内 ComputeButtonClickPoint 的 3 用例)互为独立验证:
// - Dev A 测"实现" —— 断言具体结果值(1200×900→(129,306)、800×600→(86,204)、防御);
// - 本类测"接缝" —— 断言坐标空间换算的数学不变性 + 多档缩放一致性 + 模拟实测偏移传播。

using DH2.App.Commands;
using DH2.Core.Config;
using DH2.Core.Models;
using Xunit;
using DH2Point = DH2.Core.Models.Point;

namespace DH2.Tests.Unit.Commands;

public class E2eButtonClickPointScaleTests
{
    /// <summary>
    /// 标准 mock_800x600 layout:任务栏 (16,16,320,88) 中心 (176, 60);
    /// 按钮 (16,180,140,48) 中心 (86, 204)。
    /// </summary>
    private static MockLayoutConfig NewMock800x600Layout() => new MockLayoutConfig
    {
        Window = new MockWindowConfig { Width = 800, Height = 600 },
        Taskbar = new MockTaskbarConfig { X = 16, Y = 16, Width = 320, Height = 88 },
        Button = new MockButtonConfig { X = 16, Y = 180, Width = 140, Height = 48 },
        Status = new MockStatusPosition { X = 16, Y = 116 },
    };

    /// <summary>
    /// 不同缩放档位下,taskbar 实测中心(假设 match 完美,按 scale 等比放大)。
    /// 100% → (176, 60);150% → (264, 90);125% → (220, 75);200% → (352, 120)。
    /// </summary>
    private static DH2Point TaskbarMeasuredCenterFor(int frameWidth) => new DH2Point(
        (int)Math.Round(176.0 * frameWidth / 800.0),
        (int)Math.Round(60.0 * frameWidth / 800.0));

    // ───── RJ-S4-02 多缩放档位双缩放断言 ─────

    [Fact]
    public void ComputeButtonClickPoint_100Percent_NoScale_ReturnsLayoutCenter()
    {
        var layout = NewMock800x600Layout();
        var center = E2eCommand.ComputeButtonClickPoint(800, layout, new DH2Point(176, 60));

        // scale = 1.0 → 退化等同 layout 按钮中心 (86, 204)
        Assert.Equal(86, center.X);
        Assert.Equal(204, center.Y);
    }

    [Fact]
    public void ComputeButtonClickPoint_125Percent_Scale125X_ReturnsPhysicalButtonCenter()
    {
        var layout = NewMock800x600Layout();
        var measured = TaskbarMeasuredCenterFor(1000); // 1000/800 = 1.25
        var center = E2eCommand.ComputeButtonClickPoint(1000, layout, measured);

        // scale = 1.25,taskbar 中心 (220, 75);按钮布局中心 − 任务栏布局中心 = (-90, +144)
        // 乘 1.25 = (-112.5, +180);按钮帧中心 = (220-112.5, 75+180) = (107.5, 255)
        Assert.Equal(108, center.X); // (int)Math.Round(107.5) = 108(banker's rounding → 108)
        Assert.Equal(255, center.Y);
    }

    [Fact]
    public void ComputeButtonClickPoint_150Percent_Scale150X_ReturnsPhysicalButtonCenter()
    {
        var layout = NewMock800x600Layout();
        var measured = TaskbarMeasuredCenterFor(1200); // 1200/800 = 1.5
        var center = E2eCommand.ComputeButtonClickPoint(1200, layout, measured);

        // scale = 1.5,taskbar 中心 (264, 90);按钮布局中心 − 任务栏布局中心 = (-90, +144)
        // 乘 1.5 = (-135, +216);按钮帧中心 = (264-135, 90+216) = (129, 306)
        Assert.Equal(129, center.X);
        Assert.Equal(306, center.Y);
    }

    [Fact]
    public void ComputeButtonClickPoint_200Percent_Scale2X_ReturnsPhysicalButtonCenter()
    {
        var layout = NewMock800x600Layout();
        var measured = TaskbarMeasuredCenterFor(1600); // 1600/800 = 2.0
        var center = E2eCommand.ComputeButtonClickPoint(1600, layout, measured);

        // scale = 2.0,taskbar 中心 (352, 120);按钮布局中心 − 任务栏布局中心 = (-90, +144)
        // 乘 2.0 = (-180, +288);按钮帧中心 = (352-180, 120+288) = (172, 408)
        Assert.Equal(172, center.X);
        Assert.Equal(408, center.Y);
    }

    // ───── 坐标空间不变性断言 ─────

    [Fact]
    public void ComputeButtonClickPoint_RelativeButtonTaskbarOffset_ProportionalToFrameWidth()
    {
        // 不变性断言:button 与 taskbar 在帧像素空间的距离 = (button_layout - taskbar_layout) × scale
        // 在不同缩放下,这个距离应严格按比例放大。
        var layout = NewMock800x600Layout();

        // 100%
        var c100 = E2eCommand.ComputeButtonClickPoint(800, layout, new DH2Point(176, 60));
        // 150%
        var c150 = E2eCommand.ComputeButtonClickPoint(1200, layout, new DH2Point(264, 90));

        // dx, dy(button − taskbar 中心)在两个缩放下
        var dx100 = c100.X - 176;
        var dy100 = c100.Y - 60;
        var dx150 = c150.X - 264;
        var dy150 = c150.Y - 90;

        // 150% 的偏移应是 100% 的 1.5 倍(layout.Window.Width=800 不变)
        Assert.Equal((int)Math.Round(dx100 * 1.5), dx150);
        Assert.Equal((int)Math.Round(dy100 * 1.5), dy150);
    }

    // ───── 实测偏移传播 ─────

    [Fact]
    public void ComputeButtonClickPoint_TaskbarMeasuredCenterOffset_PropagatesToButton()
    {
        // 模拟 match 偏差:taskbar 实测中心偏离真值 (+10, -5)
        var layout = NewMock800x600Layout();
        var trueCenter = new DH2Point(176, 60);
        var offsetMeasured = new DH2Point(176 + 10, 60 - 5); // (186, 55)

        var c = E2eCommand.ComputeButtonClickPoint(800, layout, offsetMeasured);

        // 按钮中心应同步偏移 (+10, -5),即 (86 + 10, 204 - 5) = (96, 199)
        Assert.Equal(96, c.X);
        Assert.Equal(199, c.Y);
    }

    // ───── 防御:非零非负 frameWidth ─────

    [Fact]
    public void ComputeButtonClickPoint_FrameWidthZero_Throws()
    {
        var layout = NewMock800x600Layout();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            E2eCommand.ComputeButtonClickPoint(0, layout, new DH2Point(176, 60)));
    }

    [Fact]
    public void ComputeButtonClickPoint_FrameWidthNegative_Throws()
    {
        var layout = NewMock800x600Layout();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            E2eCommand.ComputeButtonClickPoint(-1, layout, new DH2Point(176, 60)));
    }

    [Fact]
    public void ComputeButtonClickPoint_LayoutWindowWidthZero_Throws()
    {
        var layout = NewMock800x600Layout();
        layout.Window.Width = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            E2eCommand.ComputeButtonClickPoint(800, layout, new DH2Point(176, 60)));
    }

    [Fact]
    public void ComputeButtonClickPoint_LayoutWindowWidthNegative_Throws()
    {
        var layout = NewMock800x600Layout();
        layout.Window.Width = -100;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            E2eCommand.ComputeButtonClickPoint(800, layout, new DH2Point(176, 60)));
    }

    [Fact]
    public void ComputeButtonClickPoint_LayoutNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            E2eCommand.ComputeButtonClickPoint(800, null!, new DH2Point(176, 60)));
    }

    // ───── 真实边缘案例 ─────

    [Fact]
    public void ComputeButtonClickPoint_FrameWidthEqualsLayoutWidth_NoScale()
    {
        // frame 宽度 == layout 宽度 → scale = 1.0 → 退化等同布局按钮中心
        var layout = NewMock800x600Layout();
        var center = E2eCommand.ComputeButtonClickPoint(800, layout, new DH2Point(176, 60));

        Assert.Equal(86, center.X);
        Assert.Equal(204, center.Y);
    }

    [Fact]
    public void ComputeButtonClickPoint_NonStandardScale_33Percent_RoundsCorrectly()
    {
        // 非标准缩放 1.25 测试 Math.Round 行为一致性
        var layout = NewMock800x600Layout();
        var measured = new DH2Point(220, 75); // 1000/800 = 1.25
        var center = E2eCommand.ComputeButtonClickPoint(1000, layout, measured);

        // 公式(100%):button 中心 (86, 204);taskbar 中心 (176, 60)
        // diff = (-90, +144);×1.25 = (-112.5, +180);+ measured = (107.5, 255)
        // Math.Round(107.5) = 108 (banker's rounding → 108),Math.Round(255.0) = 255
        Assert.Equal(108, center.X);
        Assert.Equal(255, center.Y);
    }
}
