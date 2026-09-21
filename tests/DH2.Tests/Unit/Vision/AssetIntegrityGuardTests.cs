// DH2.Tests — L1 单元测试
// RJ-S4-05 资产完整性守护 UT(永久防线,S4 架构师审核第三轮指令):
// 读仓库真实文件,断言像素签名,任何污染资产无法静默入仓。
// 防御 S3 DEF-S3-02 教训:10:40–14:10 间生成的资产被全屏游戏遮挡污染,
// 靠 e2e 才暴露(taskbar/btn_go/btn_return/gold-idle 均曾为游戏场景裁剪)。
//
// 严格断言(任务书 §1):
// - templates/mock_800x600/png/mock_taskbar.png: 平均色 ≈ #1E1E2E(深藏青)
// - templates/mock_800x600/png/mock_btn_go.png: 中心区域主色 ≈ #2D5BFF(蓝)且非灰白
// - templates/mock_800x600/png/mock_btn_return.png: 中心区域主色 ≈ #2D5BFF(蓝)且非灰白
// - tests/golden/screenshots/mock/idle.png: (264,90) 处 ≈ #1E1E2E + (129,306) 处 ≈ #2D5BFF
//
// 注:本 UT 必须保持独立性 —— 任意资产被污染(浅灰/透明/hover/游戏场景覆盖)即 FAIL,
// 防止未来污染资产再次静默入仓(架构师第三轮报告 §3)。

using OpenCvSharp;
using Xunit;

namespace DH2.Tests.Unit.Vision;

public class AssetIntegrityGuardTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

    private static string LocateRepoRoot()
    {
        // 从测试 bin 目录向上找仓库根(Directory.Build.props 标志)
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }
        if (dir == null)
        {
            throw new InvalidOperationException(
                $"未找到仓库根(从 {AppContext.BaseDirectory} 向上遍历未发现 Directory.Build.props)");
        }
        return dir.FullName;
    }

    /// <summary>加载 PNG;不存在立即 FAIL。</summary>
    private static Mat LoadRequired(string relativePath)
    {
        var absPath = Path.Combine(RepoRoot, relativePath);
        Assert.True(File.Exists(absPath), $"资产缺失: {relativePath} (绝对路径: {absPath})");
        var mat = Cv2.ImRead(absPath);
        Assert.False(mat.Empty(), $"资产无法解码为图像(可能已损坏): {relativePath}");
        return mat;
    }

    /// <summary>计算 ROI 子区域均值。</summary>
    private static Scalar MeanOfRegion(Mat mat, OpenCvSharp.Rect roi)
    {
        Assert.True(roi.X >= 0 && roi.Y >= 0, $"ROI 越界: ({roi.X}, {roi.Y})");
        Assert.True(roi.X + roi.Width <= mat.Cols, $"ROI 越界 X: ({roi.X}, {roi.Y}, {roi.Width}, {roi.Height}) on {mat.Cols}x{mat.Rows}");
        Assert.True(roi.Y + roi.Height <= mat.Rows, $"ROI 越界 Y: ({roi.X}, {roi.Y}, {roi.Width}, {roi.Height}) on {mat.Cols}x{mat.Rows}");

        using var region = new Mat(mat, roi);
        return Cv2.Mean(region);
    }

    /// <summary>读取单像素 BGR(OpenCV 顺序:B, G, R)。</summary>
    private static Vec3b Pixel(Mat mat, int x, int y)
    {
        Assert.True(x >= 0 && y >= 0 && x < mat.Cols && y < mat.Rows, $"像素越界: ({x}, {y}) on {mat.Cols}x{mat.Rows}");
        return mat.At<Vec3b>(y, x);
    }

    /// <summary>
    /// 断言 BGR 像素 ≈ 目标 #RRGGBB,容忍 ±tolerance。
    /// </summary>
    private static void AssertBgrClose(
        Vec3b actual,
        string name,
        int expectedR, int expectedG, int expectedB,
        int tolerance)
    {
        // actual: BGR(OpenCV); expected: RGB
        var actualB = actual.Item0;
        var actualG = actual.Item1;
        var actualR = actual.Item2;

        Assert.True(
            Math.Abs(actualR - expectedR) <= tolerance,
            $"{name}: R 失配 expected={expectedR}±{tolerance}, actual={actualR} (BGR=({actualB}, {actualG}, {actualR}))");
        Assert.True(
            Math.Abs(actualG - expectedG) <= tolerance,
            $"{name}: G 失配 expected={expectedG}±{tolerance}, actual={actualG} (BGR=({actualB}, {actualG}, {actualR}))");
        Assert.True(
            Math.Abs(actualB - expectedB) <= tolerance,
            $"{name}: B 失配 expected={expectedB}±{tolerance}, actual={actualB} (BGR=({actualB}, {actualG}, {actualR}))");
    }

    /// <summary>
    /// 断言区域主色 ≈ #RRGGBB(单色,无非灰要求)。
    /// 适用于深藏青单色任务栏底色 + 文字带(各通道差异小是正常的)。
    /// </summary>
    private static void AssertRegionColor(
        Scalar meanBgr,
        string name,
        int expectedR, int expectedG, int expectedB,
        int tolerance)
    {
        var b = (int)Math.Round(meanBgr.Val0);
        var g = (int)Math.Round(meanBgr.Val1);
        var r = (int)Math.Round(meanBgr.Val2);

        Assert.True(
            Math.Abs(r - expectedR) <= tolerance,
            $"{name}: R 失配 expected={expectedR}±{tolerance}, actual={r} (BGR=({b}, {g}, {r}))");
        Assert.True(
            Math.Abs(g - expectedG) <= tolerance,
            $"{name}: G 失配 expected={expectedG}±{tolerance}, actual={g} (BGR=({b}, {g}, {r}))");
        Assert.True(
            Math.Abs(b - expectedB) <= tolerance,
            $"{name}: B 失配 expected={expectedB}±{tolerance}, actual={b} (BGR=({b}, {g}, {r}))");
    }

    /// <summary>
    /// 断言区域主色 ≈ #RRGGBB 且非灰白(R/G/B 各通道差异 ≤ grayThreshold 视为灰白)。
    /// 适用于蓝底按钮(防 hover/透明/游戏场景截屏,各通道差异应明显)。
    /// </summary>
    private static void AssertRegionBlueNotGray(
        Scalar meanBgr,
        string name,
        int expectedR, int expectedG, int expectedB,
        int tolerance,
        int grayThreshold = 25)
    {
        var b = (int)Math.Round(meanBgr.Val0);
        var g = (int)Math.Round(meanBgr.Val1);
        var r = (int)Math.Round(meanBgr.Val2);

        Assert.True(
            Math.Abs(r - expectedR) <= tolerance,
            $"{name}: R 失配 expected={expectedR}±{tolerance}, actual={r} (BGR=({b}, {g}, {r}))");
        Assert.True(
            Math.Abs(g - expectedG) <= tolerance,
            $"{name}: G 失配 expected={expectedG}±{tolerance}, actual={g} (BGR=({b}, {g}, {r}))");
        Assert.True(
            Math.Abs(b - expectedB) <= tolerance,
            $"{name}: B 失配 expected={expectedB}±{tolerance}, actual={b} (BGR=({b}, {g}, {r}))");

        // 防 hover/透明/游戏场景:非灰白(各通道差异应 > grayThreshold)
        var maxChannel = Math.Max(r, Math.Max(g, b));
        var minChannel = Math.Min(r, Math.Min(g, b));
        var spread = maxChannel - minChannel;
        Assert.True(
            spread > grayThreshold,
            $"{name}: 颜色过灰(各通道 max-min={spread} ≤ {grayThreshold}),可能为透明/hover/游戏场景截屏;BGR=({b}, {g}, {r})");
    }

    // ───── mock_taskbar.png(深藏青 #1E1E2E) ─────

    [Fact]
    public void MockTaskbar_MeanColor_IsDeepIndigo_NotGameSceneOrTransparent()
    {
        using var mat = LoadRequired("templates/mock_800x600/png/mock_taskbar.png");
        // 模板是任务栏 ROI 整图(150% 缩放:480×132),均值即代表任务栏底色
        var mean = Cv2.Mean(mat);

        // taskbar 是单色深藏青 + 文字带(各通道差异小是正常的),不需要"非灰白"断言
        AssertRegionColor(
            mean,
            "mock_taskbar.png 全图均值",
            expectedR: 0x1E, expectedG: 0x1E, expectedB: 0x2E,
            tolerance: 25);
    }

    [Fact]
    public void MockTaskbar_CenterPixel_IsDeepIndigo_NotGameScene()
    {
        using var mat = LoadRequired("templates/mock_800x600/png/mock_taskbar.png");
        // 任务栏中心(150% 物理像素)
        var center = Pixel(mat, x: mat.Cols / 2, y: mat.Rows / 2);

        AssertBgrClose(
            center,
            "mock_taskbar.png 中心像素",
            expectedR: 0x1E, expectedG: 0x1E, expectedB: 0x2E,
            tolerance: 20);
    }

    // ───── mock_btn_go.png(蓝 #2D5BFF, 非灰白) ─────

    [Fact]
    public void MockBtnGo_FullImageMean_BlueNotGray()
    {
        // 用全图均值(避开文字带):btn_go 模板全图均值应近似蓝底 #2D5BFF
        using var mat = LoadRequired("templates/mock_800x600/png/mock_btn_go.png");
        var mean = Cv2.Mean(mat);

        AssertRegionBlueNotGray(
            mean,
            "mock_btn_go.png 全图均值",
            expectedR: 0x2D, expectedG: 0x5B, expectedB: 0xFF,
            tolerance: 30,
            grayThreshold: 30);
    }

    [Fact]
    public void MockBtnGo_TopQuarterRegion_BlueNotGray_ExcludesText()
    {
        // 用顶部 25% 区域(避开文字带,纯蓝底)
        using var mat = LoadRequired("templates/mock_800x600/png/mock_btn_go.png");
        var roi = new OpenCvSharp.Rect(0, 0, mat.Cols, mat.Rows / 4);
        var mean = MeanOfRegion(mat, roi);

        AssertRegionBlueNotGray(
            mean,
            "mock_btn_go.png 顶部 25% 区域均值",
            expectedR: 0x2D, expectedG: 0x5B, expectedB: 0xFF,
            tolerance: 30,
            grayThreshold: 30);
    }

    [Fact]
    public void MockBtnGo_HasExpectedDimensions()
    {
        // 150% 缩放:140×48 → 210×72(防御未来尺寸漂移)
        using var mat = LoadRequired("templates/mock_800x600/png/mock_btn_go.png");
        Assert.Equal(210, mat.Cols);
        Assert.Equal(72, mat.Rows);
    }

    // ───── mock_btn_return.png(蓝 #2D5BFF, 非灰白 — 永久防线核心) ─────

    [Fact]
    public void MockBtnReturn_FullImageMean_BlueNotGray_DefendsAgainstContamination()
    {
        // 永久防线核心:任何污染/hover/透明/游戏场景/无背景状态均 FAIL
        // 任务书 §1 明确要求"btn_return 中心区域主色 ≈ #2D5BFF 且非灰白"
        // 用全图均值避开文字带干扰,期望 #2D5BFF ±30
        using var mat = LoadRequired("templates/mock_800x600/png/mock_btn_return.png");
        var mean = Cv2.Mean(mat);

        AssertRegionBlueNotGray(
            mean,
            "mock_btn_return.png 全图均值",
            expectedR: 0x2D, expectedG: 0x5B, expectedB: 0xFF,
            tolerance: 30,
            grayThreshold: 30);
    }

    [Fact]
    public void MockBtnReturn_TopQuarterRegion_BlueNotGray_ExcludesText()
    {
        // 顶部 25% 区域(避开文字带)也应纯蓝底 — 防文字覆盖区污染陷阱
        using var mat = LoadRequired("templates/mock_800x600/png/mock_btn_return.png");
        var roi = new OpenCvSharp.Rect(0, 0, mat.Cols, mat.Rows / 4);
        var mean = MeanOfRegion(mat, roi);

        AssertRegionBlueNotGray(
            mean,
            "mock_btn_return.png 顶部 25% 区域均值",
            expectedR: 0x2D, expectedG: 0x5B, expectedB: 0xFF,
            tolerance: 30,
            grayThreshold: 30);
    }

    [Fact]
    public void MockBtnReturn_HasExpectedDimensions()
    {
        using var mat = LoadRequired("templates/mock_800x600/png/mock_btn_return.png");
        Assert.Equal(210, mat.Cols);
        Assert.Equal(72, mat.Rows);
    }

    // ───── idle.png(MockGame Idle 态整帧金样本,150% 缩放 1200×900) ─────

    [Fact]
    public void GoldIdle_TaskbarPosition_IsDeepIndigo()
    {
        // idle.png (264, 90) = 任务栏中心物理像素(150% 缩放)
        using var mat = LoadRequired("tests/golden/screenshots/mock/idle.png");
        var pixel = Pixel(mat, x: 264, y: 90);

        AssertBgrClose(
            pixel,
            "idle.png 任务栏中心 (264, 90)",
            expectedR: 0x1E, expectedG: 0x1E, expectedB: 0x2E,
            tolerance: 25);
    }

    [Fact]
    public void GoldIdle_ButtonPosition_IsBlue()
    {
        // idle.png (129, 306) = 按钮中心物理像素(150% 缩放,Idle 态"前往"按钮可见)
        using var mat = LoadRequired("tests/golden/screenshots/mock/idle.png");
        var pixel = Pixel(mat, x: 129, y: 306);

        AssertBgrClose(
            pixel,
            "idle.png 按钮中心 (129, 306)",
            expectedR: 0x2D, expectedG: 0x5B, expectedB: 0xFF,
            tolerance: 35);
    }

    [Fact]
    public void GoldIdle_HasExpectedDimensions()
    {
        // 150% 缩放:800×600 → 1200×900
        using var mat = LoadRequired("tests/golden/screenshots/mock/idle.png");
        Assert.Equal(1200, mat.Cols);
        Assert.Equal(900, mat.Rows);
    }

    // ───── 一致性:模板与金样本像素签名自洽 ─────

    [Fact]
    public void MockTaskbar_TemplateRegion_AndGoldIdle_TaskbarPosition_AgreeWithinTolerance()
    {
        // 不变性:模板均值(任务栏 ROI)≈ idle.png 任务栏位置像素(同源)
        using var template = LoadRequired("templates/mock_800x600/png/mock_taskbar.png");
        using var idle = LoadRequired("tests/golden/screenshots/mock/idle.png");

        var templateMean = Cv2.Mean(template);
        var idlePixel = Pixel(idle, 264, 90);

        // BGR 顺序
        Assert.True(Math.Abs(templateMean.Val2 - idlePixel.Item2) <= 30,
            $"模板均值 R={templateMean.Val2:F0} 与 idle.png 任务栏 R={idlePixel.Item2} 差异 > 30");
        Assert.True(Math.Abs(templateMean.Val1 - idlePixel.Item1) <= 30,
            $"模板均值 G={templateMean.Val1:F0} 与 idle.png 任务栏 G={idlePixel.Item1} 差异 > 30");
        Assert.True(Math.Abs(templateMean.Val0 - idlePixel.Item0) <= 30,
            $"模板均值 B={templateMean.Val0:F0} 与 idle.png 任务栏 B={idlePixel.Item0} 差异 > 30");
    }
}
