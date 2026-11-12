using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Build;
using Gbm.Services.GitHub;
using Gbm.Services.Jira;
using Gbm.Services.Tasks;
using Microsoft.Extensions.Configuration;

namespace Gbm.Unit.Tests.Shared.Fakes;

public sealed class TaskWorld
{
    public FakeTaskInfoRepository Repository { get; } = new();
    public FakeGitTool Git { get; } = new();
    public TestJira Jira { get; } = new();
    public TestSelection Selection { get; } = new();
    public TestBuild Build { get; } = new();
    public TestGitHub GitHub { get; } = new();
    public TestPullRequests PullRequests { get; } = new();
    public RepositoryCatalog Catalog { get; }
    public TaskBranchDiscovery Discovery { get; }
    public TaskResolver Resolver { get; }
    public TaskBranchTargetResolver Targets { get; }
    public IConfiguration Configuration { get; }
    public TaskBranchNameFactory Names { get; }
    public TaskBranchCreator Creator { get; }

    public TaskWorld(string? format = "feature/{TaskId}")
    {
        Catalog = new(Git);
        Discovery = new(Git, Catalog, Selection);
        Resolver = new(Repository, Jira, Discovery);
        Targets = new(Git, Catalog);
        Configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["BranchDefaultNameFormat"] = format }).Build();
        Names = new(Configuration);
        Creator = new(Git, Repository, Catalog, Names);
    }

    public TaskInfo Seed(string id = "PROJ-123", params RepositoryBranch[] branches)
    {
        var task = new TaskInfo(id, "Summary", "Description", $"https://jira/{id}", [.. branches]);
        Repository.Seed(task);
        return task;
    }

    public TaskInfo SeedTwo()
    {
        Git.AddRepository("Api", ["main", "develop", "feature/PROJ-123-api"], ["feature/PROJ-123-api"]);
        Git.AddRepository("Web", ["main", "develop", "fix/PROJ-123-ui"], ["fix/PROJ-123-ui"]);
        return Seed("PROJ-123", new("Api", "feature/PROJ-123-api"), new("Web", "fix/PROJ-123-ui"));
    }
}

public sealed class TestJira : IJiraClient
{
    public TaskInfo? Result { get; set; }
    public Exception? Error { get; set; }
    public int Calls { get; private set; }
    public Task<TaskInfo?> GetTaskInfoFromJiraAsync(string taskId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        if (Error is not null) throw Error;
        return Task.FromResult(Result);
    }
}

public sealed class TestSelection : IBranchSelection
{
    public Func<string, IReadOnlyList<string>, string> Choose { get; set; } = (_, names) => names[0];
    public List<(string Repository, string[] Candidates)> Calls { get; } = [];
    public Task<string> SelectAsync(string repository, IReadOnlyList<string> candidates, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add((repository, candidates.ToArray()));
        return Task.FromResult(Choose(repository, candidates));
    }
}

public sealed class TestBuild : IBuildTool
{
    public string? FailRepository { get; set; }
    public List<(string Repository, string? Branch)> Calls { get; } = [];
    public Action? OnBuild { get; set; }
    public Task<bool> BuildBranchAsync(string repo, string branchName, CancellationToken cancellationToken = default)
    {
        Calls.Add((repo, branchName)); OnBuild?.Invoke(); return Task.FromResult(repo != FailRepository);
    }
    public Task<bool> BuildRepositoryAsync(string repo, CancellationToken cancellationToken = default)
    {
        Calls.Add((repo, null)); OnBuild?.Invoke(); return Task.FromResult(repo != FailRepository);
    }
}

public sealed class TestGitHub : IGitHubClient
{
    public string Owner => "test";
    public List<(string Repository, string Branch, string Base)> Created { get; } = [];
    public List<(string Repository, string Links)> Updated { get; } = [];
    public string? FailRepository { get; set; }
    public Task<PullRequestInfo> CreatePullRequestAsync(string repo, string branchName, string baseBranch, TaskInfo taskInfo, CancellationToken cancellationToken = default)
    {
        if (repo == FailRepository) throw new IOException("Simulated GitHub failure.");
        Created.Add((repo, branchName, baseBranch));
        return Task.FromResult(new PullRequestInfo(taskInfo.Id, repo, Created.Count, $"https://github.test/{repo}/pull/{Created.Count}"));
    }
    public Task UpdatePullRequestAsync(string repo, int prNumber, string relatedPRsText, TaskInfo taskInfo, CancellationToken cancellationToken = default)
    {
        Updated.Add((repo, relatedPRsText)); return Task.CompletedTask;
    }
}

public sealed class TestPullRequests : IPullRequestInfoRepository
{
    public List<PullRequestInfo> Items { get; } = [];
    public Task<PullRequestInfo?> GetAsync(string taskId, string repository, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Find(p => p.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase) && p.Repository.Equals(repository, StringComparison.OrdinalIgnoreCase)));
    public Task SaveAsync(string taskId, string repository, int number, string url, CancellationToken cancellationToken = default)
        => SaveAsync(new PullRequestInfo(taskId, repository, number, url), cancellationToken);
    public Task SaveAsync(PullRequestInfo prInfo, CancellationToken cancellationToken = default) { Items.Add(prInfo); return Task.CompletedTask; }
    public Task<List<PullRequestInfo>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Where(p => p.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase)).ToList());
    public async Task<bool> ExistsAsync(string taskId, string repository, CancellationToken cancellationToken = default)
        => await GetAsync(taskId, repository, cancellationToken) is not null;
}
