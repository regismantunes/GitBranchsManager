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
                .Where(IsSdkCompatibleSolution);
            
            foreach (var solution in solutions)
            {
                MyConsole.WriteInfo($"Solution: {solution}");
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

        /// <summary>
        /// Returns true only if all projects in the solution are SDK-style (compatible with dotnet CLI).
        /// Skips solutions containing .sqlproj or old-style .csproj (ToolsVersion-based) projects.
        /// </summary>
        private static bool IsSdkCompatibleSolution(string slnPath)
        {
            var slnDir = Path.GetDirectoryName(slnPath)!;
            var slnContent = File.ReadAllText(slnPath);

            // Skip solutions with SQL projects
            if (slnContent.Contains(".sqlproj", StringComparison.OrdinalIgnoreCase))
                return false;

            // Extract relative project paths from the solution file
            var projectPaths = System.Text.RegularExpressions.Regex
                .Matches(slnContent, @"= "".+?"", ""(.+?\.csproj)""")
                .Select(m => m.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar))
                .Select(rel => Path.GetFullPath(Path.Combine(slnDir, rel)))
                .Where(File.Exists)
                .ToList();

            // If we can't find any csproj projects, check for other project types — if any exist, skip this solution
            if (projectPaths.Count == 0)
            {
                // If the solution references any project files at all, it's likely not SDK-compatible
                return !System.Text.RegularExpressions.Regex.IsMatch(
                    slnContent, @"""[^""]+\.[a-zA-Z]+proj""");
            }

            // Skip if any project is old-style (has ToolsVersion attribute, not SDK-style)
            foreach (var proj in projectPaths)
            {
                var firstLine = File.ReadLines(proj).FirstOrDefault(l => l.TrimStart().StartsWith("<Project")) ?? string.Empty;
                if (!firstLine.Contains("Sdk=", StringComparison.OrdinalIgnoreCase))
                    return false;
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