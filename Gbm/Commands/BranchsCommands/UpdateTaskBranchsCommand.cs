using Gbm.Services.Git;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.BranchsCommands
{
    public class UpdateTaskBranchsCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchsCommandArgsBuilder>("-u", Description = "Update task branches from base", Example = "gbm -u <TaskId> [Repos...]")]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
            {
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                gitTool.SetRepository(repo);
                MyConsole.WriteStep($"→ Checking out to '{taskBranch}'");
                await gitTool.CheckoutAsync(taskBranch, cancellationToken);
                MyConsole.WriteStep($"→ Updating '{taskBranch}' from {repo}");
                await gitTool.GetMainChangesAsync(cancellationToken);
            }

            MyConsole.WriteSucess($"✅ Branches from task {taskBranch} were updated!.");
            return 0;
        }
    }
}