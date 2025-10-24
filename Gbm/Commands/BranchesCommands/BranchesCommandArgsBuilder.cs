using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Git;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Commands.BranchesCommands
{
    public class BranchesCommandArgsBuilder(ITaskInfoRepository taskInfoRepository, IGitTool gitTool) : IArgsBuilderAsync
    {
        public async Task<IDictionary<string, object>> BuildAsync(string[] args, CancellationToken cancellationToken = default)
        {
            if (args == null ||
                args.Length == 0)
                throw new ArgumentException("Arguments cannot be null or empty.", nameof(args));

            var command = args[0].ToLower();
            if (args.Length < 2) throw new ArgsValidationException($"Missing TaskId. Example: gbm {command} TaskId [Repo1 Repo2 ...]");
            if (command == "-n" && args.Length < 3) throw new ArgsValidationException("Missing repositories. Example: gbm -n TaskId Repo1 Repo2");

            // Get task branch
            var taskId = args[1];
            var taskInfo = await taskInfoRepository.GetAsync(taskId, cancellationToken);
            if (taskInfo == null)
                throw new ArgsValidationException($"Task with id '{taskId}' not found. You need firt to inform task details. Exmaple: gbm -t TaskId");
            if (string.IsNullOrWhiteSpace(taskInfo.BranchName))
                throw new ArgsValidationException($"Task with id '{taskId}' does not have a branch name defined. You need firt to inform task details. Exmaple: gbm -t TaskId");

            // Get repositories
            var repositories = args.Length > 2 ? args[2..] : null;

            if (repositories is null)
            {
                repositories = [.. (await gitTool.GetRepositoriesWithBranchAsync(taskInfo.BranchName, cancellationToken))];
                if (repositories.Length == 0)
                    throw new ArgsValidationException($"No repositories found with branch '{taskInfo.BranchName}'.");
            }

            return new Dictionary<string, object>
            {
                { "TaskBranch",  taskInfo.BranchName },
                { "Repositories", repositories }
            };
        }
    }
}
