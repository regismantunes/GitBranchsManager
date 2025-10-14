using Gbm.Persistence.Repositories.Interfaces;

namespace Gbm.Commands.PullRequestCommands
{
    public class ListPullRequestInfoByTaskIdCommand
    {
        public async Task<int> ExecuteAsync(string taskId, ITaskInfoRepository taskInfoRepository, IPullRequestInfoRepository pullRequestInfoRepository, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(taskId))
                throw new ArgumentException("Task ID cannot be null or empty.", nameof(taskId));

            var pullRequests = await pullRequestInfoRepository.GetByTaskIdAsync(taskId, cancellationToken);
            if (pullRequests.Count == 0)
            {
                MyConsole.WriteError($"❌ No pull requests found for task ID: {taskId}");
                return 1;
            }

            var taskInfo = await taskInfoRepository.GetAsync(taskId, cancellationToken);
            MyConsole.WriteInfo($"[{taskId}] {taskInfo?.Summary}");
            foreach (var pr in pullRequests)
            {
                MyConsole.WriteInfo($"- {pr.Repository}: {pr.Url}");
            }
            return 0;
        }
    }
}
