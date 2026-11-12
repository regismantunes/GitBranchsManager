using Gbm.Persistence.Entities;
using Gbm.Services.Git;
using Gbm.Unit.Tests.Shared.Fakes;

namespace Gbm.Unit.Tests.Services.Tasks;

public class TaskResolverTests
{
    [Fact]
    public async Task ImportsMetadataAndExactPerRepositorySelectionsOnce()
    {
        var w = new TaskWorld();
        w.Jira.Result = new("PROJ-12", "Jira summary", "", "url", []);
        w.Git.AddRepository("Api", remote: ["feature/PROJ-123", "Fix/proj-12", "Fix/proj-12", "unrelated"]);
        w.Git.AddRepository("Web", remote: ["work/PROJ-12-ui"]);
        w.Selection.Choose = (_, options) => options.Single(b => b == "Fix/proj-12");
        var task = await w.Resolver.GetAsync("PROJ-12");
        Assert.Equal([new RepositoryBranch("Api", "Fix/proj-12"), new("Web", "work/PROJ-12-ui")], task.Repositories);
        Assert.Equal(["Fix/proj-12", "feature/PROJ-123"], Assert.Single(w.Selection.Calls).Candidates);
        Assert.Equal(1, w.Repository.Writes);
        Assert.Equal(1, w.Jira.Calls);
        w.Git.RemoteFailures.Add("Api");
        Assert.Equal(task.Repositories, (await w.Resolver.GetAsync("proj-12")).Repositories);
        Assert.Equal(1, w.Jira.Calls);
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation is "checkout" or "track" or "create" or "pull");
    }

    [Fact]
    public async Task EmptyDiscoveryIsSavedAndDoesNotRepeatImplicitly()
    {
        var w = new TaskWorld(); w.Jira.Result = new("P-1", "s", "d", "u", []); w.Git.AddRepository("Api", hasOrigin: false);
        Assert.Empty((await w.Resolver.GetAsync("P-1")).Repositories);
        Assert.Empty((await w.Resolver.GetAsync("P-1")).Repositories);
        Assert.Equal(1, w.Jira.Calls);
        Assert.Equal(1, w.Repository.Writes);
    }

    [Fact]
    public async Task SelectionCancelledOnSecondRepositoryDoesNotSaveFirstSelectionOrMetadata()
    {
        var w = new TaskWorld(); w.Jira.Result = new("P-1", "s", "d", "u", []);
        w.Git.AddRepository("Api", remote: ["a/P-1", "b/P-1"]);
        w.Git.AddRepository("Web", remote: ["a/P-1", "b/P-1"]);
        w.Selection.Choose = (repo, names) => repo == "Web" ? throw new OperationCanceledException() : names[0];
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => w.Resolver.GetAsync("P-1"));
        Assert.Equal(2, w.Selection.Calls.Count);
        Assert.Null(await w.Repository.GetAsync("P-1"));
        Assert.Equal(0, w.Repository.Writes);
    }

    [Fact]
    public async Task ResetRemovesAbsentMappingsButRemoteFailurePreservesSnapshot()
    {
        var w = new TaskWorld(); var original = w.SeedTwo();
        w.Git.Remote["Api"] = ["new/PROJ-123"];
        w.Git.RemoteFailures.Add("Web");
        await Assert.ThrowsAsync<IOException>(() => w.Resolver.ResetAsync("PROJ-123"));
        Assert.Equal(original.Repositories, (await w.Repository.GetAsync(original.Id))!.Repositories);
        Assert.Equal(0, w.Repository.Writes);
        w.Git.RemoteFailures.Clear(); w.Git.Remote["Web"] = [];
        var reset = await w.Resolver.ResetAsync("PROJ-123");
        Assert.Equal(new RepositoryBranch("Api", "new/PROJ-123"), Assert.Single(reset.Repositories));
        Assert.Equal(original.Summary, reset.Summary);
        Assert.Equal(0, w.Jira.Calls);
    }

    [Fact]
    public async Task ResetCanRemoveEveryMappingIncludingMissingRepositories()
    {
        var w = new TaskWorld(); w.Seed("P-1", [new("Missing", "old")]);
        Assert.Empty((await w.Resolver.ResetAsync("P-1")).Repositories);
        Assert.Empty((await w.Repository.GetAsync("P-1"))!.Repositories);
    }

    [Fact]
    public async Task UnavailableJiraOrCancelledTokenDoesNotCreateTask()
    {
        var w = new TaskWorld();
        await Assert.ThrowsAsync<InvalidOperationException>(() => w.Resolver.GetAsync("P-1"));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => w.Resolver.GetAsync("P-1", cancellation.Token));
        Assert.Equal(0, w.Repository.Writes);
    }

    [Fact]
    public async Task UnknownTaskLikeReferenceImportsButLiteralDoesNot()
    {
        var w = new TaskWorld(); w.Jira.Result = new("P-1", "s", "d", "u", []);
        Assert.Equal("Feature/Custom", (await w.Resolver.ResolveReferenceAsync("Feature/Custom")).Literal);
        Assert.Equal("P-1", (await w.Resolver.ResolveReferenceAsync("P-1", true)).Literal);
        Assert.Equal(0, w.Jira.Calls);
        Assert.NotNull((await w.Resolver.ResolveReferenceAsync("P-1")).Task);
        Assert.Equal(1, w.Jira.Calls);
    }

    [Fact]
    public void RemoteParserPreservesCaseAndRejectsMalformedResponses()
    {
        var result = GitTool.ParseRemoteHeads("abc123\trefs/heads/Feature/P-1\ndef456 refs/heads/feature/P-1\nabc123\trefs/heads/Feature/P-1\n");
        Assert.Equal(["Feature/P-1", "feature/P-1"], result);
        Assert.Empty(GitTool.ParseRemoteHeads("\n"));
        Assert.Throws<InvalidDataException>(() => GitTool.ParseRemoteHeads("abc refs/tags/v1"));
        Assert.Throws<InvalidDataException>(() => GitTool.ParseRemoteHeads("not-a-hash refs/heads/x"));
    }
}
