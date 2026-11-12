using Gbm.Persistence.Entities;

namespace Gbm.Services.Tasks;

public record RepositoryBranchTarget(string Repository, string BranchName, string? OriginBranchName = null);
public record TaskBranchReference(TaskInfo? Task, string? Literal, bool MainAlias = false);

public interface ITaskResolver
{
    Task<TaskInfo> GetAsync(string taskId, CancellationToken cancellationToken = default);
    Task<TaskInfo> ResetAsync(string taskId, CancellationToken cancellationToken = default);
    Task<TaskBranchReference> ResolveReferenceAsync(string value, bool forceLiteral = false, CancellationToken cancellationToken = default);
}

public interface IBranchSelection
{
    Task<string> SelectAsync(string repository, IReadOnlyList<string> candidates, CancellationToken cancellationToken = default);
}

public interface ITaskBranchDiscovery
{
    Task<List<RepositoryBranch>> DiscoverAsync(string taskId, CancellationToken cancellationToken = default);
}

public interface ITaskBranchTargetResolver
{
    Task<RepositoryBranchTarget[]> ResolveAsync(TaskBranchReference reference, string[] repositories, string command,
        CancellationToken cancellationToken = default, IReadOnlySet<string>? existingPrRepositories = null);
    Task<RepositoryBranchTarget[]> WithOriginAsync(RepositoryBranchTarget[] targets, TaskBranchReference origin, CancellationToken cancellationToken = default);
}

public interface ITaskBranchCreator
{
    Task CreateAsync(TaskInfo task, string[] repositories, string? explicitBranch = null, CancellationToken cancellationToken = default);
}
