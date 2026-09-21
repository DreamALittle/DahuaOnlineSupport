// DH2.Tests — L1 单元测试
// UT-07 金样本静态识别(测试设计 §1):
// mock_taskbar 模板 + 测试"金样本" → Found=true + 中心误差 ≤2px
// 实际金样本由 S3-4 资产生成产生(需 GUI);本轮在 headless 用合成"金样本"等价场景:
// - 合成 800×600 帧,严格按 mock-layout 在 (16,16,320,88) 嵌入"任务栏"暗色矩形 + 中央亮色
// - 用合成模板(嵌入区域的精确灰度子图)匹配
// - 断言 MatchResult.Center 误差 ≤2px(对齐 S3 SAC3-2 IT-03 ≤2px 契约)
// - ROI 中心 ±2px 同样验证

using System.Text;
using DH2.Core.Models;
using DH2.Vision;
using OpenCvSharp;
using Xunit;
using DH2Rect = DH2.Core.Models.Rect;

namespace DH2.Tests.Unit.Vision;

public class TemplateMatcherGoldenSampleTests : IDisposable
{
    private readonly List<string> _tempDirs = new();

    private string BuildProfileWithTemplates(string profile, params (string key, Mat template)[] entries)
    {
        var root = Path.Combine(Path.GetTempPath(), $"dh2-golden-{Guid.NewGuid():N}");
        var profileDir = Path.Combine(root, profile);
        Directory.CreateDirectory(profileDir);
        Directory.CreateDirectory(Path.Combine(profileDir, "png"));

        var sb = new StringBuilder();
        sb.AppendLine($"profile: {profile}");
        sb.AppendLine("templates:");
        foreach (var (key, _) in entries)
        {
            sb.AppendLine($"  - key: {key}");
            sb.AppendLine($"    file: png/{key}.png");
            sb.AppendLine($"    threshold: 0.85");
            sb.AppendLine($"    clickOffset: {{ x: 0, y: 0 }}");
        }
        File.WriteAllText(Path.Combine(profileDir, "manifest.yaml"), sb.ToString(), Encoding.UTF8);
        foreach (var (key, template) in entries)
        {
            Cv2.ImWrite(Path.Combine(profileDir, $"png/{key}.png"), template);
            template.Dispose();
        }
        _tempDirs.Add(root);
        return root;
    }

    /// <summary>模拟 mock-taskbar:320×88 灰度图,四周暗 (#1E1E2E 类似) + 居中略亮带(模拟任务栏文字)。</summary>
    private static Mat MakeMockTaskbarTemplate()
    {
        var mat = new Mat(88, 320, MatType.CV_8UC1, Scalar.All(30)); // 暗底
        // 居中文字区(亮)
        var textRegion = new OpenCvSharp.Rect(20, 30, 280, 28);
        Cv2.Rectangle(mat, textRegion, Scalar.All(200), thickness: -1);
        return mat;
    }

    /// <summary>模拟 mock-btn-go:140×48 蓝底白字按钮。</summary>
    private static Mat MakeMockBtnTemplate()
    {
        var mat = new Mat(48, 140, MatType.CV_8UC1, Scalar.All(91)); // 蓝底 #2D5BFF ≈ (91,91,255) 灰度近似
        // 居中文字亮带
        var textRegion = new OpenCvSharp.Rect(30, 16, 80, 16);
        Cv2.Rectangle(mat, textRegion, Scalar.All(240), thickness: -1);
        return mat;
    }

    /// <summary>
    /// 800×600 模拟 MockGame 帧(对应 mock_800x600 profile):
    /// - 任务栏 (16, 16, 320, 88) ← mock_layout.yaml 真值
    /// - 按钮 (16, 180, 140, 48) ← mock_layout.yaml 真值
    /// 嵌入"金样本"等价内容(灰度:任务栏暗底 + 文字亮带;按钮蓝底 + 文字亮带)
    /// </summary>
    private static Mat MakeMockIdleFrame()
    {
        var frame = new Mat(600, 800, MatType.CV_8UC3, new Scalar(50, 50, 50));
        // 任务栏
        var taskbarRect = new OpenCvSharp.Rect(16, 16, 320, 88);
        Cv2.Rectangle(frame, taskbarRect, new Scalar(30, 30, 30), thickness: -1);
        Cv2.Rectangle(frame, new OpenCvSharp.Rect(16 + 20, 16 + 30, 280, 28),
            new Scalar(200, 200, 200), thickness: -1);
        // 按钮(蓝底)
        var btnRect = new OpenCvSharp.Rect(16, 180, 140, 48);
        Cv2.Rectangle(frame, btnRect, new Scalar(255, 91, 91), thickness: -1);
        Cv2.Rectangle(frame, new OpenCvSharp.Rect(16 + 30, 180 + 16, 80, 16),
            new Scalar(240, 240, 240), thickness: -1);
        return frame;
    }

    public void Dispose()
    {
        foreach (var d in _tempDirs)
        {
            try { Directory.Delete(d, recursive: true); } catch { /* best effort */ }
        }
    }

    // ───── UT-07 金样本静态识别:中心误差 ≤2px ─────

    [Fact]
    public void Match_Taskbar_Found_And_CenterWithin2px()
    {
        // 模板与帧的"金样本"等价:严格几何一致 + 像素一致 → score 应 ≈ 1.0,Location 精确
        var template = MakeMockTaskbarTemplate();
        var root = BuildProfileWithTemplates("mock_800x600", ("mock_taskbar", template));
        var frame = MakeMockIdleFrame();

        using var store = new TemplateStore(root, "mock_800x600", defaultThreshold: 0.85);
        using var matcher = new TemplateMatcher(store);

        var result = matcher.Match(frame, "mock_taskbar");

        Assert.True(result.Found, $"match should succeed (score={result.Score:F3})");
        // mock-layout 真值中心 (16+320/2, 16+88/2) = (176, 60)
        Assert.InRange(result.Center.X, 174, 178); // ±2px 容忍
        Assert.InRange(result.Center.Y, 58, 62);
        Assert.True(result.Score >= 0.85, $"score {result.Score:F3} 应 ≥ 0.85");
        // 期望位置 = mock-layout 真值 (16, 16)
        Assert.Equal(16, result.Location.X);
        Assert.Equal(16, result.Location.Y);

        frame.Dispose();
    }

    [Fact]
    public void Match_Button_Found_And_CenterWithin2px()
    {
        var template = MakeMockBtnTemplate();
        var root = BuildProfileWithTemplates("mock_800x600", ("mock_btn_go", template));
        var frame = MakeMockIdleFrame();

        using var store = new TemplateStore(root, "mock_800x600", defaultThreshold: 0.85);
        using var matcher = new TemplateMatcher(store);

        var result = matcher.Match(frame, "mock_btn_go");

        Assert.True(result.Found);
        // mock-layout 按钮中心 (16+140/2, 180+48/2) = (86, 204)
        Assert.InRange(result.Center.X, 84, 88); // ±2px
        Assert.InRange(result.Center.Y, 202, 206);
        Assert.True(result.Score >= 0.85);
    }

    [Fact]
    public void Match_Taskbar_FoundAcrossMultipleInvocations_IsConsistent()
    {
        // 同一张帧多次匹配应得到稳定结果(不可变快照)
        var template = MakeMockTaskbarTemplate();
        var root = BuildProfileWithTemplates("mock_800x600", ("mock_taskbar", template));
        var frame = MakeMockIdleFrame();

        using var store = new TemplateStore(root, "mock_800x600", defaultThreshold: 0.85);
        using var matcher = new TemplateMatcher(store);

        var r1 = matcher.Match(frame, "mock_taskbar");
        var r2 = matcher.Match(frame, "mock_taskbar");

        Assert.Equal(r1.Found, r2.Found);
        Assert.Equal(r1.Center.X, r2.Center.X);
        Assert.Equal(r1.Center.Y, r2.Center.Y);
        Assert.Equal(r1.Score, r2.Score, 4);

        frame.Dispose();
    }

    [Fact]
    public void Match_TaskbarWithRoi_Found_And_CenterWithin2px()
    {
        // ROI 围绕任务栏 (0, 0, 400, 200) → 整帧坐标仍为 (176, 60)
        var template = MakeMockTaskbarTemplate();
        var root = BuildProfileWithTemplates("mock_800x600", ("mock_taskbar", template));
        var frame = MakeMockIdleFrame();

        using var store = new TemplateStore(root, "mock_800x600", defaultThreshold: 0.85);
        using var matcher = new TemplateMatcher(store);

        var result = matcher.Match(frame, "mock_taskbar", new DH2Rect(0, 0, 400, 200));

        Assert.True(result.Found);
        Assert.InRange(result.Center.X, 174, 178);
        Assert.InRange(result.Center.Y, 58, 62);

        frame.Dispose();
    }
}
