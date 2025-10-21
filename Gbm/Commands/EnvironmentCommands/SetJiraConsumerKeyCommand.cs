using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraConsumerKeyCommand
    {
        [Command("-jc", Description = "Set Jira Consumer Key", Example = "gbm -jc <ConsumerKey>")]
        public int Execute(string value)
        {
            EnvironmentVariable.JiraConsumerKey.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira Consumer Key updated to '{value}' (User scope)");
            return 0;
        }
    }
}
