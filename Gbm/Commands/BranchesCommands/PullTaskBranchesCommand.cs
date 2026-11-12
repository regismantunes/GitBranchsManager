using Gbm.Services.Git;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands;

public class PullTaskBranchesCommand(IGitTool gitTool)
{
    [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-pull", Description = "Pull mapped task branches",
        Example = "gbm -pull <TaskId> [Repos...]", Group = CommandGroups.Branches, Order = 5)]
    public async Task<int> ExecuteAsync(RepositoryBranchTarget[] targets, CancellationToken cancellationToken = default)
    {
        MyConsole.WriteCommandHeader("📥 Pulling task branches...");
        await BranchExecution.ForEachAsync(targets, async target =>
        {
            await BranchExecution.CheckoutRequiredAsync(gitTool, target, cancellationToken);
            await gitTool.PullAsync(cancellationToken);
        });
        MyConsole.WriteSucess("✅ Task branches pulled!");
        return 0;
    }
}
