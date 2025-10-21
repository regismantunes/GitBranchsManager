using Gbm.Services.Git;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.BranchsCommands
{
	public class SendTaskBranchsToDevelopCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchsCommandArgsBuilder>("-d", Description = "Merge task into develop and push", Example = "gbm -d <TaskId> [Repos...]")]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
				if (repo.EndsWith("sdk", StringComparison.OrdinalIgnoreCase)) continue;
                gitTool.SetRepository(repo);
                var branchExists = await gitTool.BranchExistsAsync(taskBranch, cancellationToken);
				if (!branchExists) continue;
				MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
				
				MyConsole.WriteStep("→ Checking out 'develop' branch");
				await gitTool.CheckoutAsync("develop", cancellationToken);

                MyConsole.WriteStep("→ Pulling latest changes from 'develop'");
				await gitTool.PullAsync(cancellationToken);

                MyConsole.WriteStep($"→ Merging branch '{taskBranch}'");
				await gitTool.PullOriginAsync(taskBranch, cancellationToken);

                MyConsole.WriteStep("→ Pushing changes to remote 'develop' branch");
				await gitTool.PushAsync(cancellationToken);
			}

            MyConsole.WriteSucess("✅ Changes were moved to develop successfully!");
			return 0;
		}
	}
}