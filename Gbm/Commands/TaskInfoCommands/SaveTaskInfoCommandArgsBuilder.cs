using Gbm.Services.Git;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Commands.TaskInfoCommands
{
    public sealed class SaveTaskInfoCommandArgsBuilder(IGitTool gitTool) : IArgsBuilderAsync
    {
        public async Task<IDictionary<string, object>> BuildAsync(string[] args, CancellationToken cancellationToken)
        {
            if (args == null ||
                args.Length == 0)
                throw new ArgumentException("Arguments cannot be null or empty.", nameof(args));

            var command = args[0].ToLower();
            if (command != "-t") throw new ArgsValidationException($"Invalid command. Expected '-t' but got '{command}'. Example: gbm -t TaskId");
            if (args.Length < 2) throw new ArgsValidationException($"Missing TaskId. Example: gbm {command} TaskId");

            var taskId = args[1];
            string? taskSummary = null;
            string? taskDescription = null;
            string? taskBranch = null;
            bool? useDefaultBranch = null;
            bool? noRepo = null;
            var optionalArgs = args.Skip(2).ToList();

            if (optionalArgs.Contains("-summary", StringComparer.InvariantCultureIgnoreCase))
            {
                var summaryIndex = optionalArgs.IndexOf("-summary");
                if (summaryIndex == args.Length - 1) throw new ArgsValidationException("Missing summary value after '-summary'. Example: gbm -t TaskId -summary \"Task Summary\"");
                taskSummary = optionalArgs[summaryIndex + 1];
                optionalArgs.RemoveRange(summaryIndex, 2);
            }

            if (optionalArgs.Contains("-description", StringComparer.InvariantCultureIgnoreCase))
            {
                var descriptionIndex = optionalArgs.IndexOf("-description");
                if (descriptionIndex == args.Length - 1) throw new ArgsValidationException("Missing description value after '-description'. Example: gbm -t TaskId -description \"Task Description\"");
                taskDescription = optionalArgs[descriptionIndex + 1];
                optionalArgs.RemoveRange(descriptionIndex, 2);
            }

            if (optionalArgs.Contains("-branch", StringComparer.InvariantCultureIgnoreCase))
            {
                var branchIndex = optionalArgs.IndexOf("-branch");
                if (branchIndex == args.Length - 1) throw new ArgsValidationException("Missing branch value after '-branch'. Example: gbm -t TaskId -branch \"BranchName\"");
                taskBranch = optionalArgs[branchIndex + 1];
                optionalArgs.RemoveRange(branchIndex, 2);
            }

            if (optionalArgs.Contains("usedefaultbranch", StringComparer.InvariantCultureIgnoreCase))
            {
                useDefaultBranch = true;
                optionalArgs.Remove("usedefaultbranch");
            }

            if (optionalArgs.Contains("norepo", StringComparer.InvariantCultureIgnoreCase))
            {
                noRepo = true;
                optionalArgs.Remove("norepo");
            }

            var repositories = optionalArgs.ToArray();
            foreach (var repo in repositories)
                await gitTool.SetRepositoryAsync(repo, cancellationToken);

            return new Dictionary<string, object>
            {
                { "TaskId", taskId },
                { "TaskSummary", taskSummary },
                { "TaskDescription", taskDescription },
                { "TaskBranch", taskBranch },
                { "UseDefaultBranch", useDefaultBranch },
                { "NoRepo", noRepo },
                { "Repositories", repositories }
            };
        }
    }
}
