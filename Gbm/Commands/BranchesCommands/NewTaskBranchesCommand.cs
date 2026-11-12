using Gbm.Persistence.Entities;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands;

public class NewTaskBranchesCommand(ITaskBranchCreator creator)
{
    [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-n", Description = "Create or confirm task branches per repository",
        Example = "gbm -n <TaskId> <Repos...>", Group = CommandGroups.Branches, Order = 0)]
    public async Task<int> ExecuteAsync(TaskInfo taskInfo, string[] repositories, CancellationToken cancellationToken = default)
    {
        MyConsole.WriteCommandHeader("🌱 Creating task branches...");
        await creator.CreateAsync(taskInfo, repositories, cancellationToken: cancellationToken);
        MyConsole.WriteSucess("✅ Task branches created or confirmed.");
        return 0;
    }
}
