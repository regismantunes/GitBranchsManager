using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetGitHubTokenCommand
    {
        [Command("-gt",
            Description = "Set GitHub Token",
            Example = "gbm -gt <GitHubToken>",
            Group = CommandGroups.Configuration,
            Order = 1)]
        public int Execute(string value)
        {
            EnvironmentVariable.GitHubToken.SetValue(value);
            MyConsole.WriteSucess($"✅ GitHub Token has updated. (User scope)");
            return 0;
        }
    }
}
