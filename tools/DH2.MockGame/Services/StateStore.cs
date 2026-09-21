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
    /// 使用匿名对象序列化,避免对 MockStatePayload 强依赖(RJ-S1-04 后 Models 仅保留 MockPhase 枚举)。
    /// </summary>
    public void Write(MockPhase phase, int counter)
    {
        var payload = new
        {
            state = PhaseToString(phase),
            counter,
            ts = DateTime.UtcNow.ToString("o"),
        };

        var json = JsonSerializer.Serialize(payload);
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
