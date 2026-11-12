using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories.Interfaces;
using System.Text.Json;

namespace Gbm.Persistence.Repositories;

public sealed class TaskInfoRepository : ITaskInfoRepository
{
    private readonly string _jiraDomain;
    private readonly string _jsonTaskFile;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public TaskInfoRepository(string jiraDomain, string jsonTaskFile)
    {
        _jiraDomain = jiraDomain ?? throw new ArgumentNullException(nameof(jiraDomain));
        _jsonTaskFile = jsonTaskFile ?? throw new ArgumentNullException(nameof(jsonTaskFile));
        if (!Path.IsPathFullyQualified(jsonTaskFile))
            throw new ArgumentException("Json task file invalid path.", nameof(jsonTaskFile));
    }

    public async Task<TaskInfo?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { return (await ReadAsync(cancellationToken)).Find(t => Same(t.Id, taskId)); }
        finally { _gate.Release(); }
    }

    public Task SaveMetadataAsync(string taskId, string summary, string description, CancellationToken cancellationToken = default)
        => UpdateAsync(taskId, previous => new TaskInfo(previous?.Id ?? taskId, summary, description,
            GetTaskUrl(previous?.Id ?? taskId), previous?.Repositories.ToList() ?? []), cancellationToken);

    public Task UpsertRepositoryBranchAsync(string taskId, RepositoryBranch repositoryBranch, CancellationToken cancellationToken = default)
        => UpdateAsync(taskId, previous =>
        {
            if (previous is null) throw new InvalidOperationException($"Task '{taskId}' is not saved.");
            var branches = previous.Repositories.Where(r => !Same(r.Repository, repositoryBranch.Repository)).ToList();
            branches.Add(repositoryBranch);
            return previous with { Repositories = branches };
        }, cancellationToken);

    public Task SaveSnapshotAsync(TaskInfo taskInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskInfo);
        Validate([taskInfo]);
        var snapshot = taskInfo with { Repositories = taskInfo.Repositories.ToList() };
        return UpdateAsync(taskInfo.Id, previous => snapshot with { Id = previous?.Id ?? snapshot.Id }, cancellationToken);
    }

    private async Task UpdateAsync(string taskId, Func<TaskInfo?, TaskInfo> update, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        string? temporary = null;
        try
        {
            var tasks = await ReadAsync(cancellationToken);
            var index = tasks.FindIndex(t => Same(t.Id, taskId));
            var updated = update(index < 0 ? null : tasks[index]);
            if (index < 0) tasks.Add(updated); else tasks[index] = updated;
            Validate(tasks);
            temporary = Path.Combine(Path.GetDirectoryName(_jsonTaskFile)!, $".tasksinfo-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(tasks, JsonOptions), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, _jsonTaskFile, overwrite: true);
        }
        finally
        {
            try { if (temporary is not null && File.Exists(temporary)) File.Delete(temporary); }
            finally { _gate.Release(); }
        }
    }

    private async Task<List<TaskInfo>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_jsonTaskFile)) return [];
        try
        {
            var json = await File.ReadAllTextAsync(_jsonTaskFile, cancellationToken);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) throw InvalidFormat();
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object || entry.TryGetProperty("BranchName", out _) ||
                    !entry.TryGetProperty("Repositories", out var repositories) || repositories.ValueKind != JsonValueKind.Array)
                    throw InvalidFormat();
            }
            var tasks = JsonSerializer.Deserialize<List<TaskInfo>>(json) ?? throw InvalidFormat();
            Validate(tasks);
            return tasks;
        }
        catch (JsonException ex) { throw InvalidFormat(ex); }
    }

    private static void Validate(List<TaskInfo> tasks)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in tasks)
        {
            if (task is null || string.IsNullOrWhiteSpace(task.Id) || task.Summary is null ||
                task.Description is null || string.IsNullOrWhiteSpace(task.Url) || task.Repositories is null || !ids.Add(task.Id))
                throw InvalidFormat();
            var repos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var branch in task.Repositories)
            {
                if (branch is null || string.IsNullOrWhiteSpace(branch.Repository) || string.IsNullOrWhiteSpace(branch.BranchName) ||
                    branch.Repository is "." or ".." || branch.Repository.IndexOfAny(['/', '\\', ':']) >= 0 ||
                    branch.BranchName.StartsWith("refs/", StringComparison.Ordinal) || branch.BranchName.StartsWith("origin/", StringComparison.Ordinal) ||
                    !repos.Add(branch.Repository)) throw InvalidFormat();
            }
        }
    }

    private static InvalidDataException InvalidFormat(Exception? inner = null)
        => new("Invalid or incompatible tasksinfo.json. Preserve a backup and recreate the task data with this version; automatic migration is not supported.", inner);

    private static bool Same(string first, string second) => first.Equals(second, StringComparison.OrdinalIgnoreCase);
    public string GetTaskUrl(string taskId) => $"https://{_jiraDomain}.atlassian.net/browse/{taskId}";
}
