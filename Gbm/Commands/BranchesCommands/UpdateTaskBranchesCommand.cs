using Gbm.Services.Git;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands;

public class UpdateTaskBranchesCommand(IGitTool gitTool)
{
    [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-u", Description = "Update mapped task branches from main or a per-repository origin",
        Example = "gbm -u <TaskId> [--origin <TaskId/Branch>|--origin-branch <Branch>] [Repos...]", Group = CommandGroups.Branches, Order = 3)]
    public async Task<int> ExecuteAsync(RepositoryBranchTarget[] targets, CancellationToken cancellationToken = default)
    {
        await BranchExecution.ForEachAsync(targets, async target =>
        {
            await BranchExecution.CheckoutRequiredAsync(gitTool, target, cancellationToken);
            if (target.OriginBranchName is null) await gitTool.GetMainChangesAsync(cancellationToken);
            else await gitTool.PullOriginAsync(target.OriginBranchName, cancellationToken);
        });
        return 0;
    }
}
