using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraUserPasswordCommand
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
            EnvironmentVariable.JiraUserPassword.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira user password has updated (User scope)");
            return 0;
        }
    }
}