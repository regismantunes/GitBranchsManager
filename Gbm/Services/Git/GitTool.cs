using System.Diagnostics;

namespace Gbm.Services.Git
{
    public class GitTool : IGitTool
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

        public void SetRepository(string repository)
        {
            var workingDirectory = Path.Combine(BasePath, repository);
            if (!Directory.Exists(workingDirectory)) throw new DirectoryNotFoundException($"The directory '{workingDirectory}' does not exist.");
            WorkingDirectory = workingDirectory;
        }

        public async Task<string> GetMainBranchAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                DisableShowGitOutput();
                var gitResult = await RunGitAsync("branch --list", cancellationToken);
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

        public async Task CheckoutToMainAsync(CancellationToken cancellationToken = default)
        {
            var mainBranch = await GetMainBranchAsync(cancellationToken);
            await CheckoutAsync(mainBranch, cancellationToken);
        }

        public async Task CheckoutAsync(string branch, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (await RunGitOkAsync($"checkout {branch}", cancellationToken)) return;
                MyConsole.WriteError($"❌ It was not possible to checkout to '{branch}' branch.");
                MyConsole.WriteError("🛑 Please resolve the not commited files, then press ENTER to continue...");
                MyConsole.ReadLineThenClear();
            }
        }

        public async Task GetMainChangesAsync(CancellationToken cancellationToken = default)
        {
            var mainBranch = await GetMainBranchAsync(cancellationToken);
            await PullOriginAsync(branchFrom: mainBranch, cancellationToken);
        }

        public async Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                DisableShowGitOutput();
                var branch = await RunGitAsync("rev-parse --abbrev-ref HEAD", cancellationToken);
                if (string.IsNullOrWhiteSpace(branch))
                    throw new InvalidOperationException("Could not determine the current branch. Please ensure you are in a valid Git repository.");
                return branch.Trim();
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public async Task PullOriginAsync(string branchFrom, CancellationToken cancellationToken = default)
        {
            var branchTo = await GetCurrentBranchAsync(cancellationToken);
            if (await RunGitOkAsync($"pull origin {branchFrom}", cancellationToken)) return;
            MyConsole.WriteError($"❌ Merge conflict detected while getting changes from '{branchFrom}' into '{branchTo}'.");
            MyConsole.WriteError("🛑 Please resolve the conflicts manually, then press ENTER to continue...");
            MyConsole.ReadLineThenClear();
            while (await HasUncommittedChangesAsync(cancellationToken))
            {
                MyConsole.WriteError($"❌ There is still uncommitted changes.");
                MyConsole.WriteError("🛑 Please resolve the not commited files, then press ENTER to continue...");
                MyConsole.ReadLineThenClear();
            }
        }

        public async Task PullAsync(CancellationToken cancellationToken = default)
        {
            await RunGitAsync("pull", cancellationToken);
        }

        public async Task CheckoutNewBranchAsync(string branch, CancellationToken cancellationToken = default)
        {
            await RunGitAsync($"checkout -b {branch}", cancellationToken);
        }

        public async Task PushAsync(CancellationToken cancellationToken = default)
        {
            var branch = await GetCurrentBranchAsync(cancellationToken);
            var branchRemoteExists = await BranchExistsRemotelyAsync(branch, cancellationToken);

            await RunGitAsync(branchRemoteExists ?
                "push" :
                $"push --set-upstream origin {branch}", cancellationToken);
        }

        public async Task<bool> BranchExistsRemotelyAsync(string branch, CancellationToken cancellationToken = default)
        {
            try
            {
                DisableShowGitOutput();
                var result = await RunGitAsync($"ls-remote --heads origin {branch}", cancellationToken);
                return !string.IsNullOrWhiteSpace(result);
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public async Task DeleteLocalBranchAsync(string branch, CancellationToken cancellationToken = default)
        {
            var currentBranch = await GetCurrentBranchAsync(cancellationToken);
            if (currentBranch == branch)
                await CheckoutToMainAsync(cancellationToken);
            await RunGitAsync($"branch -D {branch}", cancellationToken);
        }

        public async Task<bool> HasUncommittedChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                DisableShowGitOutput();
                var status = await RunGitAsync("status --porcelain", cancellationToken);
                return !string.IsNullOrWhiteSpace(status);
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public async Task<bool> BranchExistsAsync(string branch, CancellationToken cancellationToken = default)
        {
            try
            {
                DisableShowGitOutput();
                var result = await RunGitAsync($"branch --list {branch}", cancellationToken);
                return !string.IsNullOrWhiteSpace(result);
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public async Task<IEnumerable<string>> GetRepositoriesWithBranchAsync(string branch, CancellationToken cancellationToken = default)
        {
            MyConsole.WriteStep($"🔎 Searching for repositories with branch {branch}...");
            var repositoriesWithBranch = new List<string>();
            var directories = Directory.GetDirectories(BasePath);
            foreach (var directory in directories)
            {
                var repositoryName = Path.GetFileName(directory);
                if (string.IsNullOrWhiteSpace(repositoryName)) continue;
                SetRepository(repositoryName);
                if (await BranchExistsAsync(branch, cancellationToken))
                {
                    repositoriesWithBranch.Add(repositoryName);
                }
            }
            MyConsole.BackToPreviousLine();
            return repositoriesWithBranch;
        }

        private async Task<bool> RunGitOkAsync(string arguments, CancellationToken cancellationToken = default)
        {
            var result = await RunGitAndGetResultAsync(arguments, cancellationToken);
            return result.ExitCode == 0;
        }

        private async Task<string> RunGitAsync(string arguments, CancellationToken cancellationToken = default)
        {
            var result = await RunGitAndGetResultAsync(arguments, cancellationToken);
            return result.Output;
        }

        private async Task<RunGitResult> RunGitAndGetResultAsync(string arguments, CancellationToken cancellationToken = default)
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
            await process.WaitForExitAsync(cancellationToken);
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
