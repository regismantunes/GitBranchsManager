using Gbm.Services.Git;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.BranchsCommands
{
	public class RemoveTaskBranchsCommand(IGitTool gitTool)
	{
        [CommandAsyncWithArgsBuilderAsync<BranchsCommandArgsBuilder>("-r", Description = "Remove local task branches", Example = "gbm -r <TaskId> [Repos...]")]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
				gitTool.SetRepository(repo);
                MyConsole.WriteStep($"→ Deleting local branch '{taskBranch}' from {repo}");
				await gitTool.DeleteLocalBranchAsync(taskBranch, cancellationToken);
			}

            MyConsole.WriteSucess("✅ Branch cleanup completed!");
			return 0;
		}
	}
}