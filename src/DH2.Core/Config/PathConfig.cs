namespace DH2.Core.Config;

/// <summary>
/// 路径配置:模板库根目录与运行期产物目录。
/// </summary>
/// <param name="Templates">模板库根目录(相对运行目录;manifest 与 PNG 都在其下)。</param>
/// <param name="Artifacts">运行产物目录(截图 / 报告 / 日志;M0 §9)。</param>
public sealed record PathConfig(string Templates, string Artifacts);