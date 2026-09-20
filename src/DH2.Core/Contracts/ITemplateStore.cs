namespace DH2.Core.Contracts;

/// <summary>
/// 模板库抽象:对外暴露不可变快照,支持运行时热重载。
/// </summary>
/// <remarks>
/// M0 实现: <c>DH2.Vision.TemplateStore</c>(S3,Dev B):解析 <c>templates/{profile}/manifest.yaml</c> +
/// 加载 PNG → <c>Cv2.ImRead</c> 灰度;加载完成形成不可变快照;<see cref="Reload"/> 重新读取。
/// </remarks>
public interface ITemplateStore
{
    /// <summary>当前已加载的全部模板键(逻辑名)。</summary>
    IReadOnlyList<string> Keys { get; }

    /// <summary>
    /// 按键获取模板条目。未注册时抛 <see cref="KeyNotFoundException"/>。
    /// </summary>
    /// <param name="key">模板逻辑名(见 manifest 的 <c>key</c> 字段)。</param>
    TemplateEntry Get(string key);

    /// <summary>
    /// 重新读 manifest 与图片,生成新快照。期间不抛业务异常(错误通过 <c>Get</c> 的 <c>KeyNotFoundException</c> 暴露)。
    /// </summary>
    void Reload();
}