using Gbm.Commands.BranchsCommands;
using Gbm.Persistence.Entities;
using Gbm.Unit.Tests.Shared.Fakes;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Unit.Tests.Commands.BranchsCommands;

public class BranchsCommandArgsBuilderTests
{
    [Fact]
    public async Task BuildAsync_Throws_OnNullOrEmptyArgs()
    {
        var repo = new FakeTaskInfoRepository();
        var git = new FakeGitTool();
        var sut = new BranchsCommandArgsBuilder(repo, git);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.BuildAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => sut.BuildAsync([]));
    }

    [Fact]
    public async Task BuildAsync_Throws_OnMissingTaskId()
    {
        var repo = new FakeTaskInfoRepository();
        var git = new FakeGitTool();
        var sut = new BranchsCommandArgsBuilder(repo, git);

        var ex = await Assert.ThrowsAsync<ArgsValidationException>(() => sut.BuildAsync(["-n"]));
        Assert.Contains("Missing TaskId", ex.Message);
    }

    [Fact]
    public async Task BuildAsync_Throws_WhenTaskNotFound()
    {
        var repo = new FakeTaskInfoRepository();
        var git = new FakeGitTool();
        var sut = new BranchsCommandArgsBuilder(repo, git);

        var ex = await Assert.ThrowsAsync<ArgsValidationException>(() => sut.BuildAsync(["-u", "GBM-1"]));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task BuildAsync_Throws_WhenNoReposFoundAutomatically()
    {
        var repo = new FakeTaskInfoRepository();
        var git = new FakeGitTool();
        repo.Seed(new TaskInfo("GBM-1", "s", "d", "u", "feature/GBM-1"));
        var sut = new BranchsCommandArgsBuilder(repo, git);

        var ex = await Assert.ThrowsAsync<ArgsValidationException>(() => sut.BuildAsync(["-u", "GBM-1"]));
        Assert.Contains("No repositories found", ex.Message);
    }

    [Fact]
    public async Task BuildAsync_ReturnsBranchAndRepos_WhenProvided()
    {
        var repo = new FakeTaskInfoRepository();
        var git = new FakeGitTool();
        repo.Seed(new TaskInfo("GBM-1", "s", "d", "u", "feature/GBM-1"));
        var sut = new BranchsCommandArgsBuilder(repo, git);

        var dict = await sut.BuildAsync(["-u", "GBM-1", "Repo1", "Repo2"]);
        Assert.Equal("feature/GBM-1", dict["TaskBranch"]);
        Assert.Equal(["Repo1", "Repo2"], dict["Repositories"] as IEnumerable<string>);
    }

    [Fact]
    public async Task BuildAsync_FetchesReposFromGit_WhenNotProvided()
    {
        var repo = new FakeTaskInfoRepository();
        var git = new FakeGitTool();
        repo.Seed(new TaskInfo("GBM-2", "s", "d", "u", "feature/GBM-2"));
        git.Seed("feature/GBM-2", "RepoA");
        var sut = new BranchsCommandArgsBuilder(repo, git);

        var dict = await sut.BuildAsync(["-u", "GBM-2"]);
        var repos = Assert.IsAssignableFrom<IEnumerable<string>>(dict["Repositories"]);
        Assert.Contains("RepoA", repos);
    }
}
