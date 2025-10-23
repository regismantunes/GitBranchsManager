using System.Collections.Concurrent;
using Gbm.Services.Git;

namespace Gbm.Unit.Tests.Shared.Fakes;

public class FakeGitTool : IGitTool
{
    private readonly ConcurrentDictionary<string, string[]> _reposByBranch = new();

    public string BasePath { get; set; } = string.Empty;
    public bool ShowGitOutput { get; set; }
    public string? WorkingDirectory => null;

    public void Seed(string branch, params string[] repositories)
    {
        _reposByBranch[branch] = repositories;
    }

    public Task<bool> BranchExistsAsync(string branch, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> BranchExistsRemotelyAsync(string branch, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task CheckoutAsync(string branch, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CheckoutNewBranchAsync(string branch, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CheckoutToMainAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteLocalBranchAsync(string branch, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default) => Task.FromResult("main");
    public Task<string> GetMainBranchAsync(CancellationToken cancellationToken = default) => Task.FromResult("main");
    public Task GetMainChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> HasUncommittedChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task PullAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PullOriginAsync(string branchFrom, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void SetRepository(string repository) { }
    public Task<IEnumerable<string>> GetRepositoriesWithBranchAsync(string branch, CancellationToken cancellationToken = default)
    {
        _reposByBranch.TryGetValue(branch, out var repos);
        return Task.FromResult<IEnumerable<string>>(repos ?? Array.Empty<string>());
    }
}
