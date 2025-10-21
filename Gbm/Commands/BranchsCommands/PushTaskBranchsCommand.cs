using Gbm.Services.Git;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.BranchsCommands
{
	public class PushTaskBranchsCommand(IGitTool gitTool)
	{
        [CommandAsyncWithArgsBuilderAsync<BranchsCommandArgsBuilder>("-p", Description = "Push task branches", Example = "gbm -p <TaskId> [Repos...]")]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
		{
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
			{
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                gitTool.SetRepository(repo);

                MyConsole.WriteStep($"→ Checking out to '{taskBranch}'");
                await gitTool.CheckoutAsync(taskBranch, cancellationToken);
                
				MyConsole.WriteStep($"→ Pushing '{taskBranch}'");
                await gitTool.PushAsync(cancellationToken);
			}

            MyConsole.WriteSucess("✅ Branch's task pushed!");
			return 0;
		}
	}
}