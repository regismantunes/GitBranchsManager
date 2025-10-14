using Gbm.Persistence.Entities;

namespace Gbm.Persistence.Repositories.Interfaces
{
    public interface IPullRequestInfoRepository
    {
        Task<PullRequestInfo?> GetAsync(string taskId, string repository, CancellationToken cancellationToken = default);
        Task SaveAsync(string taskId, string repository, int number, string url, CancellationToken cancellationToken = default);
        Task SaveAsync(PullRequestInfo prInfo, CancellationToken cancellationToken = default);
        Task<List<PullRequestInfo>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string taskId, string repository, CancellationToken cancellationToken = default);
    }
}