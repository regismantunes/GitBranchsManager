using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetJiraConsumerKeyCommand(IConfiguration configuration)
    {
        [Command("-jc",
            Description = "Set Jira Consumer Key",
            Example = "gbm -jc <ConsumerKey>",
            Group = CommandGroups.Configuration,
            Order = 6)]
        public int Execute(string value)
        {
            MyConsole.WriteCommandHeader("⚙️ Updating configuration: Jira Consumer Key...");
            configuration.SetValue(ConfigurationVariable.JiraConsumerKey, value);
            MyConsole.WriteSucess($"✅ Jira Consumer Key updated to '{value}' (User scope)");
            return 0;
        }
    }
}
