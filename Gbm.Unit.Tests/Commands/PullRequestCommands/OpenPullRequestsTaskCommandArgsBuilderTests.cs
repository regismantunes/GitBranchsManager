using Gbm.Commands.PullRequestCommands;
using Gbm.Services.Tasks;
using Gbm.Unit.Tests.Shared.Fakes;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Unit.Tests.Commands.PullRequestCommands;

public class OpenPullRequestsTaskCommandArgsBuilderTests
{
    private static OpenPullRequestsTaskCommandArgsBuilder Builder(TaskWorld w) => new(w.Resolver, w.Targets, w.Catalog, w.PullRequests);

    [Fact]
    public async Task RejectsMissingAndInvalidArguments()
    {
        var builder = Builder(new());
        await Assert.ThrowsAsync<ArgumentException>(() => builder.BuildAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => builder.BuildAsync([]));
        await Assert.ThrowsAsync<ArgsValidationException>(() => builder.BuildAsync(["-pr"]));
        await Assert.ThrowsAsync<ArgsValidationException>(() => builder.BuildAsync(["-pr", "P-1", "nobuild", "nopush"]));
    }

    [Fact]
    public async Task CarriesOneTaskSnapshotConcreteTargetsAndFlags()
    {
        var world = new TaskWorld(); world.SeedTwo();
        var result = await Builder(world).BuildAsync(["-pr", "PROJ-123", "nopush", "nobuild", "web"]);
        Assert.False((bool)result["PushLocalChanges"]);
        Assert.True((bool)result["NoBuild"]);
        Assert.Equal("fix/PROJ-123-ui", Assert.Single((RepositoryBranchTarget[])result["Targets"]).BranchName);
        Assert.Equal(0, world.Jira.Calls);
    }

    [Fact]
    public async Task ExistingPrDoesNotRequireLocalBranchForLinkUpdates()
    {
        var world = new TaskWorld(); world.Git.AddRepository("Api"); world.Seed("P-1", [new("Api", "removed-local")]);
        await world.PullRequests.SaveAsync("P-1", "api", 1, "url");
        var result = await Builder(world).BuildAsync(["-pr", "P-1"]);
        Assert.Equal("removed-local", Assert.Single((RepositoryBranchTarget[])result["Targets"]).BranchName);
    }
}
