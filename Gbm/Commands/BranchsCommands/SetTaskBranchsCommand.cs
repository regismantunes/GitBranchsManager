using Gbm.Services.Git;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.BranchsCommands
{
	public class SetTaskBranchsCommand(IGitTool gitTool)
	{
        [CommandAsyncWithArgsBuilderAsync<BranchsCommandArgsBuilder>("-s", Description = "Checkout task branches", Example = "gbm -s <TaskId> [Repos...]")]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
                gitTool.SetRepository(repo);
                MyConsole.WriteStep($"→ Setting local branch '{taskBranch}' from {repo}");
				await gitTool.CheckoutAsync(taskBranch, cancellationToken);
			}

            MyConsole.WriteSucess($"✅ Repositories setted to {taskBranch}!");
			return 0;
		}
	}
}