# AGENTS.md — Git Branch Manager (gbm)

## Project Overview

**Git Branch Manager (gbm)** is a .NET CLI tool that orchestrates git branch operations across multiple related repositories simultaneously. It maps a task/ticket ID (e.g., a Jira issue) to a branch name and applies branch commands — create, checkout, pull, push, merge, remove — to all configured repos in one invocation.

The solution is structured as two projects:

| Project | Purpose |
|---|---|
| `Gbm/` | Main CLI application |
| `Gbm.Unit.Tests/` | xUnit unit tests |

---

## Architecture

```
Gbm/
├── Program.cs                      # Entry point; wires DI and runs the console app
├── Commands/                       # CLI commands (grouped by domain)
│   ├── CommandGroups.cs            # Group name constants
│   ├── BranchesCommands/           # Branch lifecycle commands (-n, -l, -s, -u, -push, -pull, -d, -r)
│   ├── ConfigurationCommands/      # Tool setup commands (base path, tokens, formats)
│   ├── PullRequestCommands/        # GitHub PR creation and cross-linking (-pr, -prl)
│   └── TaskInfoCommands/           # Task metadata storage (-t)
├── Services/                       # Business logic / external integrations
│   ├── Git/                        # IGitTool / GitTool — wraps git CLI
│   ├── GitHub/                     # IGitHubClient / GitHubClient — GitHub REST API
│   ├── Jira/                       # IJiraClient / JiraClient — Jira REST API
│   ├── Configuration/              # IConfiguration extensions
│   ├── Initialization/             # DI registration (IServiceCollectionExtensions)
│   └── Middleware/                 # CommandsMiddleware — pre/post command hooks
└── Persistence/
    ├── Configuration/              # ConfigurationVariable enum (stored settings keys)
    ├── Entities/                   # TaskInfo, PullRequestInfo records
    └── Repositories/               # Local JSON persistence (task info, PR info)
```

---

## Command Reference

### Configuration Commands (`1. Configuration Commands`)

These must be set up before using branch or PR commands.

| Command | Description |
|---|---|
| `gbm --set-base-path <path>` | Set the root directory containing all repositories |
| `gbm --set-github-token <token>` | Set GitHub personal access token |
| `gbm --set-github-owner <owner>` | Set GitHub repositories owner (org or user) |
| `gbm --set-jira-domain <domain>` | Set Jira domain |
| `gbm --set-jira-mail <mail>` | Set Jira user email |
| `gbm --set-jira-password <password>` | Set Jira user password |
| `gbm --set-branch-format <format>` | Set default branch name format (supports `{TaskId}`, `{TaskSummary}`) |
| `gbm --set-list-repos <true\|false>` | List repositories after task creation |
| `gbm --version` | Display the current version |

### Task Commands (`2. Tasks Commands`)

| Command | Description |
|---|---|
| `gbm -t <TaskId>` | Save task metadata (summary, description, branch name). Prompts interactively; reads from clipboard if input is empty. Optionally creates branches immediately after. |

### Branch Commands (`3. Branchs Commands`)

All branch commands accept `[Repos...]` as optional positional arguments. When omitted, repositories are auto-detected from those that contain the task branch.

| Command | Example | Description |
|---|---|---|
| `-n` | `gbm -n <TaskId> [Repos...]` | Create new task branch in each repo (from main, after pull) |
| `-l` | `gbm -l <TaskId> [Repos...]` | List repos that have the task branch |
| `-s` | `gbm -s <TaskId/Branch> [Repos...]` | Checkout task branch in each repo and pull |
| `-u` | `gbm -u <TaskId> [--origin <TaskId>] [Repos...]` | Update task branch from main (or another task branch via `--origin`) |
| `-push` | `gbm -push <TaskId> [Repos...]` | Push task branch to remote in each repo |
| `-pull` | `gbm -pull <TaskId> [Repos...]` | Pull task branch from remote in each repo |
| `-d` | `gbm -d <TaskId> [Repos...]` | Merge task branch into `develop` and push |
| `-r` | `gbm -r <TaskId> [Repos...]` | Delete local task branch in each repo |

### Pull Request Commands (`4. Pull Requests Commands`)

| Command | Example | Description |
|---|---|---|
| `-pr` | `gbm -pr <TaskId> [nopush] [Repos...]` | Create GitHub PRs for task branches; cross-links all PRs in their descriptions. Pushes local changes unless `nopush` is provided. |
| `-prl` | `gbm -prl <TaskId>` | List existing PR info saved for a task |

---

## Key Concepts

- **BasePath**: The root folder where all related repositories live as subdirectories. Set with `--set-base-path`.
- **TaskId**: A unique identifier for a task (e.g., a Jira ticket like `PROJ-123`). Used to look up the stored branch name.
- **BranchDefaultNameFormat**: A configurable template (e.g., `feature/{TaskId}-{TaskSummary}`) used to auto-generate branch names when saving a task.
- **Cross-repo operations**: All branch commands iterate over the resolved list of repositories, running git operations sequentially on each.
- **PR cross-linking**: The `-pr` command creates PRs on GitHub and updates all of them with a **Related PRs** section linking to each other.

---

## Development Guide

### Tech Stack

- **.NET 8** — C# 12, top-level records, primary constructors
- **xUnit** — unit tests in `Gbm.Unit.Tests/`
- **RA.Console.DependencyInjection** — command routing and DI wiring
- **TextCopy** — clipboard read support for task input

### Build & Run

```bash
# Build
dotnet build GitBranchManager.sln

# Run tests
dotnet test GitBranchManager.sln

# Run the CLI directly
dotnet run --project Gbm -- <args>

# Or use the wrapper script (Windows)
gbm.cmd <args>
```

### Adding a New Command

1. Create a class in the appropriate `Commands/<Group>/` folder.
2. Inject required services via the primary constructor.
3. Decorate the method with `[CommandAsync]` or `[CommandAsyncWithArgsBuilderAsync<T>]`, setting `Group`, `Order`, `Description`, and `Example`.
4. Register any new services in `Gbm/Services/Initialization/IServiceCollectionExtensions.cs`.
5. If the command needs custom argument parsing (e.g., resolving task ID → branch name), create an `IArgsBuilderAsync` implementation.

### Adding a New Configuration Variable

1. Add the key to `ConfigurationVariable` enum in `Gbm/Persistence/Configuration/ConfigurationVariable.cs`.
2. Add a setter command in `Gbm/Commands/ConfigurationCommands/`.
3. Read the value via `IConfiguration.GetValue(ConfigurationVariable.X)` (extension in `Services/Configuration/`).

### Testing

- Unit tests live in `Gbm.Unit.Tests/`, mirroring the `Gbm/` folder structure.
- Use the `Shared/` folder for mocks and test helpers.
- Run tests with `dotnet test`.

---

## Conventions

- Use `MyConsole` static helpers (`WriteCommandHeader`, `WriteStep`, `WriteSucess`, `WriteError`, `WriteInfo`) for all console output — do not use `Console` directly.
- Return `0` for success, `1` for handled errors.
- All git, GitHub, and Jira operations are async and accept a `CancellationToken`.
- Repository names are matched case-insensitively.
- Repositories whose names end with `sdk` are skipped in the `-d` (send-to-develop) command.
