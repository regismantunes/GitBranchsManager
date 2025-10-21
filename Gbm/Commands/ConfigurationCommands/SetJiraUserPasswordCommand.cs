using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetJiraUserPasswordCommand(IConfiguration configuration)
    {
        [Command(
            "-jp",
            Description = "Set Jira user password",
            Example = "gbm -jp <UserPassword>",
            Group = CommandGroups.Configuration,
            Order = 5)]
        public int Execute(string value)
        {
            MyConsole.WriteHeader("⚙️ Updating configuration: Jira User Password...");
            configuration.SetValue(ConfigurationVariable.JiraUserPassword, value);
            MyConsole.WriteSucess($"✅ Jira user password has updated (User scope)");
            return 0;
        }
    }
}