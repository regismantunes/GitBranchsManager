using Gbm.Services.Initialization;
using RA.Console.DependecyInjection;
using RA.Console.DependecyInjection.Args;

namespace Gbm
{
    public static class Program
    {
        [STAThread]
        public static async Task<int> Main(string[] args)
        {
            try
            {
                MyConsole.SetEncoding();

                var commandBuilder = new ConsoleAppBuilder(args);
                commandBuilder.Services.AddAllServices();
                
                var app = commandBuilder
                    .AddAssembly(typeof(Program).Assembly)
                    .UseDefaultHelpResources()
                    .UseOptimizedInitialization()
                    .Build();

                return await app.RunAsync();
            }
            catch (ArgsValidationException vex)
            {
                MyConsole.WriteError(vex.Message);
                return 2;
            }
            catch (Exception ex)
            {
                MyConsole.WriteError($"Error: {ex.Message}");
                return 1;
            }
        }
    }
}