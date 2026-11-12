using System.Diagnostics;
using Gbm.Commands.BranchesCommands;
using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories;
using Gbm.Services.Git;
using Gbm.Services.Tasks;
using Gbm.Unit.Tests.Shared;
using Gbm.Unit.Tests.Shared.Fakes;

namespace Gbm.Unit.Tests.Services.Git;

public class GitDiscoveryIntegrationTests
{
    private static async Task<string> GitAsync(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = await stdout;
        Assert.True(process.ExitCode == 0, await stderr);
        return output.TrimEnd();
    }

    private static async Task<(string Work, string Remote)> CreateRepositoryAsync(TemporaryDirectory root, string repo, string branch)
    {
        var basePath = root.File("repos"); Directory.CreateDirectory(basePath);
        var origins = root.File("origins"); Directory.CreateDirectory(origins);
        var remote = Path.Combine(origins, repo + ".git");
        await GitAsync(root.Path, "init", "--bare", "--initial-branch=main", remote);
        var work = Path.Combine(basePath, repo);
        await GitAsync(root.Path, "init", "--initial-branch=main", work);
        await GitAsync(work, "config", "user.name", "GBM Tests");
        await GitAsync(work, "config", "user.email", "gbm-tests@example.invalid");
        await GitAsync(work, "config", "commit.gpgsign", "false");
        await GitAsync(work, "config", "core.hooksPath", root.File("no-hooks"));
        await File.WriteAllTextAsync(Path.Combine(work, "file.txt"), "original\n", TestContext.Current.CancellationToken);
        await GitAsync(work, "add", "file.txt");
        await GitAsync(work, "commit", "-m", "initial");
        await GitAsync(work, "remote", "add", "origin", remote);
        await GitAsync(work, "push", "--set-upstream", "origin", "main");
        var head = await GitAsync(work, "rev-parse", "HEAD");
        await GitAsync(remote, "update-ref", $"refs/heads/{branch}", head);
        return (work, remote);
    }

    private static async Task<string[]> StateAsync(string work)
        => [await GitAsync(work, "symbolic-ref", "HEAD"), await GitAsync(work, "rev-parse", "HEAD"),
            await GitAsync(work, "for-each-ref", "--format=%(refname) %(objectname)"), await GitAsync(work, "status", "--porcelain=v1")];

    [Fact]
    public async Task ResetReadsLiveHeadsWithoutChangingGitAndRemoteFailureKeepsFileIntact()
    {
        using var root = new TemporaryDirectory();
        var api = await CreateRepositoryAsync(root, "Api", "Feature/PROJ-123-api");
        var web = await CreateRepositoryAsync(root, "Web", "fix/PROJ-123-ui");
        await File.AppendAllTextAsync(Path.Combine(api.Work, "file.txt"), "uncommitted\n", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(web.Work, "untracked.txt"), "keep me", TestContext.Current.CancellationToken);
        var beforeApi = await StateAsync(api.Work); var beforeWeb = await StateAsync(web.Work);
        var git = new GitTool(root.File("repos")) { ShowGitOutput = false };
        await git.SetRepositoryAsync("Api", TestContext.Current.CancellationToken);
        var file = root.File("tasksinfo.json");
        var repository = new TaskInfoRepository("jira", file);
        await repository.SaveSnapshotAsync(new("PROJ-123", "s", "d", "url", [new("RemovedRepo", "old")]), TestContext.Current.CancellationToken);
        var catalog = new RepositoryCatalog(git);
        var resolver = new TaskResolver(repository, new TestJira(), new TaskBranchDiscovery(git, catalog, new TestSelection()));
        var reset = await resolver.ResetAsync("PROJ-123", TestContext.Current.CancellationToken);
        Assert.Equal([new RepositoryBranch("Api", "Feature/PROJ-123-api"), new("Web", "fix/PROJ-123-ui")], reset.Repositories);
        Assert.Equal(beforeApi, await StateAsync(api.Work)); Assert.Equal(beforeWeb, await StateAsync(web.Work));
        Assert.Equal(api.Work, git.WorkingDirectory);

        await GitAsync(web.Remote, "update-ref", "-d", "refs/heads/fix/PROJ-123-ui");
        var updated = await resolver.ResetAsync("PROJ-123", TestContext.Current.CancellationToken);
        Assert.Equal("Api", Assert.Single(updated.Repositories).Repository);
        var originalBytes = await File.ReadAllBytesAsync(file, TestContext.Current.CancellationToken);
        await GitAsync(web.Work, "remote", "set-url", "origin", root.File("does-not-exist.git"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResetAsync("PROJ-123", TestContext.Current.CancellationToken));
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(file, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CheckoutMaterializesRemoteBranchAndPullsItsTrackingBranch()
    {
        using var root = new TemporaryDirectory();
        var repo = await CreateRepositoryAsync(root, "Api", "Feature/P-1");
        var git = new GitTool(root.File("repos")) { ShowGitOutput = false };
        await git.SetRepositoryAsync("Api", TestContext.Current.CancellationToken);
        Assert.False(await git.BranchExistsAsync("Feature/P-1", TestContext.Current.CancellationToken));
        var command = new SetTaskBranchesCommand(git);
        Assert.Equal(0, await command.ExecuteAsync([new("Api", "Feature/P-1")], TestContext.Current.CancellationToken));
        Assert.Equal("Feature/P-1", await git.GetCurrentBranchAsync(TestContext.Current.CancellationToken));
        Assert.Equal("origin/Feature/P-1", await GitAsync(repo.Work, "rev-parse", "--abbrev-ref", "@{upstream}"));
        Assert.False(await git.BranchExistsAsync("feature/p-1", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NoOriginIsDifferentFromFailedOriginAndCreateRejectsInvalidName()
    {
        using var root = new TemporaryDirectory();
        var work = root.File("Api"); await GitAsync(root.Path, "init", "--initial-branch=main", work);
        var git = new GitTool(root.Path) { ShowGitOutput = false };
        var noOrigin = await git.GetRemoteBranchNamesAsync("Api", TestContext.Current.CancellationToken);
        Assert.False(noOrigin.HasOrigin); Assert.Empty(noOrigin.BranchNames);
        await git.SetRepositoryAsync("Api", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => git.ValidateBranchNameAsync("bad name", TestContext.Current.CancellationToken));
        await GitAsync(work, "remote", "add", "origin", root.File("missing.git"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => git.GetRemoteBranchNamesAsync("Api", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => git.SetRepositoryAsync("../Api", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreationConfirmsRealGitBranchAndRepeatKeepsSameCommit()
    {
        using var root = new TemporaryDirectory();
        var repo = await CreateRepositoryAsync(root, "Api", "unrelated");
        var git = new GitTool(root.File("repos")) { ShowGitOutput = false };
        var repository = new TaskInfoRepository("jira", root.File("tasksinfo.json"));
        await repository.SaveMetadataAsync("P-1", "s", "d", TestContext.Current.CancellationToken);
        var creator = new TaskBranchCreator(git, repository, new RepositoryCatalog(git), new TaskWorld().Names);
        var task = (await repository.GetAsync("P-1", TestContext.Current.CancellationToken))!;
        await creator.CreateAsync(task, ["Api"], cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("feature/P-1", await git.GetCurrentBranchAsync(TestContext.Current.CancellationToken));
        var saved = (await repository.GetAsync("P-1", TestContext.Current.CancellationToken))!;
        Assert.Equal(new RepositoryBranch("Api", "feature/P-1"), Assert.Single(saved.Repositories));
        var commit = await GitAsync(repo.Work, "rev-parse", "HEAD");
        await creator.CreateAsync(saved, ["Api"], cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(commit, await GitAsync(repo.Work, "rev-parse", "HEAD"));
        Assert.Single((await repository.GetAsync("P-1", TestContext.Current.CancellationToken))!.Repositories);
    }

    [Fact]
    public async Task BrokenRepositoryMetadataAbortsInventoryInsteadOfSilentlyDroppingMappings()
    {
        using var root = new TemporaryDirectory();
        var invalid = root.File("Broken"); Directory.CreateDirectory(invalid);
        await File.WriteAllTextAsync(Path.Combine(invalid, ".git"), "gitdir: missing-directory", TestContext.Current.CancellationToken);
        var git = new GitTool(root.Path) { ShowGitOutput = false };
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in git.GetAllRepositoriesAsync(TestContext.Current.CancellationToken)) { }
        });
    }
}
