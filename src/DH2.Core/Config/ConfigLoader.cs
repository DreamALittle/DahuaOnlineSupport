using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DH2.Core.Config;

/// <summary>
/// DevConfig YAML 加载器(YamlDotNet 绑定)。
/// </summary>
/// <remarks>
/// 默认配置 <see cref="DevConfig"/> 已隐式类型属性提供默认值;缺失字段由 YamlDotNet 填充。
/// 调用方负责在加载后调用 <see cref="ConfigValidator.Validate(DevConfig)"/>。
/// </remarks>
public static class ConfigLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// 从 YAML 文本解析 <see cref="DevConfig"/>。
    /// </summary>
    /// <param name="yaml">YAML 文本内容;为 <c>null</c> / 空 / 解析失败时抛 <see cref="InvalidDataException"/>。</param>
    /// <returns>解析后的配置实例(未校验)。</returns>
    public static DevConfig Parse(string? yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new InvalidDataException("config yaml content is empty");
        }

        try
        {
            var cfg = Deserializer.Deserialize<DevConfig>(yaml);
            return cfg ?? throw new InvalidDataException("deserialized config is null");
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException($"failed to parse config yaml: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从文件加载 <see cref="DevConfig"/>;文件不存在抛 <see cref="FileNotFoundException"/>。
    /// </summary>
    /// <param name="path">YAML 文件绝对或相对路径。</param>
    public static DevConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"config file not found: {path}", path);
        }

        var yaml = File.ReadAllText(path);
        return Parse(yaml);
    }
}
