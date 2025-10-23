using System.Collections.Concurrent;
using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories.Interfaces;

namespace Gbm.Unit.Tests.Shared.Fakes;

public class FakeTaskInfoRepository : ITaskInfoRepository
{
    private readonly ConcurrentDictionary<string, TaskInfo> _store = new();

    public Task<TaskInfo?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(taskId, out var value);
        return Task.FromResult<TaskInfo?>(value);
    }

    public Task SaveAsync(string taskId, string taskSummary, string taskDescription, string taskBranch, CancellationToken cancellationToken = default)
    {
        _store[taskId] = new TaskInfo(taskId, taskSummary, taskDescription, $"http://fake/{taskId}", taskBranch);
        return Task.CompletedTask;
    }

    public void Seed(TaskInfo info)
    {
        _store[info.Id] = info;
    }
}
