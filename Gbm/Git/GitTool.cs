using System.Diagnostics;

namespace Gbm.Git
{
    public class GitTool
    {
        public string? WorkingDirectory { get; private set; }
        public string BasePath { get; set; }

        public bool ShowGitOutput { get; set; } = true;
        private bool PreviousShowGitOutput { get; set; } = true;

        public GitTool(string basePath)
        {
            if (!Directory.Exists(basePath)) throw new DirectoryNotFoundException($"The directory '{basePath}' does not exist.");
            BasePath = basePath;
        }

        private void DisableShowGitOutput()
        {
            PreviousShowGitOutput = ShowGitOutput;
            ShowGitOutput = false;
        }

        private void RestorePreviousShowGitOutput()
        {
            ShowGitOutput = PreviousShowGitOutput;
        }

        public string GetBranchNameFromTaskId(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
                throw new ArgumentException("Task ID cannot be null or empty.", nameof(taskId));

            return $"feature/{taskId}";
        }

        public void SetRepository(string repository)
        {
            var workingDirectory = Path.Combine(BasePath, repository);
            if (!Directory.Exists(workingDirectory)) throw new DirectoryNotFoundException($"The directory '{workingDirectory}' does not exist.");
            WorkingDirectory = workingDirectory;
        }

        public async Task<string> GetMainBranchAsync()
        {
            try
            {
                DisableShowGitOutput();
                var gitResult = await RunGitAsync("branch --list");
                var branchs = gitResult
                    .Split('\n')
                    .Select(b => b.Replace("* ", string.Empty))
                    .Select(b => b.Trim());
                if (!branchs.Any())
                    throw new InvalidOperationException("Could not determine the main branch. Please ensure you are in a valid Git repository.");

                var mainBranch = "master";
                if (branchs.Contains(mainBranch))
                    return mainBranch;

                mainBranch = "main";
                if (branchs.Contains(mainBranch))
                    return mainBranch;

                throw new InvalidOperationException("Could not determine the main branch. Please ensure you are in a valid Git repository.");
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public async Task CheckoutToMainAsync()
        {
            var mainBranch = await GetMainBranchAsync();
            await CheckoutAsync(mainBranch);
        }

        public async Task CheckoutAsync(string branch)
        {
            while (true)
            {
                if (await RunGitOkAsync($"checkout {branch}")) return;
                MyConsole.WriteError($"❌ It was not possible to checkout to '{branch}' branch.");
                MyConsole.WriteError("🛑 Please resolve the not commited files, then press ENTER to continue...");
                MyConsole.ReadLineThenClear();
            }
        }

        public async Task GetMainChangesAsync()
        {
            var mainBranch = await GetMainBranchAsync();
            await PullOriginAsync(branchFrom: mainBranch);
        }

        public async Task<string> GetCurrentBranchAsync()
        {
            try
            {
                DisableShowGitOutput();
                var branch = await RunGitAsync("rev-parse --abbrev-ref HEAD");
                if (string.IsNullOrWhiteSpace(branch))
                    throw new InvalidOperationException("Could not determine the current branch. Please ensure you are in a valid Git repository.");
                return branch.Trim();
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public async Task PullOriginAsync(string branchFrom)
        {
            var branchTo = await GetCurrentBranchAsync();
            if (await RunGitOkAsync($"pull origin {branchFrom}")) return;
            MyConsole.WriteError($"❌ Merge conflict detected while getting changes from '{branchFrom}' into '{branchTo}'.");
            MyConsole.WriteError("🛑 Please resolve the conflicts manually, then press ENTER to continue...");
            MyConsole.ReadLineThenClear();
            while (await HasUncommittedChangesAsync())
            {
                MyConsole.WriteError($"❌ There is still uncommitted changes.");
                MyConsole.WriteError("🛑 Please resolve the not commited files, then press ENTER to continue...");
                MyConsole.ReadLineThenClear();
            }
        }

        public async Task PullAsync()
        {
            await RunGitAsync("pull");
        }

        public async Task CheckoutNewBranchAsync(string branch)
        {
            await RunGitAsync($"checkout -b {branch}");
        }

        public async Task PushAsync()
        {
            var branch = await GetCurrentBranchAsync();
            var branchRemoteExists = await BranchExistsRemotelyAsync(branch);
            
            await RunGitAsync(branchRemoteExists ?
                "push" :
                $"push --set-upstream origin {branch}");
        }

        public async Task<bool> BranchExistsRemotelyAsync(string branch)
        {
            try
            {
                DisableShowGitOutput();
                var result = await RunGitAsync($"ls-remote --heads origin {branch}");
                return !string.IsNullOrWhiteSpace(result);
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public async Task DeleteLocalBranchAsync(string branch)
        {
            var currentBranch = await GetCurrentBranchAsync();
            if (currentBranch == branch)
                await CheckoutToMainAsync();
            await RunGitAsync($"branch -d {branch}");
        }

        public async Task<bool> HasUncommittedChangesAsync()
        {
            try
            {
                DisableShowGitOutput();
                var status = await RunGitAsync("status --porcelain");
                return !string.IsNullOrWhiteSpace(status);
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public async Task<bool> BranchExistsAsync(string branch)
        {
            try
            {
                DisableShowGitOutput();
                var result = await RunGitAsync($"branch --list {branch}");
                return !string.IsNullOrWhiteSpace(result);
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        private async Task<bool> RunGitOkAsync(string arguments)
        {
            var result = await RunGitAndGetResultAsync(arguments);
            return result.ExitCode == 0;
        }

        private async Task<string> RunGitAsync(string arguments)
        {
            var result = await RunGitAndGetResultAsync(arguments);
            return result.Output;
        }

        private async Task<RunGitResult> RunGitAndGetResultAsync(string arguments)
        {
            if (string.IsNullOrEmpty(WorkingDirectory))
                throw new InvalidOperationException("Repository is not set. Please set it using SetRepository method.");

            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            await process.WaitForExitAsync();
            var exitCode = process.ExitCode;
            if (ShowGitOutput)
            {
                if (!string.IsNullOrWhiteSpace(stderr)) MyConsole.WriteInfo(stderr.TrimEnd());
                if (!string.IsNullOrWhiteSpace(stdout)) MyConsole.WriteInfo(stdout.TrimEnd());
            }
            return new RunGitResult { ExitCode = exitCode, Output = stdout };
        }

        private struct RunGitResult
        {
            public string Output { get; set; }
            public int ExitCode { get; set; }
        }
    }
}
