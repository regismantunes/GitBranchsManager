using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Commands.TaskInfoCommands;

public sealed class SaveTaskInfoCommandArgsBuilder(RepositoryCatalog catalog) : IArgsBuilderAsync
{
    public async Task<IDictionary<string, object>> BuildAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args is null || args.Length == 0) throw new ArgumentException("Arguments cannot be null or empty.", nameof(args));
        if (!args[0].Equals("-t", StringComparison.OrdinalIgnoreCase)) throw new ArgsValidationException("Expected -t.");
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]) || args[1].StartsWith('-')) throw new ArgsValidationException("Missing TaskId.");
        string? summary = null, description = null, branch = null;
        var noRepo = false;
        var useDefault = false;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var repos = new List<string>();
        for (var i = 2; i < args.Length; i++)
        {
            var option = args[i].ToLowerInvariant();
            if (option is "-summary" or "-description" or "-branch" or "norepo" or "usedefaultbranch")
            {
                if (!seen.Add(option)) throw new ArgsValidationException($"Duplicate option '{option}'.");
                if (option == "norepo") { noRepo = true; continue; }
                if (option == "usedefaultbranch") { useDefault = true; continue; }
                if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i]) || args[i].StartsWith('-'))
                    throw new ArgsValidationException($"Missing value for '{option}'.");
                switch (option)
                {
                    case "-summary": summary = args[i]; break;
                    case "-description": description = args[i]; break;
                    case "-branch": branch = args[i]; break;
                }
            }
            else
            {
                if (args[i].StartsWith('-')) throw new ArgsValidationException($"Unknown option '{args[i]}'.");
                repos.Add(args[i]);
            }
        }
        if (branch is not null && useDefault) throw new ArgsValidationException("-branch and usedefaultbranch cannot be combined.");
        if (noRepo && (repos.Count > 0 || branch is not null || useDefault))
            throw new ArgsValidationException("norepo cannot be combined with repositories or branch options.");
        var canonical = await catalog.ValidateAsync(repos.ToArray(), cancellationToken);
        return new Dictionary<string, object>
        {
            ["TaskId"] = args[1], ["TaskSummary"] = summary!, ["TaskDescription"] = description!,
            ["TaskBranch"] = branch!, ["UseDefaultBranch"] = useDefault, ["NoRepo"] = noRepo, ["Repositories"] = canonical
        };
    }
}
