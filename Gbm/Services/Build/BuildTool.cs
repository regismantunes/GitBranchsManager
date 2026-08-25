using Gbm.Services.Git;
using System.Diagnostics;

namespace Gbm.Services.Build
{
    public class BuildTool(IGitTool gitTool) : IBuildTool
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
            return await BuildFilesFromDirectoryAsync(gitTool.WorkingDirectory!, cancellationToken);
        }

        private static async Task<bool> BuildFilesFromDirectoryAsync(string direcotryPath, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(direcotryPath))
                throw new DirectoryNotFoundException($"The directory '{direcotryPath}' does not exist.");

            var solutions = Directory.GetFiles(direcotryPath, "*.sln", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(direcotryPath, "*.slnx", SearchOption.AllDirectories))
                .Distinct()
                .Where(sln =>
                { 
                    var fileInfo = new FileInfo(sln);
                    if (fileInfo.Directory!.Name.Equals("archived", StringComparison.InvariantCultureIgnoreCase))
                        return false;
                    return IsMSBuildCompatibleSolution(sln) && !HasSqlProjects(sln); 
                });

            var hasBuilded = false;
            var combinedProjectsSolution = solutions.FirstOrDefault(x => x.EndsWith("CombinedProjects.slnx", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(combinedProjectsSolution))
            {
                hasBuilded = true;
                if (!await TryToBuildFileAsync(direcotryPath, combinedProjectsSolution, false, cancellationToken))
                    return false;
            }
            else
            {
                foreach (var solution in solutions)
                {
                    hasBuilded = true;
                    if (!await TryToBuildFileAsync(direcotryPath, solution, false, cancellationToken))
                        return false;
                }
            }

            var bicepFiles = Directory.GetFiles(direcotryPath, "*.bicep", SearchOption.TopDirectoryOnly);
            foreach (var bicepFile in bicepFiles)
            {
                hasBuilded = true;
                if (!await TryToBuildFileAsync(direcotryPath, bicepFile, true, cancellationToken))
                    return false;
            }

            if (!hasBuilded)
            {
                MyConsole.WriteInfo("No files found to build.");
                return true; // No solutions to build, consider it a success
            }

            MyConsole.WriteStep($"→ All files built successfully ✔");
            return true;
        }

        private static async Task<bool> TryToBuildFileAsync(string directoryPath, string file, bool buildWithAz, CancellationToken cancellationToken)
        {
            var fileName = Path.GetFileName(file);
            MyConsole.WriteInfo($"Building file: {fileName}");
            RunDotnetResult result;
            if (buildWithAz)
            {
                result = await RunAzAsync(directoryPath, $"bicep build --file \"{file}\"", cancellationToken);
            }
            else
            {
                var platformArg = HasX64Configuration(file) ? " /p:Platform=x64" : string.Empty;
                result = await RunMSBuildAsync(directoryPath, $"\"{file}\" /t:Restore;Build /p:Configuration=Debug{platformArg} /p:NuGetInteractive=true", cancellationToken);
            }
            if (result.ExitCode != 0)
            {
                MyConsole.WriteError($"❌ Build failed for '{fileName}':");
                if (!string.IsNullOrWhiteSpace(result.Error))
                    MyConsole.WriteError(result.Error.TrimEnd());
                else if (!string.IsNullOrWhiteSpace(result.Output))
                    MyConsole.WriteError(result.Output.TrimEnd());
                return false;
            }
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

        private static Task<RunDotnetResult> RunAzAsync(string workingDirectory, string arguments, CancellationToken cancellationToken)
            => RunBuildAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), workingDirectory, string.Concat("/c az ", arguments), cancellationToken);
        
        private static Task<RunDotnetResult> RunMSBuildAsync(string workingDirectory, string arguments, CancellationToken cancellationToken) 
            => RunBuildAsync("C:\\Program Files\\Microsoft Visual Studio\\18\\Insiders\\MSBuild\\Current\\Bin\\MSBuild.exe", workingDirectory, arguments, cancellationToken);

        private static async Task<RunDotnetResult> RunBuildAsync(string command, string workingDirectory, string arguments, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
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