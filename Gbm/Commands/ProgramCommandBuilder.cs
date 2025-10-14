using Gbm.Commands.Args;
using Gbm.Commands.EnvironmentCommands;
using Gbm.Commands.PullRequestCommands;
using Gbm.Commands.TaskCommands;
using Gbm.Jira;
using Gbm.Persistence.Repositories.Interfaces;
using System.Threading.Tasks;

namespace Gbm.Commands
{
    public static class ProgramCommandBuilder
    {
        private static readonly IReadOnlyDictionary<string, Type> ArgCommands = new Dictionary<string, Type>
        {
            { "-n", typeof(NewTaskCommand) },
            { "-u", typeof(UpdateTaskCommand) },
            { "-p", typeof(PushTaskCommand) },
            { "-r", typeof(RemoveTaskCommand) },
            { "-s", typeof(SetTaskCommand) },
            { "-d", typeof(SendTaskToDevelopCommand) },
            { "-h", typeof(HelpCommand) },
            { "-b", typeof(SetBasePathCommand) },
            { "-l", typeof(ListTaskCommand) },
            { "-pr", typeof(OpenPullRequestsTaskCommand) },
            { "-gt", typeof(SetGitHubTokenCommand) },
            { "-go", typeof(SetGitHubRepositoriesOwnerCommand) },
            { "-jt", typeof(SetJiraTokenSecretyCommand) },
            { "-ju", typeof(SetJiraUserMailCommand) },
            { "-jd", typeof(SetJiraDomainCommand) },
            { "-t", typeof(SaveTaskInfoCommand) },
            { "-jp", typeof(SetJiraUserPasswordCommand) },
            { "-jc", typeof(SetJiraConsumerKeyCommand) },
            { "-js", typeof(SetJiraConsumerSecretyCommand) },
            { "-ja", typeof(SetJiraAccessTokenCommand) },
            { "-pri", typeof(ListPullRequestInfoByTaskIdCommand) }
        };
        public static object? Create(string action)
        {
            if (ArgCommands.TryGetValue(action, out var commandType))
                return Activator.CreateInstance(commandType);

            return null;
        }

        public static bool IsValidAction(string action) => ArgCommands.ContainsKey(action);

        public static async Task<int> ExecuteWithArgsAsync(ArgsContext args)
        {
            var command = Create(args.Action)
                ?? throw new InvalidOperationException($"Unknown action '{args.Action}'.");

            if (command is ITaskCommand taskCommand)
            {
                if (args.GitTool is null) throw new InvalidOperationException("GitTool is not initialized.");
                if (args.TaskBranch is null) throw new InvalidOperationException("TaskBranch is not initialized.");
                if (args.Repositories is null) throw new InvalidOperationException("Repositories is not initialized.");

                return await taskCommand.ExecuteAsync(args.GitTool, args.TaskBranch, args.Repositories);
            }

            if (command is ISetEnvironmentCommand setEnvironment)
            {
                if (args.Environment is null) throw new InvalidOperationException("Environment is not initialized.");
                return setEnvironment.Execute(args.Environment);
            }

            if (command is OpenPullRequestsTaskCommand openPrsTask)
            {
                if (args.GitTool is null) throw new InvalidOperationException("GitTool is not initialized.");
                if (args.TaskBranch is null) throw new InvalidOperationException("TaskBranch is not initialized.");
                if (args.Repositories is null) throw new InvalidOperationException("Repositories is not initialized.");
                if (args.GitHubClient is null) throw new InvalidOperationException("GitHubClient is not initialized.");
                if (args.JiraClient is null) throw new InvalidOperationException("JiraClient is not initialized.");
                if (args.PullRequestInfoRepository is null) throw new InvalidOperationException("PullRequestInfoRepository is not initialized.");

                return await openPrsTask.ExecuteAsync(args.GitTool, args.TaskBranch, args.Repositories, args.GitHubClient, args.JiraClient, args.PullRequestInfoRepository);
            }

            if (command is SaveTaskInfoCommand saveTaskInfo)
            {
                if (args.TaskInfoRepository is not ITaskInfoRepository taskInfoRepository) throw new InvalidOperationException("TaskInfoRepository is not initialized.");
                if (args.TaskId is null) throw new InvalidOperationException("TaskId is not initialized.");
                
                return await saveTaskInfo.ExecuteAsync(taskInfoRepository, args.TaskId);
            }

            if (command is ListPullRequestInfoByTaskIdCommand listPullRequestInfoByTaskId)
            {
                if (args.TaskId is null) throw new InvalidOperationException("TaskId is not initialized.");
                if (args.TaskInfoRepository is not ITaskInfoRepository taskInfoRepository) throw new InvalidOperationException("TaskInfoRepository is not initialized.");
                if (args.PullRequestInfoRepository is not IPullRequestInfoRepository pullRequestInfoRepository) throw new InvalidOperationException("PullRequestInfoRepository is not initialized.");

                return await listPullRequestInfoByTaskId.ExecuteAsync(args.TaskId, taskInfoRepository, pullRequestInfoRepository);
            }

            if (command is HelpCommand helpCommand)
            {
                return helpCommand.Execute();
            }

            return 1;
        }
    }
}
