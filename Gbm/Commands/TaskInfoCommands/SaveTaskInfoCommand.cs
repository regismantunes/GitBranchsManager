using Gbm.Persistence.Configuration;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Configuration;
using Gbm.Services.Jira;
using Gbm.Services.Tasks;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection.Args;
using RA.Console.DependencyInjection.Attributes;
using TextCopy;

namespace Gbm.Commands.TaskInfoCommands;

public class SaveTaskInfoCommand(ITaskInfoRepository repository, IConfiguration configuration, IJiraClient jiraClient,
    RepositoryCatalog catalog, ITaskBranchCreator creator, TaskBranchNameFactory names)
{
    [CommandAsyncWithArgsBuilderAsync<SaveTaskInfoCommandArgsBuilder>("-t",
        Description = "Save task metadata; create/map branches only in selected repositories",
        Example = "gbm -t <TaskId> [-summary <Summary>] [-description <Description>] [-branch <Branch>|usedefaultbranch] [norepo|Repos...]",
        Group = CommandGroups.Tasks, Order = 0)]
    public async Task<int> ExecuteAsync(string taskId, string? taskSummary, string? taskDescription, string? taskBranch,
        bool? useDefaultBranch, bool? noRepo, string[] repositories, CancellationToken cancellationToken = default)
    {
        if ((taskBranch is not null && useDefaultBranch == true) ||
            (noRepo == true && (repositories.Length > 0 || taskBranch is not null || useDefaultBranch == true)))
            throw new ArgsValidationException("Conflicting branch/repository options.");
        MyConsole.WriteCommandHeader("💾 Saving task information...");
        var previous = await repository.GetAsync(taskId, cancellationToken);
        taskSummary ??= previous?.Summary;
        taskDescription ??= previous?.Description;
        if (taskSummary is null || taskDescription is null)
        {
            var jira = await jiraClient.GetTaskInfoFromJiraAsync(taskId, cancellationToken);
            taskSummary ??= jira?.Summary;
            taskDescription ??= jira?.Description;
        }
        if (string.IsNullOrWhiteSpace(taskSummary)) taskSummary = await ReadMetadataAsync("Summary", true, cancellationToken);
        if (taskDescription is null) taskDescription = await ReadMetadataAsync("Description", false, cancellationToken);
        await repository.SaveMetadataAsync(taskId, taskSummary, taskDescription, cancellationToken);
        MyConsole.WriteSucess("✅ Task metadata saved.");
        if (noRepo == true) return 0;
        if (repositories.Length == 0)
        {
            MyConsole.WriteStep("🌱 Create branches in repositories? (Y/N)");
            while (true)
            {
                var answer = await ReadAsync(cancellationToken);
                if (answer.Equals("n", StringComparison.OrdinalIgnoreCase)) return 0;
                if (answer.Equals("y", StringComparison.OrdinalIgnoreCase)) break;
                MyConsole.WriteError("❌ Enter Y or N.");
            }
            repositories = await SelectRepositoriesAsync(cancellationToken);
        }
        var task = (await repository.GetAsync(taskId, cancellationToken))!;
        if (useDefaultBranch == true) taskBranch = names.Create(task);
        if (taskBranch is null)
        {
            var format = configuration.GetValue(ConfigurationVariable.BranchDefaultNameFormat);
            var suggested = string.IsNullOrWhiteSpace(format) ? null : names.Create(task);
            do
            {
                MyConsole.WriteStep(suggested is null ? "Branch name:" : $"Branch name (ENTER accepts '{suggested}'):");
                taskBranch = await ReadAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(taskBranch)) taskBranch = suggested;
            } while (string.IsNullOrWhiteSpace(taskBranch));
        }
        await creator.CreateAsync(task, repositories, taskBranch, cancellationToken);
        MyConsole.WriteSucess("✅ Repository branches saved.");
        return 0;
    }

    private async Task<string[]> SelectRepositoriesAsync(CancellationToken cancellationToken)
    {
        var available = await catalog.GetAsync(cancellationToken);
        if (available.Length == 0) throw new ArgsValidationException("No repositories found under BasePath.");
        if (configuration.GetValue<bool>(ConfigurationVariable.ListRepositoriesAfterTaskCreation))
            for (var i = 0; i < available.Length; i++) MyConsole.WriteInfo($"{i + 1} - {available[i]}");
        while (true)
        {
            MyConsole.WriteStep("📚 Repository names or numbers, separated by spaces (0 cancels):");
            var line = await ReadAsync(cancellationToken);
            if (line.Trim() == "0") throw new OperationCanceledException("Repository selection cancelled; metadata was saved.");
            var entries = line.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries);
            if (entries.Length == 0) continue;
            try
            {
                var selected = entries.Select(e => int.TryParse(e, out var number)
                    ? number >= 1 && number <= available.Length ? available[number - 1] : throw new ArgsValidationException("Invalid repository number.")
                    : e).ToArray();
                return RepositoryCatalog.Validate(selected, available);
            }
            catch (ArgsValidationException ex) { MyConsole.WriteError(ex.Message); }
        }
    }

    private static async Task<string> ReadMetadataAsync(string label, bool summary, CancellationToken cancellationToken)
    {
        while (true)
        {
            MyConsole.WriteStep($"📝 {label} (ENTER reads the clipboard):");
            var value = await ReadAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(value)) value = ClipboardService.GetText() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(value) && (!summary || (!value.Contains('\n') && value.Length <= 150))) return value;
            MyConsole.WriteError(summary ? "❌ Summary must be a nonempty single line of at most 150 characters." : "❌ Description cannot be empty.");
        }
    }

    private static async Task<string> ReadAsync(CancellationToken cancellationToken)
    {
        var input = await MyConsole.ReadLineOrEndAsync(cancellationToken);
        if (input is null || input.Equals("Escape", StringComparison.OrdinalIgnoreCase))
            throw new OperationCanceledException("Task input cancelled.");
        return input;
    }
}
