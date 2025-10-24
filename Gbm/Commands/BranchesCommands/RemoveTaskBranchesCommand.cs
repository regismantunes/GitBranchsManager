using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
    public class RemoveTaskBranchesCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-r",
            Description = "Remove local task branches",
            Example = "gbm -r <TaskId> [Repos...]",
            Group = CommandGroups.Branches,
            Order = 6)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            gitTool.ShowGitOutput = true;
            MyConsole.WriteCommandHeader("🧹 Removing local task branches...");
            foreach (var repo in repositories)
            {
                gitTool.SetRepository(repo);
                MyConsole.WriteStep($"→ Deleting local branch '{taskBranch}' from {repo}");
                await gitTool.DeleteLocalBranchAsync(taskBranch, cancellationToken);
            }

            MyConsole.WriteSucess("✅ Branch cleanup completed!");
            return 0;
        }
    }
}