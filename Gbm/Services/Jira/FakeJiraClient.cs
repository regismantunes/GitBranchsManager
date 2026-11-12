using Gbm.Persistence.Entities;

namespace Gbm.Services.Jira
{
    public class FakeJiraClient : IJiraClient
    {
        public Task<TaskInfo?> GetTaskInfoFromJiraAsync(string taskId, CancellationToken cancellationToken = default)
            => Task.FromResult<TaskInfo?>(null);
    }
}
