using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetBranchDefaultNameFormatCommand(IConfiguration configuration)
    {
        [Command("-bn",
            Description = "Set branch default name format with the variables {TaskId} and {TaskSummary}, like 'feature/{TaskId}_{TaskSummary}'",
            Example = "gbm -bn <BranchDefaultNameFormat>",
            Group = CommandGroups.Configuration,
            Order = 10)]
        public int Execute(string value)
        {
            MyConsole.WriteCommandHeader("⚙️ Updating configuration: Branch default name format...");
            configuration.SetValue(ConfigurationVariable.BranchDefaultNameFormat, value);
            MyConsole.WriteSucess($"✅ Branch default name format updated to '{value}' (User scope)");
            return 0;
        }
    }
}