namespace Gbm.Commands
{
    public class SetBasePathCommand
    {
        public int Execute(string basePath)
        {
            var basePathEnvVar = EnvironmentVariables.BasePath;
            Environment.SetEnvironmentVariable(basePathEnvVar, basePath, EnvironmentVariableTarget.User);
            MyConsole.WriteSucess($"✅ Base path updated to '{basePath}' (User scope)");
            return 0;
        }
    }
}
