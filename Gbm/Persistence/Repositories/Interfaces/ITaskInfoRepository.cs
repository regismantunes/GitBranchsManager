using Gbm.Persistence.Entities;

namespace Gbm.Persistence.Repositories.Interfaces
{
    public interface ITaskInfoRepository
    {
        Task<TaskInfo?> GetAsync(string taskId, CancellationToken cancellationToken = default);
        Task SaveMetadataAsync(string taskId, string summary, string description, CancellationToken cancellationToken = default);
        Task UpsertRepositoryBranchAsync(string taskId, RepositoryBranch repositoryBranch, CancellationToken cancellationToken = default);
        Task SaveSnapshotAsync(TaskInfo taskInfo, CancellationToken cancellationToken = default);
    }
}
