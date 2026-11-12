using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Commands.BranchesCommands;

public sealed class BranchesCommandArgsBuilder(ITaskResolver tasks, ITaskBranchTargetResolver targets, RepositoryCatalog catalog) : IArgsBuilderAsync
{
    public async Task<IDictionary<string, object>> BuildAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args is null || args.Length == 0) throw new ArgumentException("Arguments cannot be null or empty.", nameof(args));
        var command = args[0].ToLowerInvariant();
        if (command is not ("-n" or "-l" or "-s" or "-u" or "-build" or "-push" or "-pull" or "-d" or "-r"))
            throw new ArgsValidationException($"Unknown branch command '{command}'.");
        var i = 1;
        var literal = command == "-s" && args.Length > i && args[i].Equals("--branch", StringComparison.OrdinalIgnoreCase);
        if (literal) i++;
        if (args.Length <= i || string.IsNullOrWhiteSpace(args[i]) || args[i].StartsWith('-'))
            throw new ArgsValidationException($"Missing TaskId/Branch. Example: gbm {command} TaskId [Repos...]");
        var value = args[i++];
        string? origin = null;
        var literalOrigin = false;
        var noBuild = false;
        if (command == "-u" && args.Length > i &&
            (args[i].Equals("--origin", StringComparison.OrdinalIgnoreCase) || args[i].Equals("--origin-branch", StringComparison.OrdinalIgnoreCase)))
        {
            literalOrigin = args[i++].Equals("--origin-branch", StringComparison.OrdinalIgnoreCase);
            if (args.Length <= i || string.IsNullOrWhiteSpace(args[i]) || args[i].StartsWith('-'))
                throw new ArgsValidationException("Missing origin TaskId/Branch.");
            origin = args[i++];
        }
        if (command is "-push" or "-d" && args.Length > i && args[i].Equals("nobuild", StringComparison.OrdinalIgnoreCase))
        {
            noBuild = true;
            i++;
        }
        var repos = args[i..];
        if (repos.Any(r => r.StartsWith('-') || r.Equals("nobuild", StringComparison.OrdinalIgnoreCase)))
            throw new ArgsValidationException("Invalid, duplicate or misplaced option.");
        if (command == "-n" && repos.Length == 0)
            throw new ArgsValidationException("Missing repositories. Example: gbm -n TaskId Repo1 Repo2");
        repos = await catalog.ValidateAsync(repos, cancellationToken);
        if (command == "-n")
            return new Dictionary<string, object>
            {
                ["TaskInfo"] = await tasks.GetAsync(value, cancellationToken), ["Repositories"] = repos
            };
        var reference = command == "-s"
            ? await tasks.ResolveReferenceAsync(value, literal, cancellationToken)
            : new TaskBranchReference(await tasks.GetAsync(value, cancellationToken), null);
        var resolved = await targets.ResolveAsync(reference, repos, command, cancellationToken);
        if (origin is not null)
            resolved = await targets.WithOriginAsync(resolved, await tasks.ResolveReferenceAsync(origin, literalOrigin, cancellationToken), cancellationToken);
        return new Dictionary<string, object> { ["Targets"] = resolved, ["NoBuild"] = noBuild };
    }
}
