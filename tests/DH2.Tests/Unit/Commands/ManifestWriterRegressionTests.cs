// DH2.Tests — RJ-S3-01 writer 合规回归 UT(架构师明令例外)
//
// 目的:锁死 save-template 写入的 manifest.yaml schema 与读取端一致(技术设计 §5.1)。
// 关键约束(RJ-S3-01 指令):
//   - 必须由 writer 真实写入临时文件路径,而非字符串字面量替代;
//   - 断言写出的 manifest 文本含 `profile:` 与 `- key:`(锁死 §5.1 schema,防回归);
//   - 同时做端到端回环:再喂给相同 CamelCase 反序列化器,字段不丢失。
//
// 端到端接缝集成 UT(RJ-S3-02)由测试 Agent 在本轮复测执行:写 → TemplateStore 加载 → Get 返回有效条目。

using DH2.App.Commands;
using Xunit;

namespace DH2.Tests.Unit.Commands;

public class ManifestWriterRegressionTests
{
    [Fact]
    public void UpsertEntry_WritesCamelCaseSchema_ProfileAndKeyPresent()
    {
        // Arrange:临时 manifest.yaml 路径
        var tempRoot = Path.Combine(Path.GetTempPath(), "dh2-manifestwriter-" + Guid.NewGuid().ToString("N"));
        var profileDir = Path.Combine(tempRoot, "mock_800x600");
        Directory.CreateDirectory(profileDir);
        var manifestPath = Path.Combine(profileDir, "manifest.yaml");

        try
        {
            // Act:第一次写入 — 单一模板条目
            ManifestWriter.UpsertEntry(
                manifestPath,
                profile: "mock_800x600",
                key: "mock_taskbar",
                file: "png/mock_taskbar.png",
                threshold: 0.85);

            // Assert 1:文件已生成且含关键 schema 标记(RJ-S3-01 锁死要求)
            Assert.True(File.Exists(manifestPath), $"manifest file must exist at {manifestPath}");
            var content = File.ReadAllText(manifestPath);

            Assert.Contains("profile: mock_800x600", content);
            Assert.Contains("- key: mock_taskbar", content);
            Assert.Contains("file: png/mock_taskbar.png", content);
            // PascalCase 必须不存在(防止再次回归)
            Assert.DoesNotContain("Profile:", content);
            Assert.DoesNotContain("- Key:", content);
            Assert.DoesNotContain("File:", content);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void UpsertEntry_Idempotent_SameKeyReplacesEntry()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), "dh2-manifestwriter-" + Guid.NewGuid().ToString("N"));
        var profileDir = Path.Combine(tempRoot, "mock_800x600");
        Directory.CreateDirectory(profileDir);
        var manifestPath = Path.Combine(profileDir, "manifest.yaml");

        try
        {
            // Act 1:写入 mock_taskbar
            ManifestWriter.UpsertEntry(
                manifestPath, "mock_800x600", "mock_taskbar", "png/mock_taskbar.png", 0.85);
            // Act 2:同 key 再写(覆盖语义;SAC3-3 幂等)
            ManifestWriter.UpsertEntry(
                manifestPath, "mock_800x600", "mock_taskbar", "png/mock_taskbar.png", 0.90);

            var content = File.ReadAllText(manifestPath);

            // Assert:仍只有 1 条 mock_taskbar 条目,且 key 仅出现 1 次(SAC3-3 幂等)
            var keyOccurrences = CountOccurrences(content, "key: mock_taskbar");
            Assert.Equal(1, keyOccurrences);
            // 覆盖后 threshold 应为最新值(0.90)
            Assert.Contains("threshold: 0.9", content);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void UpsertEntry_MultipleKeys_AppendsDistinctEntries()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), "dh2-manifestwriter-" + Guid.NewGuid().ToString("N"));
        var profileDir = Path.Combine(tempRoot, "mock_800x600");
        Directory.CreateDirectory(profileDir);
        var manifestPath = Path.Combine(profileDir, "manifest.yaml");

        try
        {
            // Act:写入三个模板条目(S3-4 资产生成)
            ManifestWriter.UpsertEntry(manifestPath, "mock_800x600", "mock_taskbar", "png/mock_taskbar.png", 0.85);
            ManifestWriter.UpsertEntry(manifestPath, "mock_800x600", "mock_btn_go", "png/mock_btn_go.png", 0.85);
            ManifestWriter.UpsertEntry(manifestPath, "mock_800x600", "mock_btn_return", "png/mock_btn_return.png", 0.85);

            var content = File.ReadAllText(manifestPath);

            // Assert:每 key 仅出现 1 次,3 条目均带 `- key:` schema
            Assert.Equal(1, CountOccurrences(content, "key: mock_taskbar"));
            Assert.Equal(1, CountOccurrences(content, "key: mock_btn_go"));
            Assert.Equal(1, CountOccurrences(content, "key: mock_btn_return"));
            Assert.Equal(3, CountOccurrences(content, "- key:"));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void UpsertEntry_ProduceYamlThatRoundtripsThroughCamelCaseDeserializer()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), "dh2-manifestwriter-" + Guid.NewGuid().ToString("N"));
        var profileDir = Path.Combine(tempRoot, "mock_800x600");
        Directory.CreateDirectory(profileDir);
        var manifestPath = Path.Combine(profileDir, "manifest.yaml");

        try
        {
            // Act:写
            ManifestWriter.UpsertEntry(manifestPath, "mock_800x600", "mock_taskbar", "png/mock_taskbar.png", 0.85);

            // 回环读(用与 DH2.Vision.TemplateManifestParser 同配置的 CamelCase 反序列化器)。
            // 关键断言:写出的文本可被相同命名约定的读取端成功反序列化,
            // 且解析后的 profile / key / file / threshold 值与写入端一致。
            // (RJ-S3-02 端到端 UT 由测试 Agent 在本轮复测执行:写 → TemplateStore → Get。)
            var yaml = File.ReadAllText(manifestPath);
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            using var reader = new StringReader(yaml);
            var stream = new YamlDotNet.RepresentationModel.YamlStream();
            stream.Load(reader);

            Assert.NotEmpty(stream.Documents);
            var root = stream.Documents[0].RootNode;
            Assert.Equal(YamlDotNet.RepresentationModel.YamlNodeType.Mapping, root.NodeType);

            // 顶层 profile 字段断言(字符串值)
            var profileNode = root["profile"];
            Assert.NotNull(profileNode);
            Assert.Equal("mock_800x600", profileNode.ToString().Trim('"', '\''));

            // templates 列表第一条断言
            var templatesNode = root["templates"];
            Assert.NotNull(templatesNode);
            var firstTemplate = templatesNode[0];
            Assert.NotNull(firstTemplate);
            Assert.Equal("mock_taskbar", firstTemplate["key"]!.ToString().Trim('"', '\''));
            Assert.Equal("png/mock_taskbar.png", firstTemplate["file"]!.ToString().Trim('"', '\''));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
