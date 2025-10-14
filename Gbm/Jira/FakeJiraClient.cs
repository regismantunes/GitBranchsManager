
using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories.Interfaces;

namespace Gbm.Jira
{
    public class FakeJiraClient(ITaskInfoRepository repository) : IJiraClient
    {
        private readonly ITaskInfoRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        public Task<TaskInfo?> GetTaskInfoAsync(string taskId, CancellationToken cancellationToken = default)
            => _repository.GetAsync(taskId, cancellationToken);
    }
}
