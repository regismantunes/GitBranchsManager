using Gbm.Persistence.Repositories.Interfaces;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.TaskInfoCommands
{
    public class ShowTaskInfoCommand(ITaskInfoRepository repository)
    {
        [CommandAsync("-ti",
            Description = "Show task information",
            Example = "gbm -ti <TaskId>",
            Group = CommandGroups.Tasks,
            Order = 1)]
        public async Task<int> ExecuteAsync(string taskId, CancellationToken cancellationToken = default)
        {
            MyConsole.WriteCommandHeader($"ℹ️ Showing task information...");
            var taskInfo = await repository.GetAsync(taskId, cancellationToken);
            if (taskInfo == null)
            {
                MyConsole.WriteError($"❌ Task '{taskId}' not found.");
                return 1;
            }
            MyConsole.WriteInfo($"→ Summary: {taskInfo.Summary}");
            MyConsole.WriteInfo($"→ Description: {taskInfo.Description}");
            MyConsole.WriteInfo($"→ Branch: {taskInfo.BranchName}");
            return 0;
        }
    }
}
