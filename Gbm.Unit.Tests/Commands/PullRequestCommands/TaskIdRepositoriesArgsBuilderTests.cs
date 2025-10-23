using Gbm.Commands.PullRequestCommands;
using Gbm.Persistence.Entities;
using Gbm.Unit.Tests.Shared.Fakes;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Unit.Tests.Commands.PullRequestCommands;

public class TaskIdRepositoriesArgsBuilderTests
{
    [Fact]
    public async Task BuildAsync_Throws_OnNullOrEmptyArgs()
    {
        var repo = new FakeTaskInfoRepository();
        var git = new Gbm.Unit.Tests.Shared.Fakes.FakeGitTool();
        var sut = new TaskIdRepositoriesArgsBuilder(repo, git);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.BuildAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => sut.BuildAsync([]));
    }

    [Fact]
    public async Task BuildAsync_Throws_OnMissingTaskId()
    {
        var repo = new FakeTaskInfoRepository();
        var git = new Gbm.Unit.Tests.Shared.Fakes.FakeGitTool();
        var sut = new TaskIdRepositoriesArgsBuilder(repo, git);

        var ex = await Assert.ThrowsAsync<ArgsValidationException>(() => sut.BuildAsync(["-pr-open"]));
        Assert.Contains("Missing TaskId", ex.Message);
    }

    [Fact]
    public async Task BuildAsync_UsesProvidedRepos()
    {
        var repo = new FakeTaskInfoRepository();
        repo.Seed(new TaskInfo("GBM-3", "s", "d", "u", "feature/GBM-3"));
        var git = new Gbm.Unit.Tests.Shared.Fakes.FakeGitTool();
        var sut = new TaskIdRepositoriesArgsBuilder(repo, git);

        var dict = await sut.BuildAsync(["-pr-open", "GBM-3", "RepoX", "RepoY"]);
        Assert.Equal("GBM-3", dict["TaskId"]);
        Assert.Equal(["RepoX", "RepoY"], dict["Repositories"] as IEnumerable<string>);
    }

    [Fact]
    public async Task BuildAsync_FetchesReposFromGit_WhenNotProvided()
    {
        var repo = new FakeTaskInfoRepository();
        repo.Seed(new TaskInfo("GBM-4", "s", "d", "u", "feature/GBM-4"));
        var git = new Gbm.Unit.Tests.Shared.Fakes.FakeGitTool();
        git.Seed("feature/GBM-4", "RepoA", "RepoB");
        var sut = new TaskIdRepositoriesArgsBuilder(repo, git);

        var dict = await sut.BuildAsync(["-pr-open", "GBM-4"]);
        var repos = Assert.IsAssignableFrom<IEnumerable<string>>(dict["Repositories"]);
        Assert.Contains("RepoA", repos);
        Assert.Contains("RepoB", repos);
    }
}
