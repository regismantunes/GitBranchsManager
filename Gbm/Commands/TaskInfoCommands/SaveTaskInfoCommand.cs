using Gbm.Persistence.Repositories.Interfaces;
using RA.Console.DependecyInjection.Attributes;
using TextCopy;

namespace Gbm.Commands.TaskInfoCommands
{
    public class SaveTaskInfoCommand(ITaskInfoRepository repository)
    {
        [CommandAsync("-t",
            Description = "Save task information",
            Example = "gbm -t <TaskId>",
            Group = CommandGroups.Tasks,
            Order = 0)]
        public async Task<int> ExecuteAsync(string taskId, CancellationToken cancellationToken = default)
        {
            MyConsole.WriteCommandHeader($"💾 Saving task information...");
            MyConsole.WriteStep($"Please, inform the task details:");

            var taskBranch = GetTaskBranch(taskId);
            if (taskBranch == ConsoleKey.Escape.ToString())
            {
                MyConsole.WriteError("❌ Operation cancelled by user.");
                return 1;
            }
            MyConsole.WriteInfo($"→ Branch: {taskBranch}");

            var taskSummary = GetSummary();
            if (taskSummary == ConsoleKey.Escape.ToString())
            {
                MyConsole.WriteError("❌ Operation cancelled by user.");
                return 1;
            }
            MyConsole.WriteInfo($"→ Summary: {taskSummary}");

            var taskDescription = GetDescription();
            if (taskDescription == ConsoleKey.Escape.ToString())
            {
                MyConsole.WriteError("❌ Operation cancelled by user.");
                return 1;
            }
            MyConsole.WriteInfo($"→ Description: {taskDescription}");
            await repository.SaveAsync(taskId, taskSummary, taskDescription, taskBranch, cancellationToken);
            MyConsole.WriteSucess($"✅ Task info was sucessfuly saved");
            return 0;
        }

        private string GetTaskBranch(string taskId)
        {
            MyConsole.WriteStep($"→ Branch (enter with the branch name or press ENTER to accept the default: 'feature/{taskId}'):");
            var taskBranch = MyConsole.ReadLineThenClear();
            if (string.IsNullOrWhiteSpace(taskBranch))
                taskBranch = $"feature/{taskId}";
            return taskBranch!;
        }

        private string GetSummary()
        {
            MyConsole.WriteStep($"→ Summary (enter with the summary or press ENTER to get from clipboard):");
            string? taskSummary;
            while (true)
            {
                taskSummary = MyConsole.ReadLineThenClear();
                if (string.IsNullOrWhiteSpace(taskSummary))
                    taskSummary = ClipboardService.GetText();
                if (!string.IsNullOrWhiteSpace(taskSummary))
                {
                    if (!taskSummary.Contains('\n') &&
                        taskSummary.Length <= 150)
                        break;

                    MyConsole.WriteError("❌ Summary must be a single line with a maximum of 150 characters. Please enter a valid information:");
                    continue;
                }
                MyConsole.WriteError("❌ It was not possible to get data from clipboard. Please enter a valid information:");
            }
            return taskSummary!;
        }

        private string GetDescription()
        {
            MyConsole.WriteStep($"→ Description (enter with the description or press ENTER to get from clipboard):");
            string? taskDescription;
            while (true)
            {
                taskDescription = MyConsole.ReadLineThenClear();
                if (string.IsNullOrWhiteSpace(taskDescription))
                    taskDescription = ClipboardService.GetText();
                if (!string.IsNullOrWhiteSpace(taskDescription))
                    break;
                MyConsole.WriteError("❌ It was not possible to get data from clipboard. Please enter a valid description:");
            }
            return taskDescription!;
        }
    }
}