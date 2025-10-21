using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraConsumerSecretyCommand
    {
        [Command("-js", Description = "Set Jira Consumer Secrety", Example = "gbm -js <ConsumerSecrety>")]
        public int Execute(string value)
        {
            EnvironmentVariable.JiraConsumerSecret.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira consumer secrety was updated (User scope)");
            return 0;
        }
    }
}
