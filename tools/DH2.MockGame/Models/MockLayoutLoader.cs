using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DH2.MockGame.Models;

/// <summary>
/// 加载并展开 configs/mock-layout.yaml(tempDir 展开 %TEMP% 等环境变量)。
/// 启动期单次调用,失败即抛(进程退出码由 Program.cs 兜底)。
/// </summary>
public static class MockLayoutLoader
{
    /// <summary>
    /// 按约定路径加载:工作目录下 configs/mock-layout.yaml,或与可执行文件并列的 Configs/mock-layout.yaml。
    /// 优先使用文件存在者。
    /// </summary>
    public static MockLayout Load()
    {
        var candidates = ResolveCandidates();
        FileInfo? found = null;
        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                found = new FileInfo(path);
                break;
            }
        }

        if (found is null)
        {
            throw new FileNotFoundException(
                $"mock-layout.yaml 未找到,已搜索: {string.Join(", ", candidates)}");
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var text = File.ReadAllText(found.FullName);
        var layout = deserializer.Deserialize<MockLayout>(text)
            ?? throw new InvalidDataException("mock-layout.yaml 反序列化结果为空");

        layout.TempDir = Environment.ExpandEnvironmentVariables(layout.TempDir);
        Validate(layout);
        return layout;
    }

    private static IEnumerable<string> ResolveCandidates()
    {
        // 1) 工作目录下的 configs/mock-layout.yaml(便于 dotnet run 场景)
        yield return Path.Combine(Directory.GetCurrentDirectory(), "configs", "mock-layout.yaml");
        // 2) 与可执行文件并列的 Configs/mock-layout.yaml(<None Link="Configs\mock-layout.yaml" CopyToOutputDirectory="PreserveNewest">)
        yield return Path.Combine(AppContext.BaseDirectory, "Configs", "mock-layout.yaml");
    }

    private static void Validate(MockLayout layout)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(layout.TempDir))
        {
            errors.Add("tempDir 不能为空");
        }
        if (layout.Window.Width <= 0 || layout.Window.Height <= 0)
        {
            errors.Add($"window 尺寸非法: {layout.Window.Width}x{layout.Window.Height}");
        }
        if (layout.StateTimings.PathfindingMs <= 0)
        {
            errors.Add($"stateTimings.pathfindingMs 必须 > 0,实际 {layout.StateTimings.PathfindingMs}");
        }

        if (errors.Count > 0)
        {
            throw new InvalidDataException("mock-layout.yaml 校验失败: " + string.Join("; ", errors));
        }
    }
}
