using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
    public class SetTaskBranchesCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-s",
            Description = "Checkout to task branches or common branches",
            Example = "gbm -s <TaskId/Branch> [Repos...]",
            Group = CommandGroups.Branches,
            Order = 2)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            gitTool.ShowGitOutput = true;
            MyConsole.WriteCommandHeader("🔀 Checking out task branches...");
            foreach (var repo in repositories)
            {
                await gitTool.SetRepositoryAsync(repo, cancellationToken);
                MyConsole.WriteStep($"→ Setting local branch '{taskBranch}' from {repo}");
                if (taskBranch == "main" || taskBranch == "master")
                    await gitTool.CheckoutToMainAsync(cancellationToken);
                else if (!await gitTool.CheckoutAsync(taskBranch, cancellationToken))
                    continue;
                MyConsole.WriteStep($"→ Pulling latest changes for branch '{taskBranch}' from {repo}");
                await gitTool.PullAsync(cancellationToken);
            }

            MyConsole.WriteSucess($"✅ Repositories setted to {taskBranch}!");
            return 0;
        }
    }
}