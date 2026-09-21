// DH2.Tests — L1 单元测试
// RJ-S3-02(测试 Agent / QA)接缝集成回归 UT(S3 架构师审核指令):
// 防御 save-template 写端与 TemplateStore 读端 schema 漂移(DEF-S3-01 类教训);
// 真实回环测试:SaveTemplateCommand 写临时目录 manifest + PNG →
// TemplateStore 从该文件加载 → Get("mock_taskbar") 返有效条目。
// 同时断言写出的 manifest 文本遵循技术设计 §5.1 camelCase schema(锁死 §5.1)。

using System.Text;
using DH2.App.Cli;
using DH2.App.Commands;
using DH2.Core.Config;
using DH2.Core.Contracts;
using DH2.Core.Models;
using DH2.Vision;
using OpenCvSharp;
using Xunit;

namespace DH2.Tests.Unit.Vision;

public class SaveTemplateStoreRoundtripTests : IDisposable
{
    private readonly List<string> _tempRoots = new();

    private string NewTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"dh2-roundtrip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        _tempRoots.Add(root);
        return root;
    }

    /// <summary>合成 800×600 BGR 帧(对应 mock_800x600 profile,MockGame 帧尺寸)。</summary>
    private static Mat MakeMockFrame()
    {
        // 不嵌入具体 mock-taskbar 内容;接缝测试不依赖模板视觉内容,
        // 仅校验"writer → reader" schema 一致性。
        return new Mat(600, 800, MatType.CV_8UC3, new Scalar(30, 30, 30));
    }

    private static DevConfig NewDevConfig(string templatesRoot, string profile = "mock_800x600")
    {
        return new DevConfig
        {
            Profile = profile,
            Paths = new PathConfig { Templates = templatesRoot, Artifacts = "artifacts" },
            Matching = new MatchingConfig { DefaultThreshold = 0.85 },
        };
    }

    /// <summary>Mock IFrameCapture:返同一张合成 Mat(独立 Clone,调用方 Dispose 互不影响)。</summary>
    private sealed class MockFrameCapture : IFrameCapture
    {
        private readonly Mat _frame;

        public MockFrameCapture(Mat frame)
        {
            _frame = frame;
        }

        public Frame Capture(long hwnd)
        {
            // 返 Clone(独立 Mat);SaveTemplateCommand 释放其 Image 时不影响 _frame
            return new Frame(hwnd, DateTime.UtcNow, _frame.Clone());
        }

        public void Dispose()
        {
            // 测试结束统一 dispose
        }
    }

    [Fact]
    public void SaveTemplate_WriteManifest_TemplateStoreCanReadBack()
    {
        // 接缝断言 1+2:RJ-S3-01 修复后(write 端用 camelCase),save→match 全链路贯通。
        var tempRoot = NewTempRoot();
        using var frame = MakeMockFrame();
        var mock = new MockFrameCapture(frame);
        var cmd = new SaveTemplateCommand(mock);

        var config = NewDevConfig(tempRoot);
        var options = new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["key"] = "mock_taskbar",
            ["x"] = "16",
            ["y"] = "16",
            ["w"] = "320",
            ["h"] = "88",
        };

        var exit = cmd.Execute(config, options, CancellationToken.None);

        // 1. 命令执行成功
        Assert.Equal((int)ExitCode.Success, exit);

        // 2. 写出的 manifest.yaml 存在 + 文本遵循 §5.1 camelCase schema(锁死 §5.1)
        var manifestPath = Path.Combine(tempRoot, "mock_800x600", "manifest.yaml");
        Assert.True(File.Exists(manifestPath), $"manifest.yaml 应已写入: {manifestPath}");
        var text = File.ReadAllText(manifestPath);
        Assert.StartsWith("profile:", text.TrimStart());
        Assert.Contains("- key: mock_taskbar", text);
        Assert.Contains("file: png/mock_taskbar.png", text);
        Assert.Contains("threshold: 0.85", text);
        Assert.Contains("clickOffset:", text);
        Assert.Contains("x: 0", text);
        Assert.Contains("y: 0", text);
        Assert.Contains("roi:", text);
        Assert.Contains("since:", text);
        // 严禁 PascalCase(防御 RJ-S3-01 DEF-S3-01 类教训)
        Assert.DoesNotContain("Profile:", text);
        Assert.DoesNotContain("- Key:", text);
        Assert.DoesNotContain("Threshold:", text);
        Assert.DoesNotContain("ClickOffset:", text);

        // 3. PNG 文件存在 + 尺寸 320×88(ROI 裁剪落盘正确)
        var pngPath = Path.Combine(tempRoot, "mock_800x600", "png", "mock_taskbar.png");
        Assert.True(File.Exists(pngPath), $"模板 PNG 应已写入: {pngPath}");

        // 4. 接缝核心:TemplateStore 从该 manifest 加载 → Get("mock_taskbar") 返有效条目
        using (var store = new TemplateStore(tempRoot, "mock_800x600", 0.85))
        {
            var entry = store.Get("mock_taskbar");
            Assert.Equal("mock_taskbar", entry.Key);
            Assert.Equal("png/mock_taskbar.png", entry.File);
            Assert.Equal(0.85, entry.Threshold);
            Assert.NotNull(entry.Image);
            Assert.Equal(320, entry.Image!.Cols);
            Assert.Equal(88, entry.Image!.Rows);
        }
    }

    [Fact]
    public void SaveTemplate_Reload_PicksUpNewEntryInSameStoreLifetime()
    {
        // 接缝断言 3:writer 写完 + TemplateStore 已实例化(本测试跳过本步),
        // 创建新 TemplateStore 模拟"程序二次启动" → 新 store 可加载该 manifest
        // (RJ-S3-01 + Reload 路径已对齐)。
        var tempRoot = NewTempRoot();
        using var frame = MakeMockFrame();
        var mock = new MockFrameCapture(frame);
        var cmd = new SaveTemplateCommand(mock);

        var config = NewDevConfig(tempRoot);
        var options = new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["key"] = "mock_btn_go",
            ["x"] = "16",
            ["y"] = "180",
            ["w"] = "140",
            ["h"] = "48",
        };

        var exit = cmd.Execute(config, options, CancellationToken.None);
        Assert.Equal((int)ExitCode.Success, exit);

        // 创建全新的 TemplateStore 实例(模拟进程重启/二次加载)
        using var store = new TemplateStore(tempRoot, "mock_800x600", 0.85);
        Assert.Single(store.Keys);
        Assert.Contains("mock_btn_go", store.Keys);

        var entry = store.Get("mock_btn_go");
        Assert.Equal("mock_btn_go", entry.Key);
        Assert.Equal(0.85, entry.Threshold);
    }

    [Fact]
    public void SaveTemplate_Twice_IdempotentAndPreservesOldEntry()
    {
        // 接缝断言 4:幂等覆盖 — 同一 key 多次 save 仅替换条目(不重复添加),
        // 不同 key 并存(防 Reload 路径丢失历史)。
        var tempRoot = NewTempRoot();
        using var frame1 = MakeMockFrame();
        using var frame2 = MakeMockFrame();
        var cmd1 = new SaveTemplateCommand(new MockFrameCapture(frame1));
        var cmd2 = new SaveTemplateCommand(new MockFrameCapture(frame2));

        var config = NewDevConfig(tempRoot);
        var optionsTaskbar = new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["key"] = "mock_taskbar",
            ["x"] = "16",
            ["y"] = "16",
            ["w"] = "320",
            ["h"] = "88",
        };
        var optionsBtn = new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["key"] = "mock_btn_go",
            ["x"] = "16",
            ["y"] = "180",
            ["w"] = "140",
            ["h"] = "48",
        };

        Assert.Equal((int)ExitCode.Success, cmd1.Execute(config, optionsTaskbar, CancellationToken.None));
        Assert.Equal((int)ExitCode.Success, cmd2.Execute(config, optionsBtn, CancellationToken.None));

        // 2 模板共存
        using var store = new TemplateStore(tempRoot, "mock_800x600", 0.85);
        Assert.Equal(2, store.Keys.Count);
        Assert.Contains("mock_taskbar", store.Keys);
        Assert.Contains("mock_btn_go", store.Keys);

        // 二次写同一 key(幂等覆盖):mock_btn_go 位置(x=16,y=180)改为(x=200,y=200) → 条目应被替换
        using var frame3 = MakeMockFrame();
        var cmd3 = new SaveTemplateCommand(new MockFrameCapture(frame3));
        var optionsBtnMoved = new Dictionary<string, string>
        {
            ["hwnd"] = "12345",
            ["key"] = "mock_btn_go",
            ["x"] = "200",
            ["y"] = "200",
            ["w"] = "140",
            ["h"] = "48",
        };
        Assert.Equal((int)ExitCode.Success, cmd3.Execute(config, optionsBtnMoved, CancellationToken.None));

        // 仍 2 条目(mock_btn_go 被替换,非追加)
        using var storeAfter = new TemplateStore(tempRoot, "mock_800x600", 0.85);
        Assert.Equal(2, storeAfter.Keys.Count);
        Assert.Contains("mock_taskbar", storeAfter.Keys);
        Assert.Contains("mock_btn_go", storeAfter.Keys);

        var moved = storeAfter.Get("mock_btn_go");
        Assert.Equal(140, moved.Image.Cols);
        Assert.Equal(48, moved.Image.Rows);
    }

    public void Dispose()
    {
        foreach (var d in _tempRoots)
        {
            try { Directory.Delete(d, recursive: true); } catch { /* best effort */ }
        }
    }
}
