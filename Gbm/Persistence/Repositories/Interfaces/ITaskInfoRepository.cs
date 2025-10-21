using Gbm.Persistence.Entities;

namespace Gbm.Persistence.Repositories.Interfaces
{
    public interface ITaskInfoRepository
    {
        Task<TaskInfo?> GetAsync(string taskId, CancellationToken cancellationToken = default);
        Task SaveAsync(string taskId, string taskSummary, string taskDescription, string taskBranch, CancellationToken cancellationToken = default);
    }
}