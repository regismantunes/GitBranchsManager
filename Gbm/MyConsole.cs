namespace Gbm
{
    public static class MyConsole
    {
        public static void SetEncoding()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }

        public static void WriteError(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public static void WriteSucess(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public static void WriteStep(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public static void WriteHeader(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public static void WriteCommandHeader(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public static void WriteInfo(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public static void BackToPreviousLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop == 0 ? 0 : Console.CursorTop - 1);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        public static string ReadLineThenClear()
        {
            var input = Console.ReadLine() ?? string.Empty;
            BackToPreviousLine();
            return input;
        }

        public static string ReadLine()
        {
            return Console.ReadLine() ?? string.Empty;
        }

        public static void WriteEmptyLine()
        {
            Console.WriteLine();
        }
    }
}