using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
    public class SetTaskBranchesCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-s",
            Description = "Checkout task branches",
            Example = "gbm -s <TaskId> [Repos...]",
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
                await gitTool.CheckoutAsync(taskBranch, cancellationToken);
            }

            MyConsole.WriteSucess($"✅ Repositories setted to {taskBranch}!");
            return 0;
        }
    }
}