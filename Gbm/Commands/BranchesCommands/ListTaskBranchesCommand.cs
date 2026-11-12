using Gbm.Services.Git;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands;

public class ListTaskBranchesCommand(IGitTool gitTool)
{
    [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-l", Description = "List local branches mapped to a task",
        Example = "gbm -l <TaskId> [Repos...]", Group = CommandGroups.Branches, Order = 1)]
    public async Task<int> ExecuteAsync(RepositoryBranchTarget[] targets, CancellationToken cancellationToken = default)
    {
        MyConsole.WriteCommandHeader("🧾 Listing repositories with task branches...");
        foreach (var target in targets)
        {
            await gitTool.SetRepositoryAsync(target.Repository, cancellationToken);
            if (await gitTool.BranchExistsAsync(target.BranchName, cancellationToken))
                MyConsole.WriteStep($"→ {target.Repository}: branch '{target.BranchName}' exists.");
        }
        return 0;
    }
}
