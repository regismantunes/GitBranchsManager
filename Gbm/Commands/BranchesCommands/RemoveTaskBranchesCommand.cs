using Gbm.Services.Git;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands;

public class RemoveTaskBranchesCommand(IGitTool gitTool)
{
    [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-r", Description = "Delete local mapped task branches, retaining saved mappings",
        Example = "gbm -r <TaskId> [Repos...]", Group = CommandGroups.Branches, Order = 6)]
    public async Task<int> ExecuteAsync(RepositoryBranchTarget[] targets, CancellationToken cancellationToken = default)
    {
        MyConsole.WriteCommandHeader("🧹 Removing local task branches...");
        await BranchExecution.ForEachAsync(targets, async target =>
        {
            await gitTool.SetRepositoryAsync(target.Repository, cancellationToken);
            await gitTool.DeleteLocalBranchAsync(target.BranchName, cancellationToken);
        });
        MyConsole.WriteSucess("✅ Local task branches removed.");
        return 0;
    }
}
