using Gbm.Commands;
using Gbm.Commands.Args;

namespace Gbm
{
    public static class Program
    {   
        public static int Main(string[] args)
        {
            try
            {
                MyConsole.SetEncoding();
                var ctx = ArgsBuilder.Build(args);
                return ProgramCommandBuilder.ExecuteWithArgs(ctx);
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