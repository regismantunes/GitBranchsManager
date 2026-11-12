using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gbm.Services.Git;

public class GitTool : IGitTool
{
    public string? WorkingDirectory { get; private set; }
    public string BasePath { get; set; }
    public bool ShowGitOutput { get; set; } = true;

    public GitTool(string basePath)
    {
        if (!Directory.Exists(basePath)) throw new DirectoryNotFoundException($"The directory '{basePath}' does not exist.");
        BasePath = Path.GetFullPath(basePath);
    }

    private string RepositoryPath(string repository)
    {
        if (string.IsNullOrWhiteSpace(repository) || repository is "." or ".." || repository.IndexOfAny(['/', '\\', ':']) >= 0)
            throw new ArgumentException($"Invalid repository name '{repository}'.");
        var path = Path.Combine(BasePath, repository);
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"The directory '{path}' does not exist.");
        return path;
    }

    public async Task SetRepositoryAsync(string repository, CancellationToken cancellationToken = default)
    {
        var path = RepositoryPath(repository);
        if (!await IsGitRepositoryAsync(path, cancellationToken))
            throw new InvalidOperationException($"The directory '{path}' is not a Git repository.");
        WorkingDirectory = path;
    }

    public async IAsyncEnumerable<string> GetAllRepositoriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var directory in Directory.GetDirectories(BasePath).Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await IsGitRepositoryAsync(directory, cancellationToken)) yield return Path.GetFileName(directory);
        }
    }

    public async Task<bool> IsGitRepositoryAsync(string workingDirectory, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(workingDirectory, ["rev-parse", "--is-inside-work-tree"], cancellationToken, false);
        if (result.ExitCode == 0) return result.Output.Trim() == "true";
        var marker = Path.Combine(workingDirectory, ".git");
        if (!File.Exists(marker) && !Directory.Exists(marker) && result.Error.Contains("not a git repository", StringComparison.OrdinalIgnoreCase))
            return false;
        throw GitError(workingDirectory, result);
    }

    public async Task ValidateBranchNameAsync(string branch, CancellationToken cancellationToken = default)
    {
        CheckBranchArgument(branch);
        await CheckedAsync(BasePath, ["check-ref-format", "--branch", branch], cancellationToken, false);
    }

    private static void CheckBranchArgument(string branch)
    {
        if (string.IsNullOrWhiteSpace(branch) || branch.StartsWith('-') || branch.StartsWith("@{", StringComparison.Ordinal))
            throw new ArgumentException($"Invalid branch name '{branch}'.");
    }

    public async Task<RemoteBranchDiscoveryResult> GetRemoteBranchNamesAsync(string repository, CancellationToken cancellationToken = default)
    {
        var path = RepositoryPath(repository);
        var remotes = await CheckedAsync(path, ["remote"], cancellationToken, false);
        if (!remotes.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Contains("origin", StringComparer.Ordinal))
            return new(false, []);
        var output = await CheckedAsync(path, ["ls-remote", "--heads", "origin"], cancellationToken, false);
        return new(true, ParseRemoteHeads(output));
    }

    internal static IReadOnlyList<string> ParseRemoteHeads(string output)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            const string prefix = "refs/heads/";
            if (fields.Length != 2 || !fields[0].All(Uri.IsHexDigit) || !fields[1].StartsWith(prefix, StringComparison.Ordinal) || fields[1].Length == prefix.Length)
                throw new InvalidDataException("Invalid response while reading remote heads.");
            names.Add(fields[1][prefix.Length..]);
        }
        return names.Order(StringComparer.Ordinal).ToArray();
    }

    public async Task<bool> BranchExistsAsync(string branch, CancellationToken cancellationToken = default)
    {
        CheckBranchArgument(branch);
        // Enumerate exact refs rather than interpreting the branch as a glob or revision.
        var output = await CheckedAsync(CurrentDirectory(), ["for-each-ref", "--format=%(refname)", "refs/heads/"], cancellationToken, false);
        return output.Split('\n', StringSplitOptions.TrimEntries).Contains($"refs/heads/{branch}", StringComparer.Ordinal);
    }

    public async Task<bool> BranchExistsRemotelyAsync(string branch, CancellationToken cancellationToken = default)
    {
        CheckBranchArgument(branch);
        var output = await CheckedAsync(CurrentDirectory(), ["ls-remote", "--heads", "origin"], cancellationToken, false);
        return ParseRemoteHeads(output).Contains(branch, StringComparer.Ordinal);
    }

    public async Task<string> GetMainBranchAsync(CancellationToken cancellationToken = default)
    {
        foreach (var branch in new[] { "master", "main" })
            if (await BranchExistsAsync(branch, cancellationToken)) return branch;
        throw new InvalidOperationException($"Could not determine the main branch in '{WorkingDirectory}'.");
    }

    public async Task CheckoutToMainAsync(CancellationToken cancellationToken = default)
    {
        if (!await CheckoutAsync(await GetMainBranchAsync(cancellationToken), cancellationToken))
            throw new InvalidOperationException("Main branch disappeared before checkout.");
    }

    public async Task<bool> CheckoutAsync(string branch, CancellationToken cancellationToken = default)
    {
        if (!await BranchExistsAsync(branch, cancellationToken)) return false;
        await CheckedAsync(CurrentDirectory(), ["checkout", "--no-guess", branch], cancellationToken);
        return true;
    }

    public async Task CheckoutNewBranchAsync(string branch, CancellationToken cancellationToken = default)
    {
        await ValidateBranchNameAsync(branch, cancellationToken);
        await CheckedAsync(CurrentDirectory(), ["checkout", "-b", branch], cancellationToken);
    }

    public async Task CheckoutRemoteBranchAsync(string branch, CancellationToken cancellationToken = default)
    {
        await ValidateBranchNameAsync(branch, cancellationToken);
        await CheckedAsync(CurrentDirectory(), ["fetch", "origin", $"refs/heads/{branch}:refs/remotes/origin/{branch}"], cancellationToken);
        await CheckedAsync(CurrentDirectory(), ["checkout", "--track", "-b", branch, $"origin/{branch}"], cancellationToken);
    }

    public async Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default)
        => (await CheckedAsync(CurrentDirectory(), ["symbolic-ref", "--quiet", "--short", "HEAD"], cancellationToken, false)).Trim();

    public async Task GetMainChangesAsync(CancellationToken cancellationToken = default)
        => await PullOriginAsync(await GetMainBranchAsync(cancellationToken), cancellationToken);

    public async Task PullAsync(CancellationToken cancellationToken = default)
        => await CheckedAsync(CurrentDirectory(), ["pull"], cancellationToken);

    public async Task PullOriginAsync(string branchFrom, CancellationToken cancellationToken = default)
    {
        CheckBranchArgument(branchFrom);
        var result = await RunAsync(CurrentDirectory(), ["pull", "origin", branchFrom], cancellationToken);
        if (result.ExitCode == 0) return;
        var conflicts = await CheckedAsync(CurrentDirectory(), ["diff", "--name-only", "--diff-filter=U"], cancellationToken, false);
        if (string.IsNullOrWhiteSpace(conflicts)) throw GitError(CurrentDirectory(), result);
        do
        {
            MyConsole.WriteError($"Resolve and commit merge conflicts in '{WorkingDirectory}', then press ENTER (EOF cancels).");
            if (await MyConsole.ReadLineOrEndAsync(cancellationToken) is null) throw new OperationCanceledException("Merge resolution cancelled.");
        } while (await HasUncommittedChangesAsync(cancellationToken) || await HasMergeInProgressAsync(cancellationToken));
    }

    private async Task<bool> HasMergeInProgressAsync(CancellationToken cancellationToken)
        => (await RunAsync(CurrentDirectory(), ["rev-parse", "--verify", "--quiet", "MERGE_HEAD"], cancellationToken, false)).ExitCode == 0;

    public async Task PushAsync(CancellationToken cancellationToken = default)
    {
        var branch = await GetCurrentBranchAsync(cancellationToken);
        await CheckedAsync(CurrentDirectory(), ["push", "--set-upstream", "origin", $"refs/heads/{branch}:refs/heads/{branch}"], cancellationToken);
    }

    public async Task DeleteLocalBranchAsync(string branch, CancellationToken cancellationToken = default)
    {
        CheckBranchArgument(branch);
        if (await GetCurrentBranchAsync(cancellationToken) == branch) await CheckoutToMainAsync(cancellationToken);
        await CheckedAsync(CurrentDirectory(), ["branch", "-D", "--", branch], cancellationToken);
    }

    public async Task<bool> HasUncommittedChangesAsync(CancellationToken cancellationToken = default)
        => !string.IsNullOrWhiteSpace(await CheckedAsync(CurrentDirectory(), ["status", "--porcelain"], cancellationToken, false));

    public async Task RestoreTrackedChangesAsync(CancellationToken cancellationToken = default)
    {
        await CheckedAsync(CurrentDirectory(), ["restore", "--worktree", "--", "."], cancellationToken);
        await CheckedAsync(CurrentDirectory(), ["clean", "-f"], cancellationToken);
    }

    public async Task<IEnumerable<string>> GetRepositoriesWithBranchAsync(string branch, CancellationToken cancellationToken = default, bool includeRemote = false)
    {
        var previous = WorkingDirectory;
        var found = new List<string>();
        try
        {
            await foreach (var repo in GetAllRepositoriesAsync(cancellationToken))
            {
                await SetRepositoryAsync(repo, cancellationToken);
                if (await BranchExistsAsync(branch, cancellationToken) ||
                    (includeRemote && (await GetRemoteBranchNamesAsync(repo, cancellationToken)).BranchNames.Contains(branch, StringComparer.Ordinal)))
                    found.Add(repo);
            }
        }
        finally { WorkingDirectory = previous; }
        return found;
    }

    private string CurrentDirectory() => WorkingDirectory ?? throw new InvalidOperationException("Repository is not set. Use SetRepositoryAsync first.");

    private async Task<string> CheckedAsync(string directory, string[] arguments, CancellationToken cancellationToken, bool? showOutput = null)
    {
        var result = await RunAsync(directory, arguments, cancellationToken, showOutput);
        if (result.ExitCode != 0) throw GitError(directory, result);
        return result.Output;
    }

    private static InvalidOperationException GitError(string directory, GitResult result)
        => new($"Git failed in '{directory}' (exit {result.ExitCode}): {result.Error.Trim()} {result.Output.Trim()}");

    private async Task<GitResult> RunAsync(string directory, string[] arguments, CancellationToken cancellationToken, bool? showOutput = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Git.");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        try { await process.WaitForExitAsync(cancellationToken); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            try { await Task.WhenAll(output, error); } catch (OperationCanceledException) { }
            throw;
        }
        var result = new GitResult(process.ExitCode, await output, await error);
        if (showOutput ?? ShowGitOutput)
        {
            if (!string.IsNullOrWhiteSpace(result.Output)) MyConsole.WriteInfo(result.Output.TrimEnd());
            if (!string.IsNullOrWhiteSpace(result.Error)) MyConsole.WriteInfo(result.Error.TrimEnd());
        }
        return result;
    }

    private record GitResult(int ExitCode, string Output, string Error);
}
