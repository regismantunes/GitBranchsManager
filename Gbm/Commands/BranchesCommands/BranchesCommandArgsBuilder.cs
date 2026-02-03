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
            if (taskInfo == null && command != "-s")
                throw new ArgsValidationException($"Task with id '{taskId}' not found. You need firt to inform task details. Exmaple: gbm -t TaskId");
            
            var branchName = taskInfo == null ? taskId.ToLower() : taskInfo.BranchName;
            if (string.IsNullOrWhiteSpace(branchName))
                throw new ArgsValidationException($"Task with id '{taskId}' does not have a branch name defined. You need firt to inform task details. Exmaple: gbm -t TaskId");

            var repositoriesIndex = 2;
            string? branchOrigin = null;
            if (command == "-u" && args.Length > 2)
            {
                var originCommand = args[2].ToLower();
                if (originCommand == "--origin")
                {
                    var originTaskId = args.Length > 3 ? args[3] : throw new ArgsValidationException("Missing Origin TaskId. Example: gbm -u TaskId --origin OriginTaskId [Repo1 Repo2 ...]");
                    var originBranchInfo = await taskInfoRepository.GetAsync(originTaskId, cancellationToken);
                    branchOrigin = originBranchInfo == null ? originTaskId.ToLower() : originBranchInfo.BranchName;
                    repositoriesIndex = 4;
                }
            }

            // Get repositories
            var repositories = args.Length > repositoriesIndex ? args[repositoriesIndex..] : null;

            if (repositories is null)
            {
                if (branchName == "main" || branchName == "master")
                    repositories = await gitTool.GetAllRepositoriesAsync(cancellationToken).ToArrayAsync(cancellationToken);
                else
                    repositories = [.. await gitTool.GetRepositoriesWithBranchAsync(branchName, cancellationToken)];

                if (repositories.Length == 0)
                    throw new ArgsValidationException($"No repositories found with branch '{branchName}'.");
            }

            return new Dictionary<string, object>
            {
                { "TaskBranch",  branchName },
                { "Repositories", repositories },
                { "BranchOrigin", branchOrigin }
            };
        }
    }
}
