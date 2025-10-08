using System.Diagnostics;
using System.Threading.Tasks;

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

        public void SetRepository(string repository)
        {
            var workingDirectory = Path.Combine(BasePath, repository);
            if (!Directory.Exists(workingDirectory)) throw new DirectoryNotFoundException($"The directory '{workingDirectory}' does not exist.");
            WorkingDirectory = workingDirectory;
        }

        public string GetMainBranch()
        {
            try
            {
                DisableShowGitOutput();
                var branchs = RunGit("branch --list")
                    .Split('\n')
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

        public void CheckoutToMain()
        {
            var mainBranch = GetMainBranch();
            Checkout(mainBranch);
        }

        public void Checkout(string branch)
        {
            while (true)
            {
                if (RunGitOk($"checkout {branch}")) return;
                MyConsole.WriteError($"❌ It was not possible to checkout to '{branch}' branch.");
                MyConsole.WriteError("🛑 Please resolve the not commited files, then press ENTER to continue...");
                Console.ReadLine();
            }
        }

        public void GetMainChanges()
        {
            var mainBranch = GetMainBranch();
            PullOrigin(branchFrom: mainBranch);
        }

        public string GetCurrentBranch()
        {
            try
            {
                DisableShowGitOutput();
                var branch = RunGit("rev-parse --abbrev-ref HEAD").Trim();
                if (string.IsNullOrWhiteSpace(branch))
                    throw new InvalidOperationException("Could not determine the current branch. Please ensure you are in a valid Git repository.");
                return branch;
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public void PullOrigin(string branchFrom)
        {
            var branchTo = GetCurrentBranch();
            if (RunGitOk($"pull origin {branchFrom}")) return;
            MyConsole.WriteError($"❌ Merge conflict detected while getting changes from '{branchFrom}' into '{branchTo}'.");
            MyConsole.WriteError("🛑 Please resolve the conflicts manually, then press ENTER to continue...");
            Console.ReadLine();
            while (HasUncommittedChanges())
            {
                MyConsole.WriteError($"❌ There is still uncommitted changes.");
                MyConsole.WriteError("🛑 Please resolve the not commited files, then press ENTER to continue...");
                Console.ReadLine();
            }
        }

        public void Pull()
        {
            RunGit("pull");
        }

        public void CheckoutNewBranch(string branch)
        {
            RunGit($"checkout -b {branch}");
        }

        public void Push()
        {
            var branch = GetCurrentBranch();
            var branchRemoteExists = BranchExistsRemotely(branch);
            
            RunGit(branchRemoteExists ?
                "push" :
                $"push --set-upstream origin {branch}");
        }

        public bool BranchExistsRemotely(string branch)
        {
            try
            {
                DisableShowGitOutput();
                var result = RunGit($"ls-remote --heads origin {branch}");
                return !string.IsNullOrWhiteSpace(result);
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public void DeleteLocalBranch(string branch)
        {
            var currentBranch = GetCurrentBranch();
            if (currentBranch == branch)
                CheckoutToMain();
            RunGit($"branch -d {branch}");
        }

        public bool HasUncommittedChanges()
        {
            try
            {
                DisableShowGitOutput();
                var status = RunGit("status --porcelain");
                return !string.IsNullOrWhiteSpace(status);
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        public bool BranchExists(string branch)
        {
            try
            {
                DisableShowGitOutput();
                var result = RunGit($"branch --list {branch}");
                return !string.IsNullOrWhiteSpace(result);
            }
            finally
            {
                RestorePreviousShowGitOutput();
            }
        }

        private bool RunGitOk(string arguments)
        {
            RunGit(arguments, out int exitCode);
            return exitCode == 0;
        }

        private string RunGit(string arguments)
        {
            return RunGit(arguments, out int _);
        }

        private string RunGit(string arguments, out int exitCode)
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
            process.WaitForExit();
            exitCode = process.ExitCode;
            if (ShowGitOutput)
            {
                if (!string.IsNullOrWhiteSpace(stderr)) MyConsole.WriteInfo(stderr.TrimEnd());
                if (!string.IsNullOrWhiteSpace(stdout)) MyConsole.WriteInfo(stdout.TrimEnd());
            }
            return stdout;
        }
    }
}
