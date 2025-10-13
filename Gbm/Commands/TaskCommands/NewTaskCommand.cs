using Gbm.Git;

namespace Gbm.Commands.TaskCommands
{
	public class NewTaskCommand : ITaskCommand
	{
		public async Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories)
		{
			gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                gitTool.SetRepository(repo);

                MyConsole.WriteStep($"→ Checking out to main branch");
				await gitTool.CheckoutToMainAsync();

				MyConsole.WriteStep("→ Pulling latest changes");
				await gitTool.PullAsync();

                MyConsole.WriteStep($"→ Creating new branch '{taskBranch}'");
				await gitTool.CheckoutNewBranchAsync(taskBranch);
			}

            MyConsole.WriteSucess("✅ New branches were successfully created!");
			return 0;
		}
	}
}