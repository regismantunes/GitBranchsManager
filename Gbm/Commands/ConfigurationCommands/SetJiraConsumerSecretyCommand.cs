using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetJiraConsumerSecretyCommand(IConfiguration configuration)
    {
        [Command("-js",
            Description = "Set Jira Consumer Secrety",
            Example = "gbm -js <ConsumerSecrety>",
            Group = CommandGroups.Configuration,
            Order = 7)]
        public int Execute(string value)
        {
            MyConsole.WriteCommandHeader("⚙️ Updating configuration: Jira Consumer Secret...");
            configuration.SetValue(ConfigurationVariable.JiraConsumerSecret, value);
            MyConsole.WriteSucess($"✅ Jira consumer secrety was updated (User scope)");
            return 0;
        }
    }
}
