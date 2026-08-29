using System.Collections.Concurrent;

namespace Xylab.Management.WebDeploy;

public sealed record TraceResult(IReadOnlyList<int> IncompleteObjectIds);

public sealed class TraceSessionCoordinator
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<TraceResult>> _sessions = new(StringComparer.Ordinal);

    public Task<TraceResult> WaitAsync(string requestId, CancellationToken cancellationToken)
    {
        var source = _sessions.GetOrAdd(requestId, CreateSource);
        return source.Task.WaitAsync(TimeSpan.FromMinutes(2), cancellationToken);
    }

    public void Complete(string requestId, TraceResult result)
    {
        var source = _sessions.GetOrAdd(requestId, CreateSource);
        source.TrySetResult(result);
    }

    public void Remove(string requestId)
    {
        _sessions.TryRemove(requestId, out _);
    }

    private static TaskCompletionSource<TraceResult> CreateSource(string _)
    {
        return new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
