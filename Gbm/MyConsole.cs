namespace Gbm
{
    public static class MyConsole
    {
        private static bool _cursorAvailable = true;

        #region Colors
        public static ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
        public static ConsoleColor SucessColor { get; set; } = ConsoleColor.Green;
        public static ConsoleColor StepColor { get; set; } = ConsoleColor.Cyan;
        public static ConsoleColor HeaderColor { get; set; } = ConsoleColor.Blue;
        public static ConsoleColor CommandHeaderColor { get; set; } = ConsoleColor.DarkBlue;
        public static ConsoleColor InfoColor { get; set; } = ConsoleColor.White;
        #endregion

        #region Settings
        public static void SetDefaults()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            SetCursorVisible(false);
        }

        public static void ResetConsole()
        {
            Console.ResetColor();
            SetCursorVisible(true);
            _progressBarActive = false;
            _progressPercent = 0m;
        }
        #endregion

        #region Write Methods
        public static void WriteError(string message) => WriteWithProgress(message, ErrorColor, true);
        public static void WriteSucess(string message) => WriteWithProgress(message, SucessColor);
        public static void WriteStep(string message) => WriteWithProgress(message, StepColor);
        public static void WriteHeader(string message) => WriteWithProgress(message, HeaderColor);
        public static void WriteCommandHeader(string message) => WriteWithProgress(message, CommandHeaderColor);
        public static void WriteInfo(string message) => WriteWithProgress(message, InfoColor);
        public static void WriteEmptyLine() => WriteWithProgress(string.Empty, InfoColor);
        #endregion

        #region Manage Showing
        public static void BackToPreviousLine()
        {
            if (!_cursorAvailable) return;
            TryCursorOperation(() => Console.SetCursorPosition(0, Console.CursorTop == 0 ? 0 : Console.CursorTop - 1));
            Console.Write(new string(' ', Console.WindowWidth));
            TryCursorOperation(() => Console.SetCursorPosition(0, Console.CursorTop));
        }
        #endregion

        #region Read Methods
        public static async ValueTask<string?> ReadLineOrEndAsync(CancellationToken cancellationToken = default)
        {
            var input = await Console.In.ReadLineAsync(cancellationToken);
            BackToPreviousLine();
            return input;
        }

        public static string ReadLine() => ReadWithProgress();

        public static string ReadLineThenClear() => ReadWithProgress(true);

        public static bool ReadYesNo(string prompt)
        {
            WriteStep(string.Concat(prompt, " (Y/N)"));
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Y)
                {
                    WriteStep("✅ You selected: Yes");
                    return true;
                }
                else if (key.Key == ConsoleKey.N)
                {
                    WriteStep("🛑 You selected: No");
                    return false;
                }
                WriteStep("❌ You have pressed an invalid key. Please, Y or N:");
            }
        }
        #endregion

        #region Progress Bar
        private static int _progressBarCursorTop;
        private static int _progressBarWidth;
        private static bool _progressBarActive;
        private static decimal _progressPercent;

        public static void StartProgressBar(int progressBarWidth = 50)
        {
            _progressBarWidth = progressBarWidth;
            _progressBarActive = true;
            _progressPercent = 0m;
            Console.WriteLine();
            _progressBarCursorTop = Console.CursorTop - 1;
            UpdateProgressBar(0m);
        }

        public static void UpdateProgressBar(decimal percent, ConsoleColor? color = null)
        {
            _progressPercent = percent;
            var previousCursorTop = Console.CursorTop;
            var previousCursorLeft = Console.CursorLeft;
            Console.SetCursorPosition(0, _progressBarCursorTop);
            var filledPercentage = Convert.ToInt32(_progressBarWidth * percent);
            var missedPercentage = _progressBarWidth - filledPercentage;
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color ?? StepColor;
            var text = string.Concat(new string('█', filledPercentage), new string(' ', missedPercentage), " ", Math.Round(percent * 100, 0), "%");
            if (text.Length < Console.WindowWidth)
                text = text.PadRight(Console.WindowWidth);
            Console.Write(text);
            Console.ForegroundColor = originalColor;
            Console.SetCursorPosition(previousCursorLeft, previousCursorTop);
        }

        private static void UpdateProgressBar() => UpdateProgressBar(_progressPercent);

        public static void FinishProgressBar()
        {
            if (!_progressBarActive) return;
            Console.SetCursorPosition(0, _progressBarCursorTop);
            Console.WriteLine();
            _progressBarActive = false;
            _progressPercent = 0m;
            _progressBarCursorTop = Console.CursorTop - 1;
            Console.SetCursorPosition(0, _progressBarCursorTop + 1);
        }
        #endregion

        private static void WriteWithProgress(string message, ConsoleColor color, bool error = false)
        {
            void write()
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                var writer = error ? Console.Error : Console.Out;
                writer.WriteLine(message);
                Console.ForegroundColor = originalColor;
            }

            if (!_progressBarActive)
            {
                write();
                return;
            }

            Console.SetCursorPosition(0, _progressBarCursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, _progressBarCursorTop);

            write();

            _progressBarCursorTop = Console.CursorTop;
            UpdateProgressBar(_progressPercent);
            Console.SetCursorPosition(0, _progressBarCursorTop);
        }
        
        private static string ReadWithProgress(bool thenClear = false)
        {
            string read()
            {
                SetCursorVisible(true);
                var input = Console.ReadLine() ?? string.Empty;
                SetCursorVisible(false);
                if (thenClear) BackToPreviousLine();
                return input;
            }

            if (!_progressBarActive)
                return read();

            var currentCursorPosition = _progressBarCursorTop;
            Console.SetCursorPosition(0, _progressBarCursorTop);
            Console.WriteLine(new string(' ', Console.WindowWidth));
            _progressBarCursorTop = Console.CursorTop;
            UpdateProgressBar(_progressPercent);
            Console.SetCursorPosition(0, currentCursorPosition);
            
            var input = read();

            if (thenClear)
            {
                Console.SetCursorPosition(0, _progressBarCursorTop);
                Console.WriteLine(new string(' ', Console.WindowWidth));
                _progressBarCursorTop--;
                UpdateProgressBar(_progressPercent);
            }

            Console.SetCursorPosition(0, _progressBarCursorTop);
            return input;
        }

        private static void SetCursorVisible(bool visible) => TryCursorOperation(() => Console.CursorVisible = visible);

        private static void TryCursorOperation(Action operation)
        {
            if (!_cursorAvailable) return;

            try
            {
                operation();
            }
            catch (IOException)
            {
                _cursorAvailable = false;
                _progressBarActive = false;
            }
            catch (InvalidOperationException)
            {
                _cursorAvailable = false;
                _progressBarActive = false;
            }
        }
    }
}
