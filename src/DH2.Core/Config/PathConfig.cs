namespace DH2.Core.Config;

/// <summary>
/// 路径配置:模板库根目录与运行期产物目录。
/// </summary>
/// <remarks>
/// M0-S2 改造(RJ-S2-01 / DEF-S2-01):
/// 从 <c>sealed record PathConfig(string Templates, string Artifacts)</c> 改为 <c>sealed class</c> +
/// 无参构造 + settable 属性,以兼容 YamlDotNet 16 反序列化。
/// </remarks>
public sealed class PathConfig
{
    /// <summary>模板库根目录(相对运行目录;manifest 与 PNG 都在其下)。</summary>
    public string Templates { get; set; } = "templates";

    /// <summary>运行产物目录(截图 / 报告 / 日志;M0 §9)。</summary>
    public string Artifacts { get; set; } = "artifacts";

    /// <summary>无参构造:供 YamlDotNet 实例化。</summary>
    public PathConfig()
    {
    }

    /// <summary>位置构造:供产品代码显式构造。</summary>
    public PathConfig(string templates, string artifacts)
    {
        Templates = templates;
        Artifacts = artifacts;
    }
}
