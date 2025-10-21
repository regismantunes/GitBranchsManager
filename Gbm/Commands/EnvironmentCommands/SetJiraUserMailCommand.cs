using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraUserMailCommand
    {
        [Command("-ju",
            Description = "Set Jira user email",
            Example = "gbm -ju <UserMail>",
            Group = CommandGroups.Configuration,
            Order = 4)]
        public int Execute(string value)
        {
            MyConsole.WriteHeader("⚙️ Updating configuration: Jira User Email...");
            EnvironmentVariable.JiraUserMail.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira user email updated to '{value}' (User scope)");
            return 0;
        }
    }
}
