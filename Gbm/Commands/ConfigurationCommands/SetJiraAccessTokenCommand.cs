using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetJiraAccessTokenCommand(IConfiguration configuration)
    {
        [Command("-ja",
            Description = "Set Jira Access Token",
            Example = "gbm -ja <JiraAccessToken>",
            Group = CommandGroups.Configuration,
            Order = 8)]
        public int Execute(string value)
        {
            MyConsole.WriteHeader("⚙️ Updating configuration: Jira Access Token...");
            configuration.SetValue(ConfigurationVariable.JiraAccessToken, value);
            MyConsole.WriteSucess($"✅ Jira access token was updated (User scope)");
            return 0;
        }
    }
}
