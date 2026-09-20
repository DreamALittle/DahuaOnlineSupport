namespace DH2.Core.Config;

/// <summary>
/// DevConfig / MockLayoutConfig 启动校验。
/// 校验失败时由调用方聚合错误并以退出码 3 退出(技术设计 §2.3 / §6)。
/// </summary>
public static class ConfigValidator
{
    private static readonly HashSet<string> SupportedDrivers = new(StringComparer.OrdinalIgnoreCase)
    {
        // M0 仅实现 background;foreground 在 M0 未实现,但列入集合使 else-if 分支可达,
        // 由下方专门分支输出"foreground driver not implemented in M0; use 'background'"。
        "background",
        "foreground",
    };

    /// <summary>
    /// 校验 <see cref="DevConfig"/>。所有错误聚合并返回,绝不抛异常。
    /// </summary>
    /// <param name="config">待校验配置;为 <c>null</c> 时返回单条 <c>"root is null"</c> 错误。</param>
    /// <returns>错误列表;为空表示校验通过。</returns>
    public static IReadOnlyList<ConfigError> Validate(DevConfig? config)
    {
        var errors = new List<ConfigError>();

        if (config is null)
        {
            errors.Add(new ConfigError("root", "configuration is null"));
            return errors;
        }

        // Profile 必须非空
        if (string.IsNullOrWhiteSpace(config.Profile))
        {
            errors.Add(new ConfigError(nameof(config.Profile), "profile must not be empty"));
        }

        // Targets 至少一个,每个至少 TitlePattern/ProcessName 之一非空
        if (config.Targets is null || config.Targets.Count == 0)
        {
            errors.Add(new ConfigError(nameof(config.Targets), "at least one target is required"));
        }
        else
        {
            for (var i = 0; i < config.Targets.Count; i++)
            {
                var t = config.Targets[i];
                if (t is null)
                {
                    errors.Add(new ConfigError($"Targets[{i}]", "target is null"));
                    continue;
                }

                var hasTitle = !string.IsNullOrWhiteSpace(t.TitlePattern);
                var hasProc = !string.IsNullOrWhiteSpace(t.ProcessName);
                if (!hasTitle && !hasProc)
                {
                    errors.Add(new ConfigError(
                        $"Targets[{i}]",
                        "at least one of TitlePattern/ProcessName must be non-empty"));
                }
            }
        }

        // Matching.DefaultThreshold ∈ (0.5, 1.0)
        var m = config.Matching;
        if (m is null)
        {
            errors.Add(new ConfigError(nameof(config.Matching), "matching section is required"));
        }
        else if (!(m.DefaultThreshold > 0.5 && m.DefaultThreshold < 1.0))
        {
            errors.Add(new ConfigError(
                $"{nameof(config.Matching)}.{nameof(m.DefaultThreshold)}",
                $"defaultThreshold must be in (0.5, 1.0); got {m.DefaultThreshold}"));
        }

        // Input.Driver ∈ {background, foreground};foreground M0 未实现
        var input = config.Input;
        if (input is null)
        {
            errors.Add(new ConfigError(nameof(config.Input), "input section is required"));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input.Driver) ||
                !SupportedDrivers.Contains(input.Driver))
            {
                errors.Add(new ConfigError(
                    $"{nameof(config.Input)}.{nameof(input.Driver)}",
                    $"driver must be one of {{background, foreground}}; got '{input.Driver}'"));
            }
            else if (string.Equals(input.Driver, "foreground", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new ConfigError(
                    $"{nameof(config.Input)}.{nameof(input.Driver)}",
                    "foreground driver not implemented in M0; use 'background'"));
            }

            if (input.PostClickDelayMs < 0)
            {
                errors.Add(new ConfigError(
                    $"{nameof(config.Input)}.{nameof(input.PostClickDelayMs)}",
                    $"postClickDelayMs must be >= 0; got {input.PostClickDelayMs}"));
            }
        }

        // Paths:存在性(暂时只校验非空字符串)
        var paths = config.Paths;
        if (paths is null)
        {
            errors.Add(new ConfigError(nameof(config.Paths), "paths section is required"));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(paths.Templates))
            {
                errors.Add(new ConfigError(
                    $"{nameof(config.Paths)}.{nameof(paths.Templates)}",
                    "templates path must not be empty"));
            }
            if (string.IsNullOrWhiteSpace(paths.Artifacts))
            {
                errors.Add(new ConfigError(
                    $"{nameof(config.Paths)}.{nameof(paths.Artifacts)}",
                    "artifacts path must not be empty"));
            }
        }

        return errors;
    }
}
