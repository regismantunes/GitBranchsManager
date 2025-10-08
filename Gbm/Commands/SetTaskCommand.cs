using Gbm.Git;

namespace Gbm.Commands
{
	public class SetTaskCommand : ITaskCommand
	{
		public int Execute(GitTool gitTool, string taskBranch, string[] repositories)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
                gitTool.SetRepository(repo);
                MyConsole.WriteStep($"→ Setting local branch '{taskBranch}' from {repo}");
				gitTool.Checkout(taskBranch);
			}

            MyConsole.WriteSucess($"✅ Repositories setted to {taskBranch}!");
			return 0;
		}
	}
}