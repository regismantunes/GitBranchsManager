using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
	public class NewTaskBranchesCommand(IGitTool gitTool)
	{
		[CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-n",
			Description = "Create new branches for the task",
			Example = "gbm -n <TaskId> [Repos...]",
			Group = CommandGroups.Branches,
			Order = 0)]
		public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
		{
			gitTool.ShowGitOutput = true;
            MyConsole.WriteCommandHeader("🌱 Creating new task branches...");
            foreach (var repo in repositories)
			{
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                await gitTool.SetRepositoryAsync(repo, cancellationToken);

                if (await gitTool.BranchExistsAsync(taskBranch, cancellationToken))
				{
                    MyConsole.WriteStep($"→ Branch '{taskBranch}' already exists");
                    await gitTool.CheckoutAsync(taskBranch, cancellationToken);
                    continue;
                }

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