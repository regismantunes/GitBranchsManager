using Gbm.Persistence.Repositories.Interfaces;
using TextCopy;

namespace Gbm.Commands.PullRequestCommands
{
    public class SaveTaskInfoCommand
    {
        public async Task<int> ExecuteAsync(ITaskInfoRepository repository, string taskId, CancellationToken cancellationToken = default)
        {
            MyConsole.WriteHeader($"--- Saving Task Info ---");
            MyConsole.WriteStep($"Please, inform the task details:");

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
            await repository.SaveAsync(taskId, taskSummary, taskDescription, cancellationToken);
            MyConsole.WriteSucess($"✅ Task info was sucessfuly saved");
            return 0;
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