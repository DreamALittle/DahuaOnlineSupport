using System.Text.RegularExpressions;
using DH2.Core.Contracts;
using DH2.Core.Models;
using OpenCvSharp;
using DH2Point = DH2.Core.Models.Point;

namespace DH2.Vision;

/// <summary>
/// 模板库实现(技术设计 §5.1)。
/// 构造:读 <c>templates/{profile}/manifest.yaml</c> → 加载 PNG → 灰度 → 不可变快照。
/// <see cref="Reload"/>:重新读 manifest + PNG,生成新快照,释放旧 Mat。
/// 校验失败(文件缺失 / 字段非法 / key 重复)聚合为 <see cref="InvalidDataException"/>(一次性抛,内含全部条目级错误)。
/// </summary>
public sealed class TemplateStore : ITemplateStore, IDisposable
{
    private readonly string _templatesRoot;
    private readonly string _profile;
    private readonly double _defaultThreshold;

    private IReadOnlyDictionary<string, TemplateEntry> _snapshot = new Dictionary<string, TemplateEntry>();
    private bool _disposed;

    /// <summary>
    /// 构造时即加载。失败抛 <see cref="InvalidDataException"/>。
    /// </summary>
    /// <param name="templatesRoot">模板根目录(如 <c>templates</c>),其下含 <c>{profile}/manifest.yaml</c> 与 PNG。</param>
    /// <param name="profile">档案名(如 <c>mock_800x600</c>);与 manifest 内 profile 字段须一致(不一致抛错)。</param>
    /// <param name="defaultThreshold">manifest 缺省阈值;manifest 中条目未给 <c>threshold</c> 时使用。</param>
    public TemplateStore(string templatesRoot, string profile, double defaultThreshold)
    {
        if (string.IsNullOrWhiteSpace(templatesRoot))
        {
            throw new ArgumentException("templatesRoot 不能为空", nameof(templatesRoot));
        }

        if (string.IsNullOrWhiteSpace(profile))
        {
            throw new ArgumentException("profile 不能为空", nameof(profile));
        }

        if (defaultThreshold <= 0.5 || defaultThreshold >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultThreshold),
                $"defaultThreshold 必须在 (0.5, 1.0),实际 {defaultThreshold}");
        }

        _templatesRoot = templatesRoot;
        _profile = profile;
        _defaultThreshold = defaultThreshold;
        Reload();
    }

    public IReadOnlyList<string> Keys => _snapshot.Keys.ToArray();

    public TemplateEntry Get(string key)
    {
        if (_snapshot.TryGetValue(key, out var entry))
        {
            return entry;
        }

        throw new KeyNotFoundException(
            $"template key '{key}' 未在 manifest 注册(已注册: {string.Join(", ", _snapshot.Keys)})");
    }

    /// <summary>重新读 manifest + PNG,生成新快照;释放旧 Mat。</summary>
    public void Reload()
    {
        DisposeSnapshot();

        var manifestPath = System.IO.Path.Combine(_templatesRoot, _profile, "manifest.yaml");
        if (!File.Exists(manifestPath))
        {
            throw new InvalidDataException(
                $"manifest.yaml 未找到: {manifestPath}(profile={_profile})");
        }

        TemplateManifestConfig manifest;
        try
        {
            manifest = TemplateManifestParser.Parse(File.ReadAllText(manifestPath));
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"解析 manifest 失败: {ex.Message}", ex);
        }

        if (!string.Equals(manifest.Profile, _profile, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"manifest.profile '{manifest.Profile}' 与期望 '{_profile}' 不一致");
        }

        var errors = new List<string>();
        var next = new Dictionary<string, TemplateEntry>(StringComparer.Ordinal);
        var profileDir = System.IO.Path.Combine(_templatesRoot, _profile);

        foreach (var entry in manifest.Templates)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                errors.Add("模板条目 key 为空");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.File))
            {
                errors.Add($"[{entry.Key}] file 字段为空");
                continue;
            }

            if (next.ContainsKey(entry.Key))
            {
                errors.Add($"[{entry.Key}] key 重复");
                continue;
            }

            // threshold 校验(若显式给出)
            if (entry.Threshold is double t && (t <= 0.5 || t >= 1.0))
            {
                errors.Add($"[{entry.Key}] threshold {t} 超出 (0.5, 1.0)");
                continue;
            }

            var pngPath = System.IO.Path.Combine(profileDir, entry.File);
            if (!File.Exists(pngPath))
            {
                errors.Add($"[{entry.Key}] 文件不存在: {pngPath}");
                continue;
            }

            Mat? image = null;
            try
            {
                image = Cv2.ImRead(pngPath, ImreadModes.Grayscale);
                if (image.Empty())
                {
                    errors.Add($"[{entry.Key}] ImRead 解析为空: {pngPath}");
                    continue;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"[{entry.Key}] ImRead 异常: {ex.Message}");
                continue;
            }

            next[entry.Key] = new TemplateEntry(
                Key: entry.Key,
                File: entry.File,
                Threshold: entry.Threshold ?? _defaultThreshold,
                ClickOffset: new DH2Point(entry.ClickOffset.X, entry.ClickOffset.Y),
                Roi: entry.Roi ?? "",
                Since: entry.Since ?? "",
                Image: image);
        }

        if (errors.Count > 0)
        {
            // 加载途中已分配成功的 Mat 全部释放,避免泄漏
            foreach (var kv in next)
            {
                kv.Value.Image.Dispose();
            }

            throw new InvalidDataException(
                "manifest 校验失败(" + errors.Count + " 项): "
                + string.Join("; ", errors));
        }

        _snapshot = next;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeSnapshot();
        GC.SuppressFinalize(this);
    }

    private void DisposeSnapshot()
    {
        foreach (var kv in _snapshot)
        {
            kv.Value.Image.Dispose();
        }

        _snapshot = new Dictionary<string, TemplateEntry>();
    }
}
