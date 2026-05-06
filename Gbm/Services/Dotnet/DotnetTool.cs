using System.Diagnostics;

namespace Gbm.Services.Dotnet
{
    public class DotnetTool : IDotnetTool
    {
        public async Task<bool> BuildRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(repositoryPath))
                throw new DirectoryNotFoundException($"The directory '{repositoryPath}' does not exist.");

            var solutions = Directory.GetFiles(repositoryPath, "*.sln", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(repositoryPath, "*.slnx", SearchOption.AllDirectories))
                .ToArray();
            if (solutions.Length == 0)
                return true;

            foreach (var solution in solutions)
            {
                var result = await RunDotnetAsync(repositoryPath, $"build \"{solution}\"", cancellationToken);
                if (result.ExitCode != 0)
                {
                    MyConsole.WriteError($"❌ Build failed for '{Path.GetFileName(solution)}':");
                    if (!string.IsNullOrWhiteSpace(result.Output))
                        MyConsole.WriteError(result.Output.TrimEnd());
                    if (!string.IsNullOrWhiteSpace(result.Error))
                        MyConsole.WriteError(result.Error.TrimEnd());
                    return false;
                }
            }

            return true;
        }

        private static async Task<RunDotnetResult> RunDotnetAsync(string workingDirectory, string arguments, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return new RunDotnetResult { ExitCode = process.ExitCode, Output = stdout, Error = stderr };
        }

        private struct RunDotnetResult
        {
            public string Output { get; set; }
            public string Error { get; set; }
            public int ExitCode { get; set; }
        }
    }
}

