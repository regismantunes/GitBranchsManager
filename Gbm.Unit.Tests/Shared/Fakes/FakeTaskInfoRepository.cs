using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories.Interfaces;

namespace Gbm.Unit.Tests.Shared.Fakes;

public class FakeTaskInfoRepository : ITaskInfoRepository
{
    private readonly Dictionary<string, TaskInfo> _store = new(StringComparer.OrdinalIgnoreCase);
    public int Writes { get; private set; }
    public string? FailUpsertRepository { get; set; }

    public Task<TaskInfo?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.TryGetValue(taskId, out var value) ? value with { Repositories = value.Repositories.ToList() } : null);
    }

    public Task SaveMetadataAsync(string taskId, string summary, string description, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryGetValue(taskId, out var previous);
        _store[taskId] = new(previous?.Id ?? taskId, summary, description, $"https://fake/{taskId}", previous?.Repositories.ToList() ?? []);
        Writes++;
        return Task.CompletedTask;
    }

    public Task UpsertRepositoryBranchAsync(string taskId, RepositoryBranch repositoryBranch, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (repositoryBranch.Repository == FailUpsertRepository) throw new IOException("Simulated save failure.");
        var previous = _store[taskId];
        _store[taskId] = previous with { Repositories = [.. previous.Repositories.Where(r => !r.Repository.Equals(repositoryBranch.Repository, StringComparison.OrdinalIgnoreCase)), repositoryBranch] };
        Writes++;
        return Task.CompletedTask;
    }

    public Task SaveSnapshotAsync(TaskInfo taskInfo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Seed(taskInfo);
        Writes++;
        return Task.CompletedTask;
    }

    public void Seed(TaskInfo info) => _store[info.Id] = info with { Repositories = info.Repositories.ToList() };
}
