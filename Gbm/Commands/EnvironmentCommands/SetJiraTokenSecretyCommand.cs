using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraTokenSecretyCommand
    {
        [Command("-jt",
            Description = "Set Jira Token Secrety",
            Example = "gbm -jt <JiraTokenSecrety>",
            Group = CommandGroups.Configuration,
            Order = 9)]
        public int Execute(string value)
        {
            EnvironmentVariable.JiraTokenSecrety.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira Token Secrety has updated. (User scope)");
            return 0;
        }
    }
}
