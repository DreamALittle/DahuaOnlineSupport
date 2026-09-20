using System.IO;
using System.Text.Json;
using DH2.MockGame.Models;

namespace DH2.MockGame.Services;

/// <summary>
/// state.json 原子写(技术设计 §7)。
/// 原子覆写 = 写临时文件 + File.Replace(Move 行为,同卷上为原子)。
/// </summary>
public sealed class StateStore
{
    private readonly string _path;
    private readonly object _gate = new();

    public StateStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = System.IO.Path.Combine(directory, "state.json");
    }

    public string Path => _path;

    /// <summary>
    /// 立即原子覆写。phase 仅作枚举传入便于调用方表达意图;序列化为规范字符串。
    /// </summary>
    public void Write(MockPhase phase, int counter)
    {
        var payload = new MockStatePayload
        {
            State = PhaseToString(phase),
            Counter = counter,
            Ts = DateTime.UtcNow.ToString("o"),
        };

        // File.Replace 在目标不存在时抛;首次创建走 Move。
        var json = JsonSerializer.Serialize(payload, StateJsonContext.Default.MockStatePayload);
        var tmp = _path + ".tmp";

        lock (_gate)
        {
            File.WriteAllText(tmp, json);
            if (File.Exists(_path))
            {
                File.Replace(tmp, _path, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tmp, _path);
            }
        }
    }

    /// <summary>进程启动时清理旧 state.json(技术设计 §7 启动时清理)。</summary>
    public void ClearOnStartup()
    {
        lock (_gate)
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }

    private static string PhaseToString(MockPhase phase) => phase switch
    {
        MockPhase.Idle => "Idle",
        MockPhase.Pathfinding => "Pathfinding",
        MockPhase.Arrived => "Arrived",
        _ => "Idle",
    };
}

/// <summary>
/// System.Text.Json 源生成上下文(技术设计 §4 编码规范:启用 AOT/性能优化)。
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(MockStatePayload))]
internal partial class StateJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
