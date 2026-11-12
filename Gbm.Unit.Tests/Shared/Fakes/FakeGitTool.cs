using System.Runtime.CompilerServices;
using Gbm.Services.Git;

namespace Gbm.Unit.Tests.Shared.Fakes;

public record GitCall(string Operation, string? Repository, string? Branch);

public class FakeGitTool : IGitTool
{
    public Dictionary<string, HashSet<string>> Local { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string[]?> Remote { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Current { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<GitCall> Calls { get; } = [];
    public HashSet<string> RemoteFailures { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string? FailOperation { get; set; }
    public string? FailRepository { get; set; }
    public string? RejectCheckoutIn { get; set; }
    public string BasePath { get; set; } = Path.GetTempPath();
    public bool ShowGitOutput { get; set; }
    public string? WorkingDirectory { get; private set; }

    public void AddRepository(string repo, string[]? local = null, string[]? remote = null, bool hasOrigin = true)
    {
        Local[repo] = new(local ?? ["main"], StringComparer.Ordinal);
        Remote[repo] = hasOrigin ? remote ?? [] : null;
        Current[repo] = Local[repo].FirstOrDefault() ?? "main";
    }

    public Task SetRepositoryAsync(string repository, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Local.ContainsKey(repository)) throw new DirectoryNotFoundException(repository);
        WorkingDirectory = Local.Keys.First(r => r.Equals(repository, StringComparison.OrdinalIgnoreCase));
        Record("set");
        return Task.CompletedTask;
    }

    private void Record(string operation, string? branch = null)
    {
        Calls.Add(new(operation, WorkingDirectory, branch));
        if (operation == FailOperation && (FailRepository is null || WorkingDirectory == FailRepository))
            throw new InvalidOperationException($"Simulated {operation} failure.");
    }

    public Task<RemoteBranchDiscoveryResult> GetRemoteBranchNamesAsync(string repository, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add(new("remote", repository, null));
        if (RemoteFailures.Contains(repository)) throw new IOException("Simulated remote failure.");
        return Task.FromResult(new RemoteBranchDiscoveryResult(Remote[repository] is not null, Remote[repository] ?? []));
    }

    public Task ValidateBranchNameAsync(string branch, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Record("validate", branch);
        if (string.IsNullOrWhiteSpace(branch) || branch.Contains(' ') || branch.StartsWith('-')) throw new ArgumentException("Invalid branch.");
        return Task.CompletedTask;
    }

    public Task<bool> BranchExistsAsync(string branch, CancellationToken cancellationToken = default)
    {
        Record("exists", branch);
        return Task.FromResult(Local[WorkingDirectory!].Contains(branch));
    }

    public async Task<bool> BranchExistsRemotelyAsync(string branch, CancellationToken cancellationToken = default)
        => (await GetRemoteBranchNamesAsync(WorkingDirectory!, cancellationToken)).BranchNames.Contains(branch, StringComparer.Ordinal);

    public Task<bool> CheckoutAsync(string branch, CancellationToken cancellationToken = default)
    {
        Record("checkout", branch);
        if (WorkingDirectory == RejectCheckoutIn || !Local[WorkingDirectory!].Contains(branch)) return Task.FromResult(false);
        Current[WorkingDirectory!] = branch;
        return Task.FromResult(true);
    }

    public Task CheckoutNewBranchAsync(string branch, CancellationToken cancellationToken = default)
    {
        Record("create", branch);
        Local[WorkingDirectory!].Add(branch);
        Current[WorkingDirectory!] = branch;
        return Task.CompletedTask;
    }

    public Task CheckoutRemoteBranchAsync(string branch, CancellationToken cancellationToken = default)
    {
        Record("track", branch);
        Local[WorkingDirectory!].Add(branch);
        Current[WorkingDirectory!] = branch;
        return Task.CompletedTask;
    }

    public async Task CheckoutToMainAsync(CancellationToken cancellationToken = default)
    {
        if (!await CheckoutAsync(await GetMainBranchAsync(cancellationToken), cancellationToken)) throw new InvalidOperationException("Checkout failed.");
    }

    public Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default) => Task.FromResult(Current[WorkingDirectory!]);
    public Task<string> GetMainBranchAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Local[WorkingDirectory!].Contains("master") ? "master" : "main");
    public Task GetMainChangesAsync(CancellationToken cancellationToken = default) { Record("main"); return Task.CompletedTask; }
    public Task PullAsync(CancellationToken cancellationToken = default) { Record("pull", Current[WorkingDirectory!]); return Task.CompletedTask; }
    public Task PullOriginAsync(string branchFrom, CancellationToken cancellationToken = default) { Record("merge", branchFrom); return Task.CompletedTask; }
    public Task PushAsync(CancellationToken cancellationToken = default) { Record("push", Current[WorkingDirectory!]); return Task.CompletedTask; }
    public Task RestoreTrackedChangesAsync(CancellationToken cancellationToken = default) { Record("restore"); return Task.CompletedTask; }
    public Task<bool> HasUncommittedChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task DeleteLocalBranchAsync(string branch, CancellationToken cancellationToken = default)
    {
        Record("delete", branch);
        Local[WorkingDirectory!].Remove(branch);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetRepositoriesWithBranchAsync(string branch, CancellationToken cancellationToken = default, bool includeRemote = false)
        => Task.FromResult(Local.Where(p => p.Value.Contains(branch) || (includeRemote && (Remote[p.Key]?.Contains(branch) ?? false))).Select(p => p.Key));

    public async IAsyncEnumerable<string> GetAllRepositoriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        foreach (var repo in Local.Keys.Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return repo;
        }
    }
}
