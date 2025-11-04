using Gbm.Persistence.Configuration;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Configuration;
using Gbm.Services.Git;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection;
using RA.Console.DependencyInjection.Attributes;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
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

            if (!MyConsole.ReadYesNo("→ Do you want to create the repositories branches?"))
                return 0;

            var repositoriesToCreateBranch = await SelectRepositoriesToCreateBranchAsync(cancellationToken);
            return await ConsoleApp.Current!.RunCommandAsync("-n", ["-n", taskId, .. repositoriesToCreateBranch], cancellationToken);
        }

        private async Task<IEnumerable<string>> SelectRepositoriesToCreateBranchAsync(CancellationToken cancellationToken = default)
        {
            MyConsole.WriteStep("→ Select the repositories you want to create a branch for this task:");
            var repositories = new List<string>();
            await foreach (var repository in gitTool.GetAllRepositoriesAsync(cancellationToken))
            {
                repositories.Add(repository);
                MyConsole.WriteInfo($"{repositories.Count} - {repository}");
            }

            do
            {
                var repositoriesInput = MyConsole.ReadLine();
                if (string.IsNullOrWhiteSpace(repositoriesInput))
                {
                    MyConsole.WriteError("❌ Please, inform at least one repository name or index separated by space:");
                    continue;
                }

                var repositoriesToCreateBranch = new List<string>();
                var selectedRepositories = repositoriesInput
                    .Split(' ')
                    .Select(r => r.Trim());
                var isValidRepositories = true;
                foreach (var repository in selectedRepositories)
                {
                    if (int.TryParse(repository, out var index))
                    {
                        if (index < 1 || index > repositories.Count)
                        {
                            MyConsole.WriteError($"❌ Invalid repository index: {index}. Please, try again:");
                            isValidRepositories = false;
                            break;
                        }
                        repositoriesToCreateBranch.Add(repositories[index - 1]);
                    }
                    else
                    {
                        if (!repositories.Contains(repository, StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true)))
                        {
                            MyConsole.WriteError($"❌ Invalid repository name: {repository}. Please, try again:");
                            isValidRepositories = false;
                            break;
                        }
                        repositoriesToCreateBranch.Add(repository);
                    }
                }

                if (isValidRepositories)
                    return repositoriesToCreateBranch;

            } while (true);
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