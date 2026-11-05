# Git Branch Manager (`gbm`)

`gbm` is a .NET CLI for managing task branches across related Git repositories. A task ID can resolve to locally saved task information and a configured branch name; branch operations can then target explicit repositories or discover repositories that contain the branch.

## Requirements

- .NET 10 SDK
- Git available on `PATH`
- Repositories as subdirectories of a configured base path
- Visual Studio MSBuild 18 Insiders for the `-build` command and build validation in `-push` / `-pr`; the build tool also uses Azure CLI (`az`) to build top-level `.bicep` files

## Install and run

Build from source:

```cmd
git clone https://github.com/regismantunes/GitBranchsManager
cd GitBranchsManager
dotnet build GitBranchManager.sln
```

Run commands through the Windows wrapper or directly through the project:

```cmd
gbm.cmd <command> [args]
dotnet run --project Gbm -- <command> [args]
```

## Quick start

Configure the base directory and, for GitHub pull requests, the repository owner and token:

```cmd
gbm -b "C:\Projects"
gbm -gt <GitHubToken>
gbm -go <GitHubOwner>
gbm -jd <JiraDomain>
```

Set Jira credentials with `-ju` and `-jp` or the OAuth options (`-jc`, `-js`, `-ja`, `-jt`) when Jira authentication is needed.

Save task information interactively or supply it on the command line. When a summary or description prompt is left blank, `gbm` reads that value from the clipboard. `norepo` skips repository selection/branch creation; otherwise, repositories can be specified at the end:

```cmd
gbm -t PROJ-123
gbm -t PROJ-123 -summary "Fix login" -description "Correct login handling" -branch "feature/PROJ-123-fix-login" api frontend
```

Create branches, validate builds, then open pull requests:

```cmd
gbm -n PROJ-123 api frontend
gbm -build PROJ-123 api frontend
gbm -pr PROJ-123
```

## Command reference

### Configuration

| Command | Description |
|---|---|
| `gbm -b <BasePath>` | Set the fully qualified root directory containing repositories and local task/PR data |
| `gbm -gt <GitHubToken>` | Set the GitHub personal access token |
| `gbm -go <RepositoriesOwner>` | Set the GitHub organization or user that owns the repositories |
| `gbm -jd <Domain>` | Set the Jira domain |
| `gbm -ju <UserMail>` | Set Jira user email (for username/password authentication) |
| `gbm -jp <UserPassword>` | Set Jira user password |
| `gbm -jc <ConsumerKey>` | Set Jira OAuth consumer key |
| `gbm -js <ConsumerSecret>` | Set Jira OAuth consumer secret |
| `gbm -ja <JiraAccessToken>` | Set Jira OAuth access token |
| `gbm -jt <JiraTokenSecret>` | Set Jira OAuth token secret |
| `gbm -bn <BranchDefaultNameFormat>` | Set the default branch format; supports `{TaskId}` and `{TaskSummary}` |
| `gbm -tlr <true\|false>` | Set whether to list repositories after task creation |
| `gbm -v` | Display the application version |

### Tasks

| Command | Description |
|---|---|
| `gbm -t <TaskId> [-summary <Task Summary>] [-description <Task Description>] [-branch <Task Branch>\|usedefaultbranch] [norepo\|Repos...]` | Save task information; missing values are prompted for. Can optionally create branches in the selected repositories. |
| `gbm -ti <TaskId>` | Show locally saved task summary, description, and branch name |

Example: `gbm -t PROJ-123 -summary "Fix login" -description "Correct login handling" usedefaultbranch api frontend`

### Branches

Branch commands accept optional repository names. If omitted, repositories containing the task branch are discovered automatically. `-s` also considers remote branches. `-n` requires at least one repository.

| Command | Description |
|---|---|
| `gbm -n <TaskId> [Repos...]` | Create task branches from the updated `main` branch |
| `gbm -l <TaskId> [Repos...]` | List the selected repositories that have the task branch |
| `gbm -s <TaskId/Branch> [Repos...]` | Check out the task branch and pull its latest changes |
| `gbm -u <TaskId> [--origin <TaskId>] [Repos...]` | Update the task branch from `main`, or from another task branch with `--origin` |
| `gbm -build <TaskId> [Repos...]` | Check out the task branch and validate builds in each repository |
| `gbm -push <TaskId> [nobuild] [Repos...]` | Validate builds and push the task branches; `nobuild` skips validation |
| `gbm -pull <TaskId> [Repos...]` | Pull the task branches from their remotes |
| `gbm -d <TaskId> [nobuild] [Repos...]` | Merge task branches into `develop` and push; repositories ending in `sdk` are skipped, and `nobuild` skips build validation |
| `gbm -r <TaskId> [Repos...]` | Delete local task branches |

### Pull requests

| Command | Description |
|---|---|
| `gbm -pr <TaskId> [nopush] [nobuild] [Repos...]` | Get task information from Jira, validate builds, push local changes unless `nopush` is used, create GitHub PRs, and cross-link related PRs. `nobuild` skips build validation. |
| `gbm -pri <TaskId>` | List saved pull request information for a task |

## Branch name format

Set the default format with `-bn`:

```cmd
gbm -bn "feature/{TaskId}-{TaskSummary}"
```

`{TaskId}` is replaced with the task ID and `{TaskSummary}` with a branch-safe form of the task summary. When saving a task, `usedefaultbranch` selects this configured format; `-branch` can provide a custom name.

## Data and configuration

User settings are stored in `%APPDATA%\Gbm\config.json`. Task information is stored in `tasksinfo.json` and pull request information in `pullrequestsinfo.json`, both under the configured base path.

## Project structure

```text
Gbm/                 CLI application, commands, integrations, and persistence
Gbm.Unit.Tests/      xUnit unit tests
gbm.cmd              Windows launcher
GitBranchManager.sln Solution
```

## License

[MIT](LICENSE)
