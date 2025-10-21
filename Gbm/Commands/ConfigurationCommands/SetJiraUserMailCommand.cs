using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetJiraUserMailCommand(IConfiguration configuration)
    {
        [Command("-ju",
            Description = "Set Jira user email",
            Example = "gbm -ju <UserMail>",
            Group = CommandGroups.Configuration,
            Order = 4)]
        public int Execute(string value)
        {
            MyConsole.WriteHeader("⚙️ Updating configuration: Jira User Email...");
            configuration.SetValue(ConfigurationVariable.JiraUserMail, value);
            MyConsole.WriteSucess($"✅ Jira user email updated to '{value}' (User scope)");
            return 0;
        }
    }
}
