using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetGitHubRepositoriesOwnerCommand
    {
        [Command("-gt", Description = "Set GitHub Token", Example = "gbm -gt <GitHubToken>")]
        public int Execute(string value)
        {
            EnvironmentVariable.GitHubRepositoriesOwner.SetValue(value);
            MyConsole.WriteSucess($"✅ Repositories owner updated to '{value}' (User scope)");
            return 0;
        }
    }
}
