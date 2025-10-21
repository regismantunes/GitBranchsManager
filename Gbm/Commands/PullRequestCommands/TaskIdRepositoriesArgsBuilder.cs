using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Git;
using RA.Console.DependecyInjection.Args;

namespace Gbm.Commands.PullRequestCommands
{
    public class TaskIdRepositoriesArgsBuilder(ITaskInfoRepository taskInfoRepository, IGitTool gitTool) : IArgsBuilderAsync
    {
        public async Task<IDictionary<string, object>> BuildAsync(string[] args, CancellationToken cancellationToken = default)
        {
            if (args == null ||
                args.Length == 0)
                throw new ArgumentException("Arguments cannot be null or empty.", nameof(args));

            var command = args[0].ToLower();
            if (args.Length < 2) throw new ArgsValidationException($"Missing TaskId. Example: gbm {command} TaskId [Repo1 Repo2 ...]");

            var taskId = args[1];
            var repositories = args.Length > 2 ? args[2..] : null;

            if (repositories is null)
            {
                var taskInfo = await taskInfoRepository.GetAsync(taskId, cancellationToken);
                repositories = [.. (await gitTool.GetRepositoriesWithBranchAsync(taskInfo.BranchName, cancellationToken))];
                if (repositories.Length == 0)
                    throw new ArgsValidationException($"No repositories found with branch '{taskInfo.BranchName}'.");
            }

            return new Dictionary<string, object>
            {
                { "TaskId", taskId },
                { "Repositories", repositories }
            };
        }
    }
}
