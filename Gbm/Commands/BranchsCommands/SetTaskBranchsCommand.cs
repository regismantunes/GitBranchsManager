using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchsCommands
{
    public class SetTaskBranchsCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchsCommandArgsBuilder>("-s",
            Description = "Checkout task branches",
            Example = "gbm -s <TaskId> [Repos...]",
            Group = CommandGroups.Branchs,
            Order = 2)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            gitTool.ShowGitOutput = true;
            MyConsole.WriteCommandHeader("🔀 Checking out task branches...");
            foreach (var repo in repositories)
            {
                gitTool.SetRepository(repo);
                MyConsole.WriteStep($"→ Setting local branch '{taskBranch}' from {repo}");
                await gitTool.CheckoutAsync(taskBranch, cancellationToken);
            }

            MyConsole.WriteSucess($"✅ Repositories setted to {taskBranch}!");
            return 0;
        }
    }
}