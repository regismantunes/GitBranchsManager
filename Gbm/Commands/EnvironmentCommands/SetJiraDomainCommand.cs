using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraDomainCommand
    {
        [Command("-jd",
            Description = "Set Jira domain",
            Example = "gbm -jd <Domain>",
            Group = CommandGroups.Configuration,
            Order = 3)]
        public int Execute(string value)
        {
            MyConsole.WriteHeader("⚙️ Updating configuration: Jira Domain...");
            EnvironmentVariable.JiraDomain.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira domain updated to '{value}' (User scope)");
            return 0;
        }
    }
}
