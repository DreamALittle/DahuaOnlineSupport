// DH2.Tests — L1 单元测试
// UT-03 TemplateStore:manifest YAML 解析 / 聚合校验 / 热重载 / 不可变快照
// 参见 docs/iterations/M0/ITER-M0-测试设计.md §1 + DH2.Vision.TemplateStore

using System.Text;
using DH2.Core.Models;
using DH2.Vision;
using OpenCvSharp;
using Xunit;

namespace DH2.Tests.Unit.Vision;

public class TemplateStoreTests : IDisposable
{
    private readonly List<string> _tempDirs = new();

    /// <summary>在临时目录建立 {profile} 子目录,写 manifest.yaml + PNG 文件,返回 profile 目录根。</summary>
    private string BuildProfile(string profile, string manifestYaml, params (string File, byte[] Png)[] pngs)
    {
        var root = Path.Combine(Path.GetTempPath(), $"dh2-vision-{Guid.NewGuid():N}");
        var profileDir = Path.Combine(root, profile);
        Directory.CreateDirectory(profileDir);
        File.WriteAllText(Path.Combine(profileDir, "manifest.yaml"), manifestYaml, Encoding.UTF8);
        foreach (var (file, png) in pngs)
        {
            var fullPath = Path.Combine(profileDir, file);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(fullPath, png);
        }
        _tempDirs.Add(root);
        return root;
    }

    /// <summary>生成一张 N×N 灰度 PNG 字节(用于合法模板)。</summary>
    private static byte[] MakeGrayPng(int size)
    {
        using var mat = new Mat(size, size, MatType.CV_8UC1, Scalar.All(128));
        Cv2.ImEncode(".png", mat, out var buf);
        return buf.ToArray();
    }

    public void Dispose()
    {
        foreach (var d in _tempDirs)
        {
            try { Directory.Delete(d, recursive: true); } catch { /* best effort */ }
        }
    }

    // ───── UT-03 manifest 解析 / 聚合校验 / 热重载 ─────

    [Fact]
    public void Construct_ValidManifest_LoadsAllEntries()
    {
        var manifest = """
            profile: mock_800x600
            templates:
              - key: mock_taskbar
                file: png/mock_taskbar.png
                threshold: 0.85
                clickOffset: { x: 0, y: 0 }
                roi: taskbar
                since: m0
              - key: mock_btn_go
                file: png/mock_btn_go.png
                threshold: 0.90
                clickOffset: { x: 0, y: 0 }
                roi: button
                since: m0
            """;
        var root = BuildProfile("mock_800x600", manifest,
            ("png/mock_taskbar.png", MakeGrayPng(100)),
            ("png/mock_btn_go.png", MakeGrayPng(50)));

        using var store = new TemplateStore(root, "mock_800x600", defaultThreshold: 0.85);

        Assert.Equal(2, store.Keys.Count);
        Assert.Contains("mock_taskbar", store.Keys);
        Assert.Contains("mock_btn_go", store.Keys);

        var entry = store.Get("mock_taskbar");
        Assert.Equal("mock_taskbar", entry.Key);
        Assert.Equal("png/mock_taskbar.png", entry.File);
        Assert.Equal(0.85, entry.Threshold);
        Assert.Equal("taskbar", entry.Roi);
        Assert.Equal("m0", entry.Since);
    }

    [Fact]
    public void Construct_ThresholdMissing_UsesDefault()
    {
        // manifest 不给 threshold → 取 defaultThreshold
        var manifest = """
            profile: mock_800x600
            templates:
              - key: k1
                file: png/k1.png
            """;
        var root = BuildProfile("mock_800x600", manifest,
            ("png/k1.png", MakeGrayPng(20)));

        using var store = new TemplateStore(root, "mock_800x600", defaultThreshold: 0.88);

        Assert.Equal(0.88, store.Get("k1").Threshold);
    }

    [Fact]
    public void Get_UnknownKey_ThrowsKeyNotFoundException()
    {
        var manifest = """
            profile: mock_800x600
            templates:
              - key: only
                file: png/only.png
            """;
        var root = BuildProfile("mock_800x600", manifest, ("png/only.png", MakeGrayPng(10)));

        using var store = new TemplateStore(root, "mock_800x600", 0.85);

        var ex = Assert.Throws<KeyNotFoundException>(() => store.Get("missing"));
        Assert.Contains("missing", ex.Message);
        Assert.Contains("only", ex.Message); // 错误消息列出已注册 key
    }

    [Fact]
    public void Construct_AggregatesErrors_PNGMissing()
    {
        // manifest 中 entry 指向不存在的 PNG → 聚合报错
        var manifest = """
            profile: mock_800x600
            templates:
              - key: k_exists
                file: png/exists.png
              - key: k_missing
                file: png/missing.png
            """;
        var root = BuildProfile("mock_800x600", manifest,
            ("png/exists.png", MakeGrayPng(10)));
        // 注意:不创建 png/missing.png

        var ex = Assert.Throws<InvalidDataException>(() =>
            new TemplateStore(root, "mock_800x600", 0.85));

        Assert.Contains("k_missing", ex.Message);
        Assert.Contains("文件不存在", ex.Message);
    }

    [Fact]
    public void Construct_AggregatesErrors_ThresholdOutOfRange()
    {
        var manifest = """
            profile: mock_800x600
            templates:
              - key: k_bad
                file: png/k_bad.png
                threshold: 1.5
            """;
        var root = BuildProfile("mock_800x600", manifest, ("png/k_bad.png", MakeGrayPng(10)));

        var ex = Assert.Throws<InvalidDataException>(() =>
            new TemplateStore(root, "mock_800x600", 0.85));

        Assert.Contains("k_bad", ex.Message);
        Assert.Contains("threshold", ex.Message);
        Assert.Contains("1.5", ex.Message);
    }

    [Fact]
    public void Construct_AggregatesErrors_KeyConflict()
    {
        // 同 key 出现两次 → 聚合报错,均带 key 名
        var manifest = """
            profile: mock_800x600
            templates:
              - key: dup
                file: png/a.png
              - key: dup
                file: png/b.png
            """;
        var root = BuildProfile("mock_800x600", manifest,
            ("png/a.png", MakeGrayPng(10)),
            ("png/b.png", MakeGrayPng(10)));

        var ex = Assert.Throws<InvalidDataException>(() =>
            new TemplateStore(root, "mock_800x600", 0.85));

        Assert.Contains("dup", ex.Message);
        Assert.Contains("key 重复", ex.Message);
    }

    [Fact]
    public void Construct_AggregatesErrors_MultipleProblems()
    {
        // 同时多个 entry 字段非法 → 聚合输出全部错误(而非只报第一个)
        var manifest = """
            profile: mock_800x600
            templates:
              - key: bad_thresh
                file: png/x.png
                threshold: 2.0
              - key: missing_png
                file: png/y.png
              - key: good
                file: png/z.png
            """;
        var root = BuildProfile("mock_800x600", manifest,
            ("png/x.png", MakeGrayPng(10)),
            ("png/z.png", MakeGrayPng(10)));
        // 不创建 png/y.png

        var ex = Assert.Throws<InvalidDataException>(() =>
            new TemplateStore(root, "mock_800x600", 0.85));

        Assert.Contains("bad_thresh", ex.Message);
        Assert.Contains("missing_png", ex.Message);
        // 'good' 不应在错误消息里
        Assert.DoesNotContain("[good]", ex.Message);
    }

    [Fact]
    public void Construct_ManifestProfileMismatch_Throws()
    {
        var manifest = """
            profile: other_profile
            templates:
              - key: k
                file: png/k.png
            """;
        var root = BuildProfile("mock_800x600", manifest, ("png/k.png", MakeGrayPng(10)));

        var ex = Assert.Throws<InvalidDataException>(() =>
            new TemplateStore(root, "mock_800x600", 0.85));

        Assert.Contains("manifest.profile", ex.Message);
        Assert.Contains("other_profile", ex.Message);
    }

    [Fact]
    public void Construct_ManifestMissing_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), $"dh2-vision-{Guid.NewGuid():N}");
        _tempDirs.Add(root);
        Directory.CreateDirectory(Path.Combine(root, "mock_800x600"));
        // 不写 manifest.yaml

        var ex = Assert.Throws<InvalidDataException>(() =>
            new TemplateStore(root, "mock_800x600", 0.85));

        Assert.Contains("manifest.yaml", ex.Message);
    }

    [Fact]
    public void Construct_DefaultThresholdOutOfRange_Throws()
    {
        var manifest = """
            profile: p
            templates: []
            """;
        var root = BuildProfile("p", manifest);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TemplateStore(root, "p", 1.5));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TemplateStore(root, "p", 0.5));
    }

    [Fact]
    public void Construct_ProfileOrRootEmpty_Throws()
    {
        var manifest = "profile: p\ntemplates: []\n";
        var root = BuildProfile("p", manifest);

        Assert.Throws<ArgumentException>(() =>
            new TemplateStore(root, "", 0.85));
        Assert.Throws<ArgumentException>(() =>
            new TemplateStore("", "p", 0.85));
    }

    [Fact]
    public void Reload_AfterFileChange_PicksUpNewEntries()
    {
        // 初始 manifest 只有 1 个条目;Reload 后再加一个,应被识别
        var manifest1 = """
            profile: p
            templates:
              - key: a
                file: png/a.png
            """;
        var root = BuildProfile("p", manifest1, ("png/a.png", MakeGrayPng(10)));

        var store = new TemplateStore(root, "p", 0.85);
        Assert.Single(store.Keys);

        // 修改 manifest,加 b 模板
        var manifest2 = """
            profile: p
            templates:
              - key: a
                file: png/a.png
              - key: b
                file: png/b.png
            """;
        File.WriteAllText(Path.Combine(root, "p", "manifest.yaml"), manifest2, Encoding.UTF8);
        File.WriteAllBytes(Path.Combine(root, "p", "png/b.png"), MakeGrayPng(10));

        store.Reload();
        Assert.Equal(2, store.Keys.Count);
        Assert.Contains("b", store.Keys);

        store.Dispose();
    }
}
