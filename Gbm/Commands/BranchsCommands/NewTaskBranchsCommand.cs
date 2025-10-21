using Gbm.Services.Git;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.BranchsCommands
{
	public class NewTaskBranchsCommand(IGitTool gitTool)
	{
		[CommandAsyncWithArgsBuilderAsync<BranchsCommandArgsBuilder>("-n",
			Description = "Create new branchs for the task",
			Example = "gbm -n <TaskId> [Repos...]",
			Group = CommandGroups.Branchs,
			Order = 0)]
		public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
		{
			gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                gitTool.SetRepository(repo);

                MyConsole.WriteStep($"→ Checking out to main branch");
				await gitTool.CheckoutToMainAsync(cancellationToken);

				MyConsole.WriteStep("→ Pulling latest changes");
				await gitTool.PullAsync(cancellationToken);

                MyConsole.WriteStep($"→ Creating new branch '{taskBranch}'");
				await gitTool.CheckoutNewBranchAsync(taskBranch, cancellationToken);
			}

            MyConsole.WriteSucess("✅ New branches were successfully created!");
			return 0;
		}
	}
}