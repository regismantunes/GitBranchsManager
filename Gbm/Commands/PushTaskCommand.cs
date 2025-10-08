using Gbm.Git;

namespace Gbm.Commands
{
	public class PushTaskCommand : ITaskCommand
	{
		public int Execute(GitTool gitTool, string taskBranch, string[] repositories)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                gitTool.SetRepository(repo);

                MyConsole.WriteStep($"→ Checking out to '{taskBranch}'");
                gitTool.Checkout(taskBranch);
                
				MyConsole.WriteStep($"→ Pushing '{taskBranch}'");
                gitTool.Push();
			}

            MyConsole.WriteSucess("✅ Branch's task pushed!");
			return 0;
		}
	}
}