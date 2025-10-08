using Gbm.Git;

namespace Gbm.Commands
{
	public class SendTaskToDevelopCommand : ITaskCommand
	{
		public int Execute(GitTool gitTool, string taskBranch, string[] repositories)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
				if (repo.EndsWith("sdk", StringComparison.OrdinalIgnoreCase)) continue;
                gitTool.SetRepository(repo);
                var branchExists = gitTool.BranchExists(taskBranch);
				if (!branchExists) continue;
				MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
				
				MyConsole.WriteStep("→ Checking out 'develop' branch");
				gitTool.Checkout("develop");

                MyConsole.WriteStep("→ Pulling latest changes from 'develop'");
				gitTool.Pull();

                MyConsole.WriteStep($"→ Merging branch '{taskBranch}'");
				gitTool.PullOrigin(taskBranch);

                MyConsole.WriteStep("→ Pushing changes to remote 'develop' branch");
				gitTool.Push();
			}

            MyConsole.WriteSucess("✅ Changes were moved to develop successfully!");
			return 0;
		}
	}
}