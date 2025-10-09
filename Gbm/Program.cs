using Gbm.Commands;
using Gbm.Commands.Args;

namespace Gbm
{
    public static class Program
    {   
        public static async Task<int> Main(string[] args)
        {
            try
            {
                MyConsole.SetEncoding();
                var ctx = ArgsBuilder.Build(args);
                return await ProgramCommandBuilder.ExecuteWithArgsAsync(ctx);
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