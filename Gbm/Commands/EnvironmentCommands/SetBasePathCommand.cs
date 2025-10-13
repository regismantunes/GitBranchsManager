using Gbm.Environment;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetBasePathCommand : ISetEnvironmentCommand
    {
        public int Execute(string value)
        {
            EnvironmentVariable.BasePath.SetValue(value);
            MyConsole.WriteSucess($"✅ Base path updated to '{value}' (User scope)");
            return 0;
        }
    }
}
