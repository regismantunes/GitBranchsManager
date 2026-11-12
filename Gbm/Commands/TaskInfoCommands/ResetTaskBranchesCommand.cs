using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Args;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.TaskInfoCommands;

public class ResetTaskBranchesCommand(ITaskResolver tasks)
{
    [CommandAsyncWithArgsBuilderAsync<ResetTaskBranchesCommandArgsBuilder>("-rb", Description = "Rediscover and replace all repository mappings without changing Git refs",
        Example = "gbm -rb <TaskId>", Group = CommandGroups.Tasks, Order = 2)]
    public async Task<int> ExecuteAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var task = await tasks.ResetAsync(taskId, cancellationToken);
        MyConsole.WriteSucess($"🔄 Task '{task.Id}': replaced mappings with {task.Repositories.Count} repository branches.");
        return 0;
    }
}

public class ResetTaskBranchesCommandArgsBuilder : IArgsBuilderAsync
{
    public Task<IDictionary<string, object>> BuildAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args is null || args.Length != 2 || !args[0].Equals("-rb", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(args[1]) || args[1].StartsWith('-'))
            throw new ArgsValidationException("Expected: gbm -rb <TaskId>. Repository filters are not supported.");
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object> { ["TaskId"] = args[1] });
    }
}
