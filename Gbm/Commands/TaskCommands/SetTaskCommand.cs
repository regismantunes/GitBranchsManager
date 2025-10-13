using Gbm.Git;

namespace Gbm.Commands.TaskCommands
{
	public class SetTaskCommand : ITaskCommand
	{
		public async Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
                gitTool.SetRepository(repo);
                MyConsole.WriteStep($"→ Setting local branch '{taskBranch}' from {repo}");
				await gitTool.CheckoutAsync(taskBranch);
			}

            MyConsole.WriteSucess($"✅ Repositories setted to {taskBranch}!");
			return 0;
		}
	}
}