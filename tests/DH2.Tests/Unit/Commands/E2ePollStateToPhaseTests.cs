// DH2.Tests — L1 单元测试
// RJ-S4-03 状态轮询谓词 UT(Dev A 实现侧):
// - Pathfinding 不截止(中间态,不应被视为目标相位)
// - Arrived 截止(目标相位)
// - Idle / 其他中间态同样不截止
// 与现有 E2eCommandTests(命令层防御深度)和 QA 的 E2eButtonClickPointScaleTests(坐标接缝)互为独立验证。

using System.Diagnostics;
using DH2.App.Commands;
using Xunit;

namespace DH2.Tests.Unit.Commands;

public class E2ePollStateToPhaseTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            try { if (File.Exists(f)) File.Delete(f); }
            catch { /* best effort */ }
        }
        _tempFiles.Clear();
    }

    /// <summary>
    /// 写入临时 state.json,记录到 _tempFiles 以便 Dispose 清理。
    /// </summary>
    private string WriteStateJson(string state)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dh2-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, $"{{\"state\":\"{state}\",\"counter\":0,\"ts\":\"\"}}");
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public void E2e_PollStateToPhase_PathfindingOnly_DoesNotTerminate_ReturnsNull()
    {
        // 谓词:Arrived 才算截止;Pathfinding 是中间态,即使 state.json 一直停留在 Pathfinding,
        // 轮询 5s 也不会满足,超时返回 null。
        var statePath = WriteStateJson("Pathfinding");

        var sw = Stopwatch.StartNew();
        var result = E2eCommand.PollStateToPhase(
            statePath,
            expectedState: "Arrived",
            timeoutMs: 300,
            intervalMs: 50,
            ct: CancellationToken.None);
        sw.Stop();

        Assert.Null(result);
        Assert.True(
            sw.ElapsedMilliseconds >= 250,
            $"应在 ~timeoutMs 内返回;实际 {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void E2e_PollStateToPhase_IdleOnly_DoesNotTerminate_ReturnsNull()
    {
        // 防御:Idle(初始相位)不应该是终止;Idle 是确保 MockGame 已就绪的前置相位(EnsureIdle 单独处理),
        // 与 step 7 的目标相位 Arrived 是不同概念。
        var statePath = WriteStateJson("Idle");

        var result = E2eCommand.PollStateToPhase(
            statePath,
            expectedState: "Arrived",
            timeoutMs: 200,
            intervalMs: 50,
            ct: CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public void E2e_PollStateToPhase_PathfindingThenArrived_ReturnsArrived()
    {
        // 路径:state.json 初始 Pathfinding,模拟 MockGame 状态机,100ms 后切换到 Arrived。
        // 轮询超时给 2s、间隔 30ms,确保有时间感知切换;断言应在 ~300ms 内返回 Arrived。
        var statePath = WriteStateJson("Pathfinding");

        // 启动后台切换任务(模拟 MockGame 寻路结束 → Arrived)
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            File.WriteAllText(statePath, "{\"state\":\"Arrived\",\"counter\":0,\"ts\":\"\"}");
        });

        var sw = Stopwatch.StartNew();
        var result = E2eCommand.PollStateToPhase(
            statePath,
            expectedState: "Arrived",
            timeoutMs: 2000,
            intervalMs: 30,
            ct: CancellationToken.None);
        sw.Stop();

        Assert.Equal("Arrived", result);
        Assert.True(
            sw.ElapsedMilliseconds < 1000,
            $"应在 1s 内返回 Arrived(100ms 切换 + 几次轮询);实际 {sw.ElapsedMilliseconds}ms");
    }
}
