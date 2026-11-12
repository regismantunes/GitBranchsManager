using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.TaskInfoCommands;

public class ShowTaskInfoCommand(ITaskResolver tasks)
{
    [CommandAsync("-ti", Description = "Show task metadata and repository branches (imports unknown tasks)",
        Example = "gbm -ti <TaskId>", Group = CommandGroups.Tasks, Order = 1)]
    public async Task<int> ExecuteAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var task = await tasks.GetAsync(taskId, cancellationToken);
        MyConsole.WriteCommandHeader("ℹ️ Showing task information...");
        MyConsole.WriteInfo($"📌 [{task.Id}] {task.Summary}");
        MyConsole.WriteInfo($"📝 {task.Description}");
        MyConsole.WriteInfo($"🔗 {task.Url}");
        foreach (var branch in task.Repositories.OrderBy(r => r.Repository, StringComparer.OrdinalIgnoreCase))
            MyConsole.WriteInfo($"🌿 {branch.Repository} -> {branch.BranchName}");
        if (task.Repositories.Count == 0) MyConsole.WriteInfo("ℹ️ No repositories mapped.");
        return 0;
    }
}
