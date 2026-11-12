using Gbm.Persistence.Entities;

namespace Gbm.Services.Jira
{
    public interface IJiraClient
    {
        Task<TaskInfo?> GetTaskInfoFromJiraAsync(string taskId, CancellationToken cancellationToken = default);
    }
}
