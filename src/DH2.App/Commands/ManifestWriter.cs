using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DH2.App.Commands;

/// <summary>
/// <c>dh2ctl save-template</c> 写入路径专用的 manifest.yaml 序列化器(技术设计 §5.1 / RJ-S3-01)。
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>序列化与反序列化均使用 <see cref="CamelCaseNamingConvention"/>,
///         与读取端 <c>DH2.Vision.TemplateManifestParser</c> 严格对齐。</item>
///   <item>schema:<c>profile: ...; templates: - key: ...</c>(§5.1)。</item>
///   <item>本类 <c>internal</c> 暴露给 <c>DH2.Tests</c>(<c>[InternalsVisibleTo]</c>),供 writer 合规回归 UT 直接调写路径。</item>
/// </list>
/// </remarks>
internal static class ManifestWriter
{
    private sealed class ManifestConfig
    {
        public string Profile { get; set; } = "";
        public List<Entry> Templates { get; set; } = new();
    }

    private sealed class Entry
    {
        public string Key { get; set; } = "";
        public string File { get; set; } = "";
        public double Threshold { get; set; }
        public Offset ClickOffset { get; set; } = new();
        public string Roi { get; set; } = "";
        public string Since { get; set; } = "m0";
    }

    private sealed class Offset
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>
    /// 幂等覆盖:同 key 替换(保留原 <c>Since</c> 字段);异 key 追加。
    /// </summary>
    public static void UpsertEntry(
        string manifestPath,
        string profile,
        string key,
        string file,
        double threshold)
    {
        var yaml = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : "";
        var config = Parse(yaml);
        config.Profile = profile;

        var existing = config.Templates.FindIndex(e => e.Key == key);
        if (existing >= 0)
        {
            config.Templates[existing] = new Entry
            {
                Key = key,
                File = file,
                Threshold = threshold,
                ClickOffset = new Offset(),
                Roi = "",
                Since = config.Templates[existing].Since,
            };
        }
        else
        {
            config.Templates.Add(new Entry
            {
                Key = key,
                File = file,
                Threshold = threshold,
                ClickOffset = new Offset(),
                Roi = "",
                Since = "m0",
            });
        }

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var output = serializer.Serialize(config);

        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        File.WriteAllText(manifestPath, output);
    }

    private static ManifestConfig Parse(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return new ManifestConfig();
        }

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            return deserializer.Deserialize<ManifestConfig>(yaml) ?? new ManifestConfig();
        }
        catch
        {
            return new ManifestConfig();
        }
    }
}
