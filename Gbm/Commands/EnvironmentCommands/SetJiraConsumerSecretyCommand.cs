using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraConsumerSecretyCommand
    {
        [Command("-js",
            Description = "Set Jira Consumer Secrety",
            Example = "gbm -js <ConsumerSecrety>",
            Group = CommandGroups.Configuration,
            Order = 7)]
        public int Execute(string value)
        {
            MyConsole.WriteHeader("⚙️ Updating configuration: Jira Consumer Secret...");
            EnvironmentVariable.JiraConsumerSecret.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira consumer secrety was updated (User scope)");
            return 0;
        }
    }
}
