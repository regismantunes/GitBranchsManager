using Gbm.Git;

namespace Gbm.Commands
{
	public class NewTaskCommand : ITaskCommand
	{
		public int Execute(GitTool gitTool, string taskBranch, string[] repositories)
		{
			gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                gitTool.SetRepository(repo);

                MyConsole.WriteStep($"→ Checking out to main branch");
				gitTool.CheckoutToMain();

				MyConsole.WriteStep("→ Pulling latest changes");
				gitTool.Pull();

                MyConsole.WriteStep($"→ Creating new branch '{taskBranch}'");
				gitTool.CheckoutNewBranch(taskBranch);
			}

            MyConsole.WriteSucess("✅ New branches were successfully created!");
			return 0;
		}
	}
}