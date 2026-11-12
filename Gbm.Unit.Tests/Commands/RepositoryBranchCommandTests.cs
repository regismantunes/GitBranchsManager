using Gbm.Commands.BranchesCommands;
using Gbm.Commands.PullRequestCommands;
using Gbm.Commands.TaskInfoCommands;
using Gbm.Services.Tasks;
using Gbm.Unit.Tests.Shared.Fakes;

namespace Gbm.Unit.Tests.Commands;

public class RepositoryBranchCommandTests
{
    private static RepositoryBranchTarget[] TwoTargets => [new("Api", "feature/PROJ-123-api"), new("Web", "fix/PROJ-123-ui")];

    [Fact]
    public async Task PushBuildsEveryTargetBeforePublishingCorrectBranches()
    {
        var w = new TaskWorld(); w.SeedTwo();
        w.Build.OnBuild = () => Assert.DoesNotContain(w.Git.Calls, c => c.Operation == "push");
        Assert.Equal(0, await new PushTaskBranchesCommand(w.Git, w.Build).ExecuteAsync(TwoTargets));
        Assert.Equal([("Api", "feature/PROJ-123-api"), ("Web", "fix/PROJ-123-ui")], w.Build.Calls);
        Assert.Equal([new GitCall("push", "Api", "feature/PROJ-123-api"), new("push", "Web", "fix/PROJ-123-ui")], w.Git.Calls.Where(c => c.Operation == "push"));
    }

    [Fact]
    public async Task FailedBuildStopsAllPushes()
    {
        var w = new TaskWorld(); w.SeedTwo(); w.Build.FailRepository = "Web";
        Assert.Equal(1, await new PushTaskBranchesCommand(w.Git, w.Build).ExecuteAsync(TwoTargets));
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation is "checkout" or "push");
        Assert.Equal(2, w.Build.Calls.Count);
    }

    [Fact]
    public async Task NoBuildSkipsBuildAndCheckoutFailureStopsPush()
    {
        var w = new TaskWorld(); w.SeedTwo(); w.Git.RejectCheckoutIn = "Api";
        await Assert.ThrowsAsync<InvalidOperationException>(() => new PushTaskBranchesCommand(w.Git, w.Build).ExecuteAsync(TwoTargets, true));
        Assert.Empty(w.Build.Calls);
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation == "push");
    }

    [Fact]
    public async Task PullStopsOnCheckoutFailure()
    {
        var w = new TaskWorld(); w.SeedTwo(); w.Git.RejectCheckoutIn = "Api";
        await Assert.ThrowsAsync<InvalidOperationException>(() => new PullTaskBranchesCommand(w.Git).ExecuteAsync(TwoTargets));
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation == "pull");
    }

    [Fact]
    public async Task UpdateUsesPerRepositoryOrigins()
    {
        var w = new TaskWorld(); w.SeedTwo();
        var targets = new[] { TwoTargets[0] with { OriginBranchName = "release/api" }, TwoTargets[1] with { OriginBranchName = "release/web" } };
        Assert.Equal(0, await new UpdateTaskBranchesCommand(w.Git).ExecuteAsync(targets));
        Assert.Equal([new GitCall("merge", "Api", "release/api"), new("merge", "Web", "release/web")], w.Git.Calls.Where(c => c.Operation == "merge"));
    }

    [Fact]
    public async Task DevelopBuildsAfterIntegrationAndSkipsSdk()
    {
        var w = new TaskWorld(); w.SeedTwo(); w.Git.AddRepository("ClientSDK");
        w.Build.OnBuild = () =>
        {
            Assert.Equal("develop", w.Git.Current[w.Git.WorkingDirectory!]);
            Assert.Equal("merge", w.Git.Calls.Last().Operation);
        };
        Assert.Equal(0, await new SendTaskBranchesToDevelopCommand(w.Git, w.Build).ExecuteAsync([.. TwoTargets, new("ClientSDK", "ignored")]));
        Assert.DoesNotContain(w.Git.Calls, c => c.Repository == "ClientSDK");
        Assert.All(w.Git.Calls.Where(c => c.Operation == "push"), c => Assert.Equal("develop", c.Branch));
        Assert.Equal([("Api", (string?)null), ("Web", (string?)null)], w.Build.Calls);
    }

    [Fact]
    public async Task RemoveDeletesExactLocalBranchesAndRetainsMappings()
    {
        var w = new TaskWorld(); var task = w.SeedTwo();
        Assert.Equal(0, await new RemoveTaskBranchesCommand(w.Git).ExecuteAsync(TwoTargets));
        Assert.DoesNotContain("feature/PROJ-123-api", w.Git.Local["Api"]);
        Assert.DoesNotContain("fix/PROJ-123-ui", w.Git.Local["Web"]);
        Assert.Equal(task.Repositories, (await w.Repository.GetAsync(task.Id))!.Repositories);
        Assert.Equal(0, w.Repository.Writes);
    }

    [Fact]
    public async Task CheckoutTracksRemoteThenPullsThatBranch()
    {
        var w = new TaskWorld(); w.Git.AddRepository("Api", remote: ["Feature/P-1"]);
        Assert.Equal(0, await new SetTaskBranchesCommand(w.Git).ExecuteAsync([new("Api", "Feature/P-1")]));
        Assert.Equal(["set", "checkout", "remote", "track", "pull"], w.Git.Calls.Select(c => c.Operation));
        Assert.Equal("Feature/P-1", w.Git.Calls.Last().Branch);
    }

    [Fact]
    public async Task NoRepoEditsMetadataWithoutCreatingOrResolvingBranches()
    {
        var w = new TaskWorld(); var task = w.SeedTwo();
        var command = new SaveTaskInfoCommand(w.Repository, w.Configuration, w.Jira, w.Catalog, w.Creator, w.Names);
        Assert.Equal(0, await command.ExecuteAsync(task.Id, "New summary", "New description", null, false, true, []));
        var saved = (await w.Repository.GetAsync(task.Id))!;
        Assert.Equal(task.Repositories, saved.Repositories);
        Assert.Equal("New summary", saved.Summary);
        Assert.Empty(w.Git.Calls);
        Assert.Equal(0, w.Jira.Calls);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PrUsesMappedHeadsAndIndependentFlags(bool noPush, bool noBuild)
    {
        var w = new TaskWorld(); var task = w.SeedTwo();
        w.Build.OnBuild = () => { Assert.Empty(w.GitHub.Created); Assert.DoesNotContain(w.Git.Calls, c => c.Operation == "push"); };
        var command = new OpenPullRequestsTaskCommand(w.PullRequests, w.GitHub, w.Git, w.Build);
        Assert.Equal(0, await command.ExecuteAsync(task, TwoTargets, !noPush, noBuild));
        Assert.Equal([("Api", "feature/PROJ-123-api", "main"), ("Web", "fix/PROJ-123-ui", "main")], w.GitHub.Created);
        Assert.Equal(noBuild ? 0 : 2, w.Build.Calls.Count);
        Assert.Equal(noPush ? 0 : 2, w.Git.Calls.Count(c => c.Operation == "push"));
        Assert.Equal(2, w.PullRequests.Items.Count);
        Assert.Equal(2, w.GitHub.Updated.Count);
        Assert.All(w.GitHub.Updated, update => { Assert.Contains("[Api]", update.Links); Assert.Contains("[Web]", update.Links); });
    }

    [Fact]
    public async Task FailedPrBuildDoesNotPublishAnything()
    {
        var w = new TaskWorld(); var task = w.SeedTwo(); w.Build.FailRepository = "Web";
        var command = new OpenPullRequestsTaskCommand(w.PullRequests, w.GitHub, w.Git, w.Build);
        Assert.Equal(1, await command.ExecuteAsync(task, TwoTargets));
        Assert.Empty(w.GitHub.Created);
        Assert.Empty(w.PullRequests.Items);
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation == "push");
    }

    [Fact]
    public async Task ExistingPrGetsLinksWithoutBuildOrCheckoutAndFailedNewPrKeepsCompletedOnes()
    {
        var w = new TaskWorld(); var task = w.SeedTwo();
        await w.PullRequests.SaveAsync(task.Id, "api", 10, "existing-url");
        var command = new OpenPullRequestsTaskCommand(w.PullRequests, w.GitHub, w.Git, w.Build);
        Assert.Equal(0, await command.ExecuteAsync(task, TwoTargets));
        Assert.Equal("Web", Assert.Single(w.GitHub.Created).Repository);
        Assert.Equal("Web", Assert.Single(w.Build.Calls).Repository);
        Assert.DoesNotContain(w.Git.Calls, c => c.Repository == "Api" && c.Operation == "checkout");
        Assert.Equal(2, w.GitHub.Updated.Count);

        var partial = new TaskWorld(); var partialTask = partial.SeedTwo(); partial.GitHub.FailRepository = "Web";
        await Assert.ThrowsAsync<IOException>(() => new OpenPullRequestsTaskCommand(partial.PullRequests, partial.GitHub, partial.Git, partial.Build)
            .ExecuteAsync(partialTask, TwoTargets, noBuild: true));
        Assert.Equal("Api", Assert.Single(partial.PullRequests.Items).Repository);
    }

    [Fact]
    public async Task PrCheckoutFailureNeverPushesOrCreatesPr()
    {
        var w = new TaskWorld(); var task = w.SeedTwo(); w.Git.RejectCheckoutIn = "Api";
        await Assert.ThrowsAsync<InvalidOperationException>(() => new OpenPullRequestsTaskCommand(w.PullRequests, w.GitHub, w.Git, w.Build)
            .ExecuteAsync(task, TwoTargets, noBuild: true));
        Assert.Empty(w.GitHub.Created);
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation == "push");
    }
}
