using Gbm.Commands.Args;
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
            { "-pr", typeof(OpenPrsTaskCommand) },
            { "-gt", typeof(SetGitHubTokenCommand) }
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

            if (command is OpenPrsTaskCommand openPrsTask)
            {
                if (args.GitTool is null) throw new InvalidOperationException("GitTool is not initialized.");
                if (args.TaskBranch is null) throw new InvalidOperationException("TaskBranch is not initialized.");
                if (args.Repositories is null) throw new InvalidOperationException("Repositories is not initialized.");
                if (args.GitHubToken is null) throw new InvalidOperationException("GitHubToken is not initialized.");

                return await openPrsTask.ExecuteAsync(args.GitTool, args.TaskBranch, args.Repositories, args.GitHubToken);
            }

            if (command is SetBasePathCommand setBasePath)
            {
                if (args.BasePath is null) throw new InvalidOperationException("BasePath is not initialized.");
                
                return setBasePath.Execute(args.BasePath);
            }

            if (command is SetGitHubTokenCommand setGitHubToken)
            {
                if (args.GitHubToken is null) throw new InvalidOperationException("GitHubToken is not initialized.");
                
                return setGitHubToken.Execute(args.GitHubToken);
            }

            if (command is HelpCommand helpCommand)
            {
                return helpCommand.Execute();
            }

            return 1;
        }
    }
}
