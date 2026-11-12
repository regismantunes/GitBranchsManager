using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Commands.PullRequestCommands;

public sealed class OpenPullRequestsTaskCommandArgsBuilder(ITaskResolver tasks, ITaskBranchTargetResolver targets,
    RepositoryCatalog catalog, IPullRequestInfoRepository pullRequests) : IArgsBuilderAsync
{
    public async Task<IDictionary<string, object>> BuildAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args is null || args.Length == 0) throw new ArgumentException("Arguments cannot be null or empty.", nameof(args));
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]) || args[1].StartsWith('-')) throw new ArgsValidationException("Missing TaskId.");
        if (!args[0].Equals("-pr", StringComparison.OrdinalIgnoreCase)) throw new ArgsValidationException("Expected -pr.");
        var i = 2;
        var push = true;
        var noBuild = false;
        if (args.Length > i && args[i].Equals("nopush", StringComparison.OrdinalIgnoreCase)) { push = false; i++; }
        if (args.Length > i && args[i].Equals("nobuild", StringComparison.OrdinalIgnoreCase)) { noBuild = true; i++; }
        var repos = args[i..];
        if (repos.Any(r => r.StartsWith('-') || r.Equals("nopush", StringComparison.OrdinalIgnoreCase) || r.Equals("nobuild", StringComparison.OrdinalIgnoreCase)))
            throw new ArgsValidationException("Expected options in order: [nopush] [nobuild] [Repos...].");
        repos = await catalog.ValidateAsync(repos, cancellationToken);
        var task = await tasks.GetAsync(args[1], cancellationToken);
        var saved = (await pullRequests.GetByTaskIdAsync(task.Id, cancellationToken)).Select(p => p.Repository).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var resolved = await targets.ResolveAsync(new(task, null), repos, "-pr", cancellationToken, saved);
        return new Dictionary<string, object>
        {
            ["TaskInfo"] = task, ["Targets"] = resolved, ["PushLocalChanges"] = push, ["NoBuild"] = noBuild
        };
    }
}
