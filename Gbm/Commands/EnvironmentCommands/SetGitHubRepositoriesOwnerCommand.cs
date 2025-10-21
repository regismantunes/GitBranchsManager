using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetGitHubRepositoriesOwnerCommand
    {
        [Command("-go",
            Description = "Set GitHub repositories owner",
            Example = "gbm -go <RepositoriesOwner>",
            Group = CommandGroups.Configuration,
            Order = 2)]
        public int Execute(string value)
        {
            EnvironmentVariable.GitHubRepositoriesOwner.SetValue(value);
            MyConsole.WriteSucess($"✅ Repositories owner updated to '{value}' (User scope)");
            return 0;
        }
    }
}
