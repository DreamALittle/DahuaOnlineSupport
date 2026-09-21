using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DH2.Vision;

/// <summary>
/// templates/{profile}/manifest.yaml 的 YAML 反序列化模型(技术设计 §5.1)。
/// 字段缺省由 YamlDotNet 的 <c>IgnoreUnmatchedProperties</c> 容忍,但 <c>key</c> / <c>file</c> / <c>clickOffset</c> 必需。
/// </summary>
internal sealed class TemplateManifestConfig
{
    public string Profile { get; set; } = "";

    public List<TemplateManifestEntry> Templates { get; set; } = new();
}

internal sealed class TemplateManifestEntry
{
    public string Key { get; set; } = "";

    public string File { get; set; } = "";

    /// <summary>可空;为空则取 <c>DevConfig.Matching.DefaultThreshold</c>。</summary>
    public double? Threshold { get; set; }

    public ClickOffsetConfig ClickOffset { get; set; } = new(0, 0);

    public string? Roi { get; set; }

    public string? Since { get; set; }
}

internal sealed record ClickOffsetConfig(int X, int Y)
{
    public ClickOffsetConfig() : this(0, 0) { }
}

/// <summary>manifest YAML 解析(私有,供 <see cref="TemplateStore"/> 使用)。</summary>
internal static class TemplateManifestParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static TemplateManifestConfig Parse(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new InvalidDataException("manifest yaml content is empty");
        }

        try
        {
            var cfg = Deserializer.Deserialize<TemplateManifestConfig>(yaml)
                ?? throw new InvalidDataException("deserialized manifest is null");
            return cfg;
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException($"failed to parse manifest yaml: {ex.Message}", ex);
        }
    }
}
