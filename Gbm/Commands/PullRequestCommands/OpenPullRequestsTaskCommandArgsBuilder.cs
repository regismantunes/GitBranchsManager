using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Git;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Commands.PullRequestCommands
{
    public class OpenPullRequestsTaskCommandArgsBuilder(ITaskInfoRepository taskInfoRepository, IGitTool gitTool) : IArgsBuilderAsync
    {
        public async Task<IDictionary<string, object>> BuildAsync(string[] args, CancellationToken cancellationToken = default)
        {
            if (args == null ||
                args.Length == 0)
                throw new ArgumentException("Arguments cannot be null or empty.", nameof(args));

            var command = args[0].ToLower();
            if (args.Length < 2) throw new ArgsValidationException($"Missing TaskId. Example: gbm {command} TaskId [Repo1 Repo2 ...]");

            var parameters = new Dictionary<string, object>();
            
            var taskId = args[1];
            parameters.Add("TaskId", taskId);

            string[]? repositories = null;

            if (args.Length > 2)
            {
                var i = 2;
                if (args[i].Equals("nopush", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add("PushLocalChanges", false);
                    i++;
                }
                repositories = args.Length > i ? args[i..] : null;
            }

            if (repositories is null)
            {
                var taskInfo = await taskInfoRepository.GetAsync(taskId, cancellationToken);
                repositories = [.. (await gitTool.GetRepositoriesWithBranchAsync(taskInfo.BranchName, cancellationToken))];
                if (repositories.Length == 0)
                    throw new ArgsValidationException($"No repositories found with branch '{taskInfo.BranchName}'.");
            }
            parameters.Add("Repositories", repositories);

            return parameters;
        }
    }
}
