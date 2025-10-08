using Gbm.Git;

namespace Gbm.Commands.Args
{
    public record ArgsContext(
        string Action,
        string? BasePath = null,
        GitTool? GitTool = null,
        string? TaskBranch = null,
        string[]? Repositories = null
    );
}