using Gbm.Commands.BranchesCommands;
using Gbm.Services.Tasks;
using Gbm.Unit.Tests.Shared.Fakes;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Unit.Tests.Commands.BranchesCommands;

public class BranchesCommandArgsBuilderTests
{
    private static BranchesCommandArgsBuilder Builder(TaskWorld world) => new(world.Resolver, world.Targets, world.Catalog);

    [Fact]
    public async Task RejectsNullEmptyMissingAndUnknownCommands()
    {
        var builder = Builder(new());
        await Assert.ThrowsAsync<ArgumentException>(() => builder.BuildAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => builder.BuildAsync([]));
        await Assert.ThrowsAsync<ArgsValidationException>(() => builder.BuildAsync(["-n"]));
        await Assert.ThrowsAsync<ArgsValidationException>(() => builder.BuildAsync(["-n", "P-1"]));
        await Assert.ThrowsAsync<ArgsValidationException>(() => builder.BuildAsync(["-other", "P-1"]));
    }

    [Theory]
    [InlineData("-l")]
    [InlineData("-s")]
    [InlineData("-u")]
    [InlineData("-build")]
    [InlineData("-push")]
    [InlineData("-pull")]
    [InlineData("-d")]
    [InlineData("-r")]
    public async Task UsesConcreteBranchForEachRepository(string command)
    {
        var world = new TaskWorld(); world.SeedTwo();
        var result = await Builder(world).BuildAsync([command, "proj-123"]);
        Assert.Equal([new RepositoryBranchTarget("Api", "feature/PROJ-123-api"), new("Web", "fix/PROJ-123-ui")], (RepositoryBranchTarget[])result["Targets"]);
        Assert.Equal(0, world.Jira.Calls);
    }

    [Fact]
    public async Task ExplicitReposAreCanonicalDeduplicatedAndValidatedBeforeImport()
    {
        var world = new TaskWorld(); world.SeedTwo();
        var result = await Builder(world).BuildAsync(["-push", "PROJ-123", "nobuild", "web", "WEB"]);
        Assert.True((bool)result["NoBuild"]);
        Assert.Equal(new RepositoryBranchTarget("Web", "fix/PROJ-123-ui"), Assert.Single((RepositoryBranchTarget[])result["Targets"]));
        await Assert.ThrowsAsync<ArgsValidationException>(() => Builder(world).BuildAsync(["-u", "UNKNOWN-1", "../Api"]));
        Assert.Equal(0, world.Jira.Calls);
        world.Git.AddRepository("Other");
        await Assert.ThrowsAsync<ArgsValidationException>(() => Builder(world).BuildAsync(["-u", "PROJ-123", "Other"]));
    }

    [Fact]
    public async Task RemoteOnlyIsEligibleForCheckoutButNotBuild()
    {
        var world = new TaskWorld(); world.Git.AddRepository("Api", remote: ["Fix/P-1"]); world.Seed("P-1", [new("Api", "Fix/P-1")]);
        Assert.Single((RepositoryBranchTarget[])(await Builder(world).BuildAsync(["-s", "P-1"]))["Targets"]);
        await Assert.ThrowsAsync<ArgsValidationException>(() => Builder(world).BuildAsync(["-build", "P-1"]));
    }

    [Fact]
    public async Task LiteralEscapePreservesCaseAndOverridesSavedTask()
    {
        var world = new TaskWorld(); world.SeedTwo();
        world.Git.Local["Api"].Add("PROJ-123");
        var result = await Builder(world).BuildAsync(["-s", "--branch", "PROJ-123", "Api"]);
        Assert.Equal("PROJ-123", Assert.Single((RepositoryBranchTarget[])result["Targets"]).BranchName);
        Assert.Equal(0, world.Jira.Calls);
        Assert.Equal(0, world.Repository.Writes);
    }

    [Fact]
    public async Task MainAliasIsPerRepoButForcedMainIsExact()
    {
        var world = new TaskWorld(); world.Git.AddRepository("Api", ["master"]); world.Git.AddRepository("Web", ["main"]);
        var result = (RepositoryBranchTarget[])(await Builder(world).BuildAsync(["-s", "main"]))["Targets"];
        Assert.Equal(["master", "main"], result.Select(t => t.BranchName));
        var exact = (RepositoryBranchTarget[])(await Builder(world).BuildAsync(["-s", "--branch", "main"]))["Targets"];
        Assert.Equal("Web", Assert.Single(exact).Repository);
    }

    [Fact]
    public async Task OriginTaskIsJoinedByRepositoryAndMustCoverEveryTarget()
    {
        var world = new TaskWorld(); world.SeedTwo();
        world.Seed("P-100", new("Web", "origin/web"), new("Api", "origin/api"));
        world.Git.Remote["Api"] = ["origin/api"]; world.Git.Remote["Web"] = ["origin/web"];
        var resolved = (RepositoryBranchTarget[])(await Builder(world).BuildAsync(["-u", "PROJ-123", "--origin", "P-100"]))["Targets"];
        Assert.Equal(["origin/api", "origin/web"], resolved.Select(t => t.OriginBranchName));
        world.Seed("P-100", [new("Api", "origin/api")]);
        await Assert.ThrowsAsync<ArgsValidationException>(() => Builder(world).BuildAsync(["-u", "PROJ-123", "--origin", "P-100"]));
        Assert.DoesNotContain(world.Git.Calls, c => c.Operation is "checkout" or "merge");
    }

    [Theory]
    [InlineData("--origin")]
    [InlineData("--origin-branch")]
    public async Task RejectsMissingOrigin(string option)
        => await Assert.ThrowsAsync<ArgsValidationException>(() => Builder(new()).BuildAsync(["-u", "P-1", option]));

    [Fact]
    public async Task SdkOnlyDevelopIsAnExplicitNoOp()
    {
        var world = new TaskWorld(); world.Git.AddRepository("ClientSDK"); world.Seed("P-1", [new("ClientSDK", "branch")]);
        var result = await Builder(world).BuildAsync(["-d", "P-1", "clientsdk"]);
        Assert.Empty((RepositoryBranchTarget[])result["Targets"]);
    }
}
