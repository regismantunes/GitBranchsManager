using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetGitHubTokenCommand(IConfiguration configuration)
    {
        [Command("-gt",
            Description = "Set GitHub Token",
            Example = "gbm -gt <GitHubToken>",
            Group = CommandGroups.Configuration,
            Order = 1)]
        public int Execute(string value)
        {
            MyConsole.WriteHeader("⚙️ Updating configuration: GitHub Token...");
            configuration.SetValue(ConfigurationVariable.GitHubToken, value);
            MyConsole.WriteSucess($"✅ GitHub Token has updated. (User scope)");
            return 0;
        }
    }
}
