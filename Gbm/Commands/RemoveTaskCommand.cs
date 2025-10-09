using Gbm.Git;

namespace Gbm.Commands
{
	public class RemoveTaskCommand : ITaskCommand
	{
		public async Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
				gitTool.SetRepository(repo);
                MyConsole.WriteStep($"→ Deleting local branch '{taskBranch}' from {repo}");
				await gitTool.DeleteLocalBranchAsync(taskBranch);
			}

            MyConsole.WriteSucess("✅ Branch cleanup completed!");
			return 0;
		}
	}
}