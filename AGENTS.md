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
│   ├── BranchesCommands/           # Branch lifecycle and build commands (-n, -l, -s, -u, -build, -push, -pull, -d, -r)
│   ├── ConfigurationCommands/      # Tool setup commands (base path, tokens, formats)
│   ├── PullRequestCommands/        # GitHub PR creation/cross-linking (-pr) and listing (-pri)
│   └── TaskInfoCommands/           # Task metadata storage and display (-t, -ti)
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

Configure the base path and Jira domain before commands that access repositories or local task/PR data. GitHub token and owner are required for GitHub PR operations; Jira authentication settings enable Jira integration.

| Command | Description |
|---|---|
| `gbm -b <BasePath>` | Set the fully qualified root containing repositories and local task/PR data |
| `gbm -gt <GitHubToken>` | Set GitHub personal access token |
| `gbm -go <RepositoriesOwner>` | Set GitHub repositories owner (org or user) |
| `gbm -jd <Domain>` | Set Jira domain |
| `gbm -ju <UserMail>` | Set Jira user email |
| `gbm -jp <UserPassword>` | Set Jira user password |
| `gbm -jc <ConsumerKey>` | Set Jira OAuth consumer key |
| `gbm -js <ConsumerSecret>` | Set Jira OAuth consumer secret |
| `gbm -ja <JiraAccessToken>` | Set Jira OAuth access token |
| `gbm -jt <JiraTokenSecret>` | Set Jira OAuth token secret |
| `gbm -bn <BranchDefaultNameFormat>` | Set default branch name format (supports `{TaskId}`, `{TaskSummary}`) |
| `gbm -tlr <true\|false>` | List repositories after task creation |
| `gbm -v` | Display the current version |

### Task Commands (`2. Tasks Commands`)

| Command | Description |
|---|---|
| `gbm -t <TaskId> [-summary <Task Summary>] [-description <Task Description>] [-branch <Task Branch>\|usedefaultbranch] [norepo\|Repos...]` | Save task metadata. Prompts for missing values and can create branches in selected repositories; `norepo` skips repository selection/branch creation. |
| `gbm -ti <TaskId>` | Show locally saved task summary, description, and branch name |

### Branch Commands (`3. Branchs Commands`)

Branch commands accept `[Repos...]` as optional positional arguments. When omitted, repositories are auto-detected from those that contain the task branch; `-s` also considers remote branches. `-n` requires at least one repository.

| Command | Example | Description |
|---|---|---|
| `-n` | `gbm -n <TaskId> [Repos...]` | Create new task branch in each repo (from updated `main`) |
| `-l` | `gbm -l <TaskId> [Repos...]` | List repos that have the task branch |
| `-s` | `gbm -s <TaskId/Branch> [Repos...]` | Checkout task branch in each repo and pull |
| `-u` | `gbm -u <TaskId> [--origin <TaskId>] [Repos...]` | Update task branch from main (or another task branch via `--origin`) |
| `-build` | `gbm -build <TaskId> [Repos...]` | Check out task branches and validate builds |
| `-push` | `gbm -push <TaskId> [nobuild] [Repos...]` | Validate builds and push task branches; `nobuild` skips validation |
| `-pull` | `gbm -pull <TaskId> [Repos...]` | Pull task branch from remote in each repo |
| `-d` | `gbm -d <TaskId> [nobuild] [Repos...]` | Merge task branch into `develop` and push; repositories ending in `sdk` are skipped, and `nobuild` skips build validation |
| `-r` | `gbm -r <TaskId> [Repos...]` | Delete local task branch in each repo |

### Pull Request Commands (`4. Pull Requests Commands`)

| Command | Example | Description |
|---|---|---|
| `-pr` | `gbm -pr <TaskId> [nopush] [nobuild] [Repos...]` | Get task information from Jira, validate builds, create GitHub PRs, and cross-link related PRs. Pushes local changes unless `nopush`; `nobuild` skips build validation. |
| `-pri` | `gbm -pri <TaskId>` | List saved PR information for a task |

### Task information commands (`2. Tasks Commands`)

`-t` accepts `-summary`, `-description`, and `-branch` options; `usedefaultbranch` selects the configured branch format. Missing values are collected interactively.

---

## Key Concepts

- **BasePath**: The root folder where all related repositories live as subdirectories. Set with `-b`.
- **TaskId**: A unique identifier for a task (e.g., a Jira ticket like `PROJ-123`). Used to look up the stored branch name.
- **BranchDefaultNameFormat**: A configurable template (e.g., `feature/{TaskId}-{TaskSummary}`) used to auto-generate branch names when saving a task.
- **Cross-repo operations**: Branch commands iterate over the resolved repository list and run Git operations sequentially. Build validation uses Visual Studio MSBuild 18 Insiders; top-level `.bicep` files are built through Azure CLI.
- **PR cross-linking**: The `-pr` command creates PRs on GitHub and updates all of them with a **Related PRs** section linking to each other.

---

## Development Guide

### Tech Stack

- **.NET 10** — C# top-level statements, records, and primary constructors
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
- Commands return `0` for success and generally `1` for handled errors; argument validation handled by middleware returns `2`.
- All git, GitHub, and Jira operations are async and accept a `CancellationToken`.
- Repository names are matched case-insensitively.
- Repositories whose names end with `sdk` are skipped in the `-d` (send-to-develop) command.
