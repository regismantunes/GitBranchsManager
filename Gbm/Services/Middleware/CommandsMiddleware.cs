using RA.Console.DependencyInjection.Args;
using RA.Console.DependencyInjection.Middleware;

namespace Gbm.Services.Middleware
{
    public class CommandsMiddleware : ICommandMiddleware
    {
        public async Task<int> InvokeAsync(CommandContext context, Func<CommandContext, Task<int>> next)
        {
            try
            {
                return await next(context);
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
