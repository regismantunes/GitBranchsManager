using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Jira;
using System.Text.RegularExpressions;

namespace Gbm.Services.Tasks;

public sealed partial class TaskResolver(ITaskInfoRepository repository, IJiraClient jiraClient, ITaskBranchDiscovery discovery) : ITaskResolver
{
    public async Task<TaskInfo> GetAsync(string taskId, CancellationToken cancellationToken = default)
        => await repository.GetAsync(taskId, cancellationToken) ?? await ImportAsync(taskId, null, cancellationToken);

    public async Task<TaskInfo> ResetAsync(string taskId, CancellationToken cancellationToken = default)
        => await ImportAsync(taskId, await repository.GetAsync(taskId, cancellationToken), cancellationToken);

    private async Task<TaskInfo> ImportAsync(string taskId, TaskInfo? existing, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taskId)) throw new ArgumentException("Task ID cannot be empty.", nameof(taskId));
        var metadata = existing ?? await jiraClient.GetTaskInfoFromJiraAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task '{taskId}' could not be loaded from Jira. Configure Jira credentials or register metadata using -t ... norepo.");
        var branches = await discovery.DiscoverAsync(metadata.Id, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = metadata with { Repositories = branches };
        foreach (var branch in branches)
        {
            var old = existing?.Repositories.Find(r => r.Repository.Equals(branch.Repository, StringComparison.OrdinalIgnoreCase));
            var state = old is null ? "added" : old.BranchName == branch.BranchName ? "unchanged" : $"changed from {old.BranchName}";
            MyConsole.WriteInfo($"{branch.Repository} -> {branch.BranchName} ({state})");
        }
        foreach (var removed in existing?.Repositories ?? [])
            if (!branches.Any(r => r.Repository.Equals(removed.Repository, StringComparison.OrdinalIgnoreCase)))
                MyConsole.WriteInfo($"Removed mapping: {removed.Repository} -> {removed.BranchName}");
        await repository.SaveSnapshotAsync(snapshot, cancellationToken);
        if (branches.Count == 0) MyConsole.WriteInfo($"Task '{metadata.Id}' was saved with no repository mappings. Use -n to create branches or -rb to rediscover them.");
        return snapshot;
    }

    public async Task<TaskBranchReference> ResolveReferenceAsync(string value, bool forceLiteral = false, CancellationToken cancellationToken = default)
    {
        if (forceLiteral) return new(null, value);
        var saved = await repository.GetAsync(value, cancellationToken);
        if (saved is not null) return new(saved, null);
        if (TaskIdPattern().IsMatch(value)) return new(await GetAsync(value, cancellationToken), null);
        return new(null, value, value is "main" or "master");
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]*-[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex TaskIdPattern();
}
