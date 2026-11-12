using Gbm.Persistence.Entities;
using Gbm.Unit.Tests.Shared.Fakes;

namespace Gbm.Unit.Tests.Services.Tasks;

public class TaskBranchCreatorTests
{
    [Fact]
    public async Task ReusesExistingMappingAndGeneratesOnlyForNewRepositories()
    {
        var w = new TaskWorld("changed/{TaskId}"); var task = w.Seed("P-1", [new("Api", "existing")]);
        w.Git.AddRepository("Api", ["main", "existing"]); w.Git.AddRepository("Web");
        await w.Creator.CreateAsync(task, ["api", "Web"]);
        Assert.Contains(new GitCall("checkout", "Api", "existing"), w.Git.Calls);
        Assert.Contains(new GitCall("create", "Web", "changed/P-1"), w.Git.Calls);
        Assert.DoesNotContain(w.Git.Calls, c => c.Repository == "Api" && c.Operation == "create");
        Assert.Equal([new RepositoryBranch("Api", "existing"), new("Web", "changed/P-1")], (await w.Repository.GetAsync("P-1"))!.Repositories);
    }

    [Fact]
    public async Task RemoteOnlyBranchIsTrackedWithoutPullingMainOrCreatingDivergentBranch()
    {
        var w = new TaskWorld(); var task = w.Seed("P-1"); w.Git.AddRepository("Api", remote: ["feature/P-1"]);
        await w.Creator.CreateAsync(task, ["Api"]);
        Assert.Contains(new GitCall("track", "Api", "feature/P-1"), w.Git.Calls);
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation is "create" or "pull");
        Assert.Equal(new RepositoryBranch("Api", "feature/P-1"), Assert.Single((await w.Repository.GetAsync("P-1"))!.Repositories));
    }

    [Theory]
    [InlineData("pull")]
    [InlineData("create")]
    public async Task PartialGitFailurePreservesOnlyConfirmedMappings(string operation)
    {
        var w = new TaskWorld(); var task = w.Seed("P-1");
        w.Git.AddRepository("Api"); w.Git.AddRepository("Web"); w.Git.AddRepository("Other");
        w.Git.FailOperation = operation; w.Git.FailRepository = "Web";
        await Assert.ThrowsAsync<InvalidOperationException>(() => w.Creator.CreateAsync(task, ["Api", "Web", "Other"]));
        Assert.Equal("Api", Assert.Single((await w.Repository.GetAsync("P-1"))!.Repositories).Repository);
        Assert.DoesNotContain(w.Git.Calls, c => c.Repository == "Other" && c.Operation == "create");
    }

    [Fact]
    public async Task FailedPersistenceCanBeRetriedWithoutRecreatingBranch()
    {
        var w = new TaskWorld(); var task = w.Seed("P-1"); w.Git.AddRepository("Api"); w.Repository.FailUpsertRepository = "Api";
        await Assert.ThrowsAsync<IOException>(() => w.Creator.CreateAsync(task, ["Api"]));
        Assert.Empty((await w.Repository.GetAsync("P-1"))!.Repositories);
        Assert.Contains("feature/P-1", w.Git.Local["Api"]);
        w.Repository.FailUpsertRepository = null;
        await w.Creator.CreateAsync(task, ["Api"]);
        Assert.Single(w.Git.Calls.Where(c => c.Operation == "create"));
        Assert.Equal("feature/P-1", Assert.Single((await w.Repository.GetAsync("P-1"))!.Repositories).BranchName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("bad name/{TaskId}")]
    public async Task InvalidOrMissingFormatFailsBeforeAnyMutation(string? format)
    {
        var w = new TaskWorld(format); var task = w.Seed("P-1"); w.Git.AddRepository("Api");
        if (format is null) await Assert.ThrowsAsync<InvalidOperationException>(() => w.Creator.CreateAsync(task, ["Api"]));
        else await Assert.ThrowsAsync<ArgumentException>(() => w.Creator.CreateAsync(task, ["Api"]));
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation is "checkout" or "create" or "pull");
        Assert.Equal(0, w.Repository.Writes);
    }

    [Fact]
    public async Task ExplicitNameOnlyReplacesSelectedMappingAndDoesNotDeleteOldBranch()
    {
        var w = new TaskWorld(); var task = w.SeedTwo();
        await w.Creator.CreateAsync(task, ["Api"], "Explicit/New");
        var saved = (await w.Repository.GetAsync(task.Id))!;
        Assert.Contains(new RepositoryBranch("Api", "Explicit/New"), saved.Repositories);
        Assert.Contains(new RepositoryBranch("Web", "fix/PROJ-123-ui"), saved.Repositories);
        Assert.Contains("feature/PROJ-123-api", w.Git.Local["Api"]);
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation == "delete");
    }
}
