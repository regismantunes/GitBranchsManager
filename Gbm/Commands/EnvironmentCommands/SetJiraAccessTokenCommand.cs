using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraAccessTokenCommand
    {
        [Command("-ja",
            Description = "Set Jira Access Token",
            Example = "gbm -ja <JiraAccessToken>",
            Group = CommandGroups.Configuration,
            Order = 8)]
        public int Execute(string value)
        {
            EnvironmentVariable.JiraAccessToken.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira access token was updated (User scope)");
            return 0;
        }
    }
}
