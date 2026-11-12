using Gbm.Services.Git;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Services.Tasks;

public sealed class RepositoryCatalog(IGitTool gitTool)
{
    public async Task<string[]> GetAsync(CancellationToken cancellationToken = default)
    {
        var repositories = await gitTool.GetAllRepositoriesAsync(cancellationToken)
            .ToArrayAsync(cancellationToken);
            
        return [.. repositories
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<string[]> ValidateAsync(string[] requested, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (requested.Length == 0) return [];
        var inventory = await GetAsync(cancellationToken);
        return Validate(requested, inventory);
    }

    public static string[] Validate(string[] requested, string[] inventory)
    {
        var result = new List<string>();
        foreach (var name in requested.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name) || name is "." or ".." || name.IndexOfAny(['/', '\\', ':']) >= 0)
                throw new ArgsValidationException($"Invalid repository name '{name}'.");
            var canonical = inventory.FirstOrDefault(r => r.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgsValidationException($"Repository '{name}' was not found under BasePath.");
            result.Add(canonical);
        }
        return [.. result];
    }
}
