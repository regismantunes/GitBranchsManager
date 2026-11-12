using Gbm.Commands.TaskInfoCommands;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Build;
using Gbm.Services.Git;
using Gbm.Services.GitHub;
using Gbm.Services.Initialization;
using Gbm.Services.Jira;
using Gbm.Services.Middleware;
using Gbm.Services.Tasks;
using Gbm.Unit.Tests.Shared.Fakes;
using Microsoft.Extensions.DependencyInjection;
using RA.Console.DependencyInjection;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Unit.Tests.Commands;

[CollectionDefinition("Console", DisableParallelization = true)]
public sealed class ConsoleCollection;

[Collection("Console")]
public class ConsoleCommandIntegrationTests
{
    private static async Task<int> RunAsync(TaskWorld w, params string[] args)
    {
        var builder = new ConsoleAppBuilder(args);
        builder.Services.AddSingleton(w.Configuration)
            .AddSingleton<ITaskInfoRepository>(w.Repository)
            .AddSingleton<IGitTool>(w.Git)
            .AddSingleton<IJiraClient>(w.Jira)
            .AddSingleton<IGitHubClient>(w.GitHub)
            .AddSingleton<IBuildTool>(w.Build)
            .AddSingleton<IPullRequestInfoRepository>(w.PullRequests)
            .AddTaskServices()
            .AddSingleton<IBranchSelection>(w.Selection);
        var app = builder.AddAssembly(typeof(Program).Assembly).UseMiddleware<CommandsMiddleware>().UseDefaultHelpResources().Build();
        return await app.RunAsync();
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
    [InlineData("-pr")]
    public async Task FrameworkBindsTargetsAndResolvesServices(string command)
    {
        var w = new TaskWorld(); w.SeedTwo();
        Assert.Equal(0, await RunAsync(w, command, "PROJ-123"));
        if (command is "-build" or "-push" or "-pr")
            Assert.Equal([("Api", "feature/PROJ-123-api"), ("Web", "fix/PROJ-123-ui")], w.Build.Calls);
        if (command == "-pr") Assert.Equal(2, w.GitHub.Created.Count);
        if (command == "-r") Assert.Equal(2, w.Git.Calls.Count(c => c.Operation == "delete"));
    }

    [Fact]
    public async Task FrameworkBindsTaskMetadataAndCreationArguments()
    {
        var w = new TaskWorld(); w.Git.AddRepository("Api");
        Assert.Equal(0, await RunAsync(w, "-t", "P-1", "-summary", "summary", "-description", "description", "norepo"));
        Assert.Empty((await w.Repository.GetAsync("P-1"))!.Repositories);
        Assert.Equal(0, await RunAsync(w, "-n", "P-1", "api"));
        Assert.Equal("feature/P-1", Assert.Single((await w.Repository.GetAsync("P-1"))!.Repositories).BranchName);
        Assert.Equal(0, await RunAsync(w, "-t", "P-1", "-branch", "Explicit/P-1", "Api"));
        Assert.Equal("Explicit/P-1", Assert.Single((await w.Repository.GetAsync("P-1"))!.Repositories).BranchName);
    }

    [Fact]
    public async Task FrameworkHonorsFlagsAndReturnsValidationCode()
    {
        var w = new TaskWorld(); w.SeedTwo();
        Assert.Equal(0, await RunAsync(w, "-pr", "PROJ-123", "nopush", "nobuild", "Api"));
        Assert.Empty(w.Build.Calls);
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation == "push");
        Assert.Equal("Api", Assert.Single(w.GitHub.Created).Repository);
        Assert.Equal(2, await RunAsync(w, "-rb", "PROJ-123", "Api"));
    }

    [Fact]
    public async Task FrameworkResetsAndShowsAllMappings()
    {
        var w = new TaskWorld(); w.SeedTwo(); w.Git.Remote["Api"] = ["New/PROJ-123"];
        Assert.Equal(0, await RunAsync(w, "-rb", "PROJ-123"));
        var writer = new StringWriter(); var original = Console.Out;
        try
        {
            Console.SetOut(writer);
            Assert.Equal(0, await RunAsync(w, "-ti", "PROJ-123"));
        }
        finally { Console.SetOut(original); }
        Assert.Contains("Api -> New/PROJ-123", writer.ToString());
        Assert.Contains("Web -> fix/PROJ-123-ui", writer.ToString());
        Assert.DoesNotContain(w.Git.Calls, c => c.Operation is "checkout" or "create" or "pull" or "track");
    }

    [Theory]
    [InlineData("garbage\n9\n\n2\n", "second")]
    [InlineData("1\n", "first")]
    public async Task BranchSelectionRetriesInvalidInput(string input, string expected)
    {
        var original = Console.In;
        try
        {
            Console.SetIn(new StringReader(input));
            Assert.Equal(expected, await new ConsoleBranchSelection().SelectAsync("Api", ["first", "second"]));
        }
        finally { Console.SetIn(original); }
    }

    [Theory]
    [InlineData("")]
    [InlineData("0\n")]
    [InlineData("invalid\n")]
    public async Task BranchSelectionCancelsOnZeroOrEof(string input)
    {
        var original = Console.In;
        try
        {
            Console.SetIn(new StringReader(input));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ConsoleBranchSelection().SelectAsync("Api", ["first", "second"]));
        }
        finally { Console.SetIn(original); }
    }

    [Theory]
    [InlineData("-SUMMARY")]
    [InlineData("-description")]
    [InlineData("-branch")]
    public async Task MetadataParserRejectsMissingValuesWithoutIndexErrors(string option)
        => await Assert.ThrowsAsync<ArgsValidationException>(() => new SaveTaskInfoCommandArgsBuilder(new TaskWorld().Catalog).BuildAsync(["-t", "P-1", option]));

    [Fact]
    public async Task MetadataParserIsCaseInsensitiveAndRejectsConflictingFlags()
    {
        var w = new TaskWorld(); w.Git.AddRepository("Api");
        var builder = new SaveTaskInfoCommandArgsBuilder(w.Catalog);
        var values = await builder.BuildAsync(["-t", "P-1", "-SUMMARY", "summary", "-DESCRIPTION", "description", "NOREPO"]);
        Assert.Equal("summary", values["TaskSummary"]);
        Assert.True((bool)values["NoRepo"]);
        await Assert.ThrowsAsync<ArgsValidationException>(() => builder.BuildAsync(["-t", "P-1", "norepo", "Api"]));
        await Assert.ThrowsAsync<ArgsValidationException>(() => builder.BuildAsync(["-t", "P-1", "-branch", "x", "usedefaultbranch", "Api"]));
        Assert.Equal(0, w.Repository.Writes);
    }
}
