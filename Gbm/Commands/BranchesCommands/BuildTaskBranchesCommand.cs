using Gbm.Services.Build;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands;

public class BuildTaskBranchesCommand(IBuildTool dotnetTool)
{
    [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-build", Description = "Build each mapped task branch",
        Example = "gbm -build <TaskId> [Repos...]", Group = CommandGroups.Branches, Order = 3)]
    public async Task<int> ExecuteAsync(RepositoryBranchTarget[] targets, CancellationToken cancellationToken = default)
    {
        foreach (var target in targets)
            if (!await dotnetTool.BuildBranchAsync(target.Repository, target.BranchName, cancellationToken))
            {
                MyConsole.WriteError($"❌ Build failed: {target.Repository} -> {target.BranchName}.");
                return 1;
            }
        MyConsole.WriteSucess("✅ All builds passed!");
        return 0;
    }
}
