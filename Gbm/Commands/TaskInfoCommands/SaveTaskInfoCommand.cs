using Gbm.Persistence.Configuration;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Configuration;
using Gbm.Services.Git;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection;
using RA.Console.DependencyInjection.Attributes;
using TextCopy;

namespace Gbm.Commands.TaskInfoCommands
{
    public class SaveTaskInfoCommand(ITaskInfoRepository repository, IGitTool gitTool, IConfiguration configuration)
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

            var taskBranch = GetTaskBranch(taskId, taskSummary);
            if (taskBranch == ConsoleKey.Escape.ToString())
            {
                MyConsole.WriteError("❌ Operation cancelled by user.");
                return 1;
            }
            MyConsole.WriteInfo($"→ Branch: {taskBranch}");

            await repository.SaveAsync(taskId, taskSummary, taskDescription, taskBranch, cancellationToken);
            MyConsole.WriteSucess($"✅ Task info was sucessfuly saved");

            MyConsole.WriteStep("→ If you want to create the repositories branches inform the repositories names:");
            do
            {
                var repositoriesInput = MyConsole.ReadLine();
                if (string.IsNullOrWhiteSpace(repositoriesInput))
                    break;

                var repositories = repositoriesInput
                    .Split(' ')
                    .Select(r => r.Trim());
                var isValidRepositories = true;
                foreach (var repository in repositories)
                {
                    try
                    {
                        gitTool.SetRepository(repository);
                    }
                    catch(DirectoryNotFoundException)
                    {
                        isValidRepositories = false;
                        MyConsole.WriteError($"The repository {repository} was not found. Please, inform valid repository names:");
                    }
                }

                if (isValidRepositories)
                    return await ConsoleApp.Current!.RunCommandAsync("-n", ["-n", taskId, .. repositories], cancellationToken);

            } while (true);

            return 0;
        }

        private string GetTaskBranch(string taskId, string taskSummary)
        {
            var defaultBranchName = configuration.GetValue(ConfigurationVariable.BranchDefaultNameFormat);

            string taskBranch;
            if (string.IsNullOrEmpty(defaultBranchName))
            {
                MyConsole.WriteStep($"→ Branch (enter with the branch name):");
                do
                {
                    taskBranch = MyConsole.ReadLineThenClear();
                    if (string.IsNullOrWhiteSpace(taskBranch))
                        MyConsole.WriteError("❌ Branch name can't be empty. Please enter with a valid branch name:");
                } while (string.IsNullOrWhiteSpace(taskBranch));
                return taskBranch!;
            }
            else
            {
                var defaultTaskBranch = defaultBranchName
                    .Replace("{TaskId}", taskId)
                    .Replace("{TaskSummary}", taskSummary);
                MyConsole.WriteStep($"→ Branch (enter with the branch name or press ENTER to accept the default: '{defaultTaskBranch}'):");
                taskBranch = MyConsole.ReadLineThenClear();
                if (string.IsNullOrWhiteSpace(taskBranch))
                    taskBranch = defaultTaskBranch;
            }
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