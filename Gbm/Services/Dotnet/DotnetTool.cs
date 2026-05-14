using Gbm.Services.Git;
using System.Diagnostics;

namespace Gbm.Services.Dotnet
{
    public class DotnetTool(IGitTool gitTool) : IDotnetTool
    {
        public async Task<bool> BuildRepositoryAsync(string repo, string branchName, CancellationToken cancellationToken = default)
        {
            await gitTool.SetRepositoryAsync(repo, cancellationToken);

            MyConsole.WriteStep($"→ Checking out '{branchName}' in '{repo}'");
            if (!await gitTool.CheckoutAsync(branchName, cancellationToken))
            {
                MyConsole.WriteError($"❌ Branch '{branchName}' not found in '{repo}'.");
                return false;
            }

            MyConsole.WriteStep($"→ Building '{repo}'...");
            return await BuildSolutionsFromDirectoryAsync(gitTool.WorkingDirectory!, cancellationToken);
        }

        private static async Task<bool> BuildSolutionsFromDirectoryAsync(string direcotryPath, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(direcotryPath))
                throw new DirectoryNotFoundException($"The directory '{direcotryPath}' does not exist.");

            var solutions = Directory.GetFiles(direcotryPath, "*.sln", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(direcotryPath, "*.slnx", SearchOption.AllDirectories))
                .Where(sln => IsMSBuildCompatibleSolution(sln) && !HasSqlProjects(sln));

            if (!solutions.Any())
            {
                MyConsole.WriteInfo("No dotnet-compatible solutions found.");
                return true; // No solutions to build, consider it a success
            }

            foreach (var solution in solutions)
            {
                var solutionName = Path.GetFileName(solution);
                MyConsole.WriteInfo($"Building solution: {solutionName}");
                var platformArg = HasX64Configuration(solution) ? " /p:Platform=x64" : string.Empty;
                var result = await RunMSBuildAsync(direcotryPath, $"\"{solution}\" /t:Restore;Build /p:Configuration=Debug{platformArg}", cancellationToken);
                if (result.ExitCode != 0)
                {
                    MyConsole.WriteError($"❌ Build failed for '{solutionName}':");
                    if (!string.IsNullOrWhiteSpace(result.Error))
                        MyConsole.WriteError(result.Error.TrimEnd());
                    return false;
                }
            }

            MyConsole.WriteStep($"→ All solutions built successfully ✔");
            return true;
        }

        /// <summary>
        /// Returns true if the solution file format is compatible with MSBuild.
        /// For .sln files, requires "Format Version 12.00" or higher (Visual Studio 2012+).
        /// For .slnx files, always considered compatible.
        /// </summary>
        private static bool IsMSBuildCompatibleSolution(string slnPath)
        {
            if (Path.GetExtension(slnPath).Equals(".slnx", StringComparison.OrdinalIgnoreCase))
                return true;
            
            const string SolutionFileHeader = "Microsoft Visual Studio Solution File, Format Version";

            foreach (var line in File.ReadLines(slnPath).Take(5))
            {
                var trimmed = line.TrimStart();
                if (!trimmed.StartsWith(SolutionFileHeader, StringComparison.OrdinalIgnoreCase))
                    continue;

                var versionPart = trimmed[SolutionFileHeader.Length..].Trim();
                return Version.TryParse(versionPart, out var version) && version >= new Version(12, 0);
            }

            return false;
        }

        private static bool HasX64Configuration(string slnPath)
        {
            var slnContent = File.ReadAllText(slnPath);
            return slnContent.Contains("x64", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true only if all projects in the solution are SDK-style (compatible with dotnet CLI).
        /// Skips solutions containing .sqlproj or old-style .csproj (ToolsVersion-based) projects.
        /// </summary>
        private static bool HasSqlProjects(string slnPath)
        {
            var slnContent = File.ReadAllText(slnPath);
            return slnContent.Contains(".sqlproj", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<RunDotnetResult> RunMSBuildAsync(string workingDirectory, string arguments, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "C:\\Program Files\\Microsoft Visual Studio\\18\\Insiders\\MSBuild\\Current\\Bin\\MSBuild.exe",
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