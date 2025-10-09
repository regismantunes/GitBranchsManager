using Gbm.Git;

namespace Gbm.Commands
{
	public class PushTaskCommand : ITaskCommand
	{
		public async Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                gitTool.SetRepository(repo);

                MyConsole.WriteStep($"→ Checking out to '{taskBranch}'");
                await gitTool.CheckoutAsync(taskBranch);
                
				MyConsole.WriteStep($"→ Pushing '{taskBranch}'");
                await gitTool.PushAsync();
			}

            MyConsole.WriteSucess("✅ Branch's task pushed!");
			return 0;
		}
	}
}