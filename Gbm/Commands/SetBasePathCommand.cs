namespace Gbm.Commands
{
    public class SetBasePathCommand
    {
        public int Execute(string basePath)
        {
            var envVar = EnvironmentVariables.BasePath;
            Environment.SetEnvironmentVariable(envVar, basePath, EnvironmentVariableTarget.User);
            MyConsole.WriteSucess($"✅ Base path updated to '{basePath}' (User scope)");
            return 0;
        }
    }
}
