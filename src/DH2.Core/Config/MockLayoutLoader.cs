using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DH2.Core.Config;

/// <summary>
/// MockLayoutConfig YAML 加载器。<see cref="MockLayoutConfig"/> 字段含默认值,缺失字段自动填充。
/// </summary>
public static class MockLayoutLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>从 YAML 文本解析 <see cref="MockLayoutConfig"/>。</summary>
    /// <param name="yaml">YAML 文本;为空 / 解析失败抛 <see cref="InvalidDataException"/>。</param>
    public static MockLayoutConfig Parse(string? yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new InvalidDataException("mock-layout yaml content is empty");
        }

        try
        {
            var cfg = Deserializer.Deserialize<MockLayoutConfig>(yaml);
            return cfg ?? throw new InvalidDataException("deserialized mock-layout is null");
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException($"failed to parse mock-layout yaml: {ex.Message}", ex);
        }
    }

    /// <summary>从文件加载;文件不存在抛 <see cref="FileNotFoundException"/>。</summary>
    /// <param name="path">YAML 文件绝对或相对路径。</param>
    public static MockLayoutConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"mock-layout file not found: {path}", path);
        }

        var yaml = File.ReadAllText(path);
        return Parse(yaml);
    }
}
