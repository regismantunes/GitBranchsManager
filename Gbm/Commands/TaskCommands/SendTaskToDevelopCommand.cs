using Gbm.Git;

namespace Gbm.Commands.TaskCommands
{
	public class SendTaskToDevelopCommand : ITaskCommand
	{
		public async Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
				if (repo.EndsWith("sdk", StringComparison.OrdinalIgnoreCase)) continue;
                gitTool.SetRepository(repo);
                var branchExists = await gitTool.BranchExistsAsync(taskBranch);
				if (!branchExists) continue;
				MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
				
				MyConsole.WriteStep("→ Checking out 'develop' branch");
				await gitTool.CheckoutAsync("develop");

                MyConsole.WriteStep("→ Pulling latest changes from 'develop'");
				await gitTool.PullAsync();

                MyConsole.WriteStep($"→ Merging branch '{taskBranch}'");
				await gitTool.PullOriginAsync(taskBranch);

                MyConsole.WriteStep("→ Pushing changes to remote 'develop' branch");
				await gitTool.PushAsync();
			}

            MyConsole.WriteSucess("✅ Changes were moved to develop successfully!");
			return 0;
		}
	}
}