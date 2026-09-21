// DH2.Tests — L1 单元测试
// RJ-S4-03 轮询谓词接缝独立验证 UT(测试 Agent):
// 防御 e2e 状态轮询谓词漂移(DEF-S4-02 类教训);与 Dev A 的 3 个实现侧 UT
// (Pathfinding/Idle 不截止、Pathfinding→Arrived 切换)在 E2ePollStateToPhaseTests 中
// 互为独立验证:
// - Dev A 测"实现" —— 断言基本契约;
// - 本类测"接缝" —— 精确字面量匹配 + 防御 + 取消 + 文件异常 + 全状态机循环。

using System.Diagnostics;
using DH2.App.Commands;
using Xunit;

namespace DH2.Tests.Unit.Commands;

public class E2ePollStateToPhaseBehaviorTests : IDisposable
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

    private string WriteStateJson(string state)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dh2-rjs403-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, $"{{\"state\":\"{state}\",\"counter\":0,\"ts\":\"\"}}");
        _tempFiles.Add(path);
        return path;
    }

    // ───── RJ-S4-03 接缝独立验证:谓词语义 ─────

    [Fact]
    public void PollStateToPhase_StateIsExactMatch_NotPrefixOrSubstring()
    {
        // 防御:state="Arrived Extra" 不应被识别为 "Arrived"(字面量匹配,不是前缀/包含)
        var statePath = WriteStateJson("Arrived Extra");
        var result = E2eCommand.PollStateToPhase(statePath, "Arrived", timeoutMs: 200, intervalMs: 30, CancellationToken.None);
        Assert.Null(result); // 超时返回 null(不是 "Arrived Extra")
    }

    [Fact]
    public void PollStateToPhase_AlreadyAtTarget_TerminatesImmediately()
    {
        // 边界:state 一开始就在 expectedState → 应立即返回(expectedState)
        var statePath = WriteStateJson("Arrived");
        var sw = Stopwatch.StartNew();
        var result = E2eCommand.PollStateToPhase(statePath, "Arrived", timeoutMs: 1000, intervalMs: 50, CancellationToken.None);
        sw.Stop();

        Assert.Equal("Arrived", result);
        Assert.True(sw.ElapsedMilliseconds < 200, $"应在 ~intervalMs 内立即返回,实际 {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void PollStateToPhase_TargetIdle_StaysPathfinding_DoesNotMatch()
    {
        // 谓词反向断言:target=Idle 但 state=Pathfinding → 不应匹配 Idle
        var statePath = WriteStateJson("Pathfinding");
        var result = E2eCommand.PollStateToPhase(statePath, "Idle", timeoutMs: 200, intervalMs: 30, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public void PollStateToPhase_TargetPathfinding_StaysArrived_DoesNotMatch()
    {
        // 防御:state 一直 Arrived,target=Pathfinding → 不应匹配(Pathfinding 是中间态而非 Arrived 后到达的状态)
        var statePath = WriteStateJson("Arrived");
        var result = E2eCommand.PollStateToPhase(statePath, "Pathfinding", timeoutMs: 200, intervalMs: 30, CancellationToken.None);
        Assert.Null(result);
    }

    // ───── 取消令牌与异常 ─────

    [Fact]
    public void PollStateToPhase_CancellationRequested_ThrowsOperationCanceledException()
    {
        // 取消令牌:cancel 后应抛 OperationCanceledException(不静默返 null)
        // 实际抛的是 TaskCanceledException(继承自 OperationCanceledException);
        // 用 ThrowsAny 接受子类。
        var statePath = WriteStateJson("Pathfinding");
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // 50ms 后取消

        Assert.ThrowsAny<OperationCanceledException>(() =>
            E2eCommand.PollStateToPhase(statePath, "Arrived", timeoutMs: 10000, intervalMs: 30, cts.Token));
    }

    // ───── 文件异常 ─────

    [Fact]
    public void PollStateToPhase_FileNotExists_KeepsPollingUntilTimeout()
    {
        // 防御:文件不存在 → 应持续轮询直到超时,返 null(不抛)
        var path = Path.Combine(Path.GetTempPath(), $"dh2-nonexistent-{Guid.NewGuid():N}.json");

        var sw = Stopwatch.StartNew();
        var result = E2eCommand.PollStateToPhase(path, "Arrived", timeoutMs: 250, intervalMs: 30, CancellationToken.None);
        sw.Stop();

        Assert.Null(result);
        Assert.True(sw.ElapsedMilliseconds >= 200, $"应在 ~timeoutMs 内返回 null,实际 {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void PollStateToPhase_EmptyFile_KeepsPollingUntilTimeout()
    {
        // 防御:文件存在但为空 → SafeReadState 抛 → 持续轮询直到超时
        var path = Path.Combine(Path.GetTempPath(), $"dh2-empty-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "");
        _tempFiles.Add(path);

        var result = E2eCommand.PollStateToPhase(path, "Arrived", timeoutMs: 200, intervalMs: 30, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public void PollStateToPhase_InvalidJson_KeepsPollingUntilTimeout()
    {
        // 防御:文件存在但 JSON 非法 → SafeReadState 抛 → 持续轮询直到超时
        var path = Path.Combine(Path.GetTempPath(), $"dh2-bad-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{not valid json");
        _tempFiles.Add(path);

        var result = E2eCommand.PollStateToPhase(path, "Arrived", timeoutMs: 200, intervalMs: 30, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public void PollStateToPhase_StateJsonMissingStateField_KeepsPollingUntilTimeout()
    {
        // 防御:合法 JSON 但缺 state 字段 → SafeReadState 返 null → 持续轮询直到超时
        var path = Path.Combine(Path.GetTempPath(), $"dh2-nostate-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{\"counter\":0,\"ts\":\"\"}");
        _tempFiles.Add(path);

        var result = E2eCommand.PollStateToPhase(path, "Arrived", timeoutMs: 200, intervalMs: 30, CancellationToken.None);
        Assert.Null(result);
    }

    // ───── 全状态机循环(模拟 MockGame 完整周期)──

    [Fact]
    public void PollStateToPhase_FullIdlePathfindingArrived_ReturnsArrived()
    {
        // 真机 MockGame 完整状态循环:Idle → Pathfinding → Arrived
        // 100ms 后切换到 Pathfinding,再 100ms 后切换到 Arrived
        var statePath = WriteStateJson("Idle");
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            File.WriteAllText(statePath, "{\"state\":\"Pathfinding\",\"counter\":0,\"ts\":\"\"}");
            await Task.Delay(100);
            File.WriteAllText(statePath, "{\"state\":\"Arrived\",\"counter\":0,\"ts\":\"\"}");
        });

        var sw = Stopwatch.StartNew();
        var result = E2eCommand.PollStateToPhase(statePath, "Arrived", timeoutMs: 2000, intervalMs: 30, CancellationToken.None);
        sw.Stop();

        Assert.Equal("Arrived", result);
        Assert.True(sw.ElapsedMilliseconds < 800, $"200ms 切换 + ~interval 应 < 800ms,实际 {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void PollStateToPhase_FastTransitionWithinInterval_Detects()
    {
        // 极速切换:50ms 后从 Pathfinding 切到 Arrived,interval=10ms 应能捕获
        var statePath = WriteStateJson("Pathfinding");
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            File.WriteAllText(statePath, "{\"state\":\"Arrived\",\"counter\":0,\"ts\":\"\"}");
        });

        var sw = Stopwatch.StartNew();
        var result = E2eCommand.PollStateToPhase(statePath, "Arrived", timeoutMs: 1000, intervalMs: 10, CancellationToken.None);
        sw.Stop();

        Assert.Equal("Arrived", result);
        Assert.True(sw.ElapsedMilliseconds < 300, $"50ms 切换 + 10ms 间隔应 < 300ms,实际 {sw.ElapsedMilliseconds}ms");
    }

    // ───── 字符串大小写敏感 ─────

    [Fact]
    public void PollStateToPhase_StateIsLowercaseArrived_DoesNotMatch()
    {
        // 防御:state="arrived"(小写)不应匹配 expectedState="Arrived"
        // 这是字面量匹配的硬约束(MockGame 状态机固定枚举字面值)
        var statePath = WriteStateJson("arrived");
        var result = E2eCommand.PollStateToPhase(statePath, "Arrived", timeoutMs: 200, intervalMs: 30, CancellationToken.None);
        Assert.Null(result);
    }
}
