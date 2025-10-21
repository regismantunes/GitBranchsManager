using Gbm.Persistence.Entities;

namespace Gbm.Services.Jira
{
    public interface IJiraClient
    {
        Task<TaskInfo?> GetTaskInfoAsync(string taskId, CancellationToken cancellationToken = default);
    }
}
