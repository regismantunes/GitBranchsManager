using Gbm.Services.Initialization;
using Gbm.Services.Middleware;
using RA.Console.DependencyInjection;

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
                    .UseMiddleware<CommandsMiddleware>()
                    .UseDefaultHelpResources()
                    .UseOptimizedInitialization()
                    .Build();

                return await app.RunAsync();
            }
            catch (Exception ex)
            {
                MyConsole.WriteError($"Error: {ex.Message}");
                return 1;
            }
        }
    }
}