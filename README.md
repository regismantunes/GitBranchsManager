# Git Branch Manager (`gbm`)

`gbm` is a .NET CLI for managing task branches across related Git repositories. Each task stores a separate branch name for each repository: for example, `Api -> feature/PROJ-123-api` and `Web -> fix/PROJ-123-ui`. Commands resolve these mappings before operating on the selected repositories.

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
gbm -bn "feature/{TaskId}"
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
| `gbm -ti <TaskId>` | Show summary, description, URL and repository/branch mappings; import an unknown task |
| `gbm -rb <TaskId>` | Rediscover remote branches across all repositories and replace the task's mappings without changing Git refs or checkout |

Example: `gbm -t PROJ-123 -summary "Fix login" -description "Correct login handling" usedefaultbranch api frontend`

### Branches

Branch commands accept optional repository names, matched without case sensitivity. If omitted, eligible repositories are selected from the saved mappings. Most commands require a local branch; `-s` also considers branches in `origin` and can create their local tracking branches. `-n` requires at least one repository. Explicit repositories must exist under the base path and have a mapping, except when creating one with `-n` or `-t`.

| Command | Description |
|---|---|
| `gbm -n <TaskId> <Repos...>` | Reuse existing mappings; generate names for unmapped repos using `-bn`. Track an existing remote branch or create from the updated main/master branch |
| `gbm -l <TaskId> [Repos...]` | List the selected repositories that have the task branch |
| `gbm -s <TaskId/Branch> [Repos...]` | Check out the task branch and pull its latest changes |
| `gbm -s --branch <Branch> [Repos...]` | Force an exact literal branch, even if its name matches a task ID |
| `gbm -u <TaskId> [--origin <TaskId/Branch>\|--origin-branch <Branch>] [Repos...]` | Update from each repo's principal branch, another task's mapping in that repo, or a forced literal origin branch |
| `gbm -build <TaskId> [Repos...]` | Check out the task branch and validate builds in each repository |
| `gbm -push <TaskId> [nobuild] [Repos...]` | Validate builds and push the task branches; `nobuild` skips validation |
| `gbm -pull <TaskId> [Repos...]` | Pull the task branches from their remotes |
| `gbm -d <TaskId> [nobuild] [Repos...]` | Merge task branches into `develop` and push; repositories ending in `sdk` are skipped, and `nobuild` skips build validation |
| `gbm -r <TaskId> [Repos...]` | Delete local task branches while retaining the saved mappings |

### Pull requests

| Command | Description |
|---|---|
| `gbm -pr <TaskId> [nopush] [nobuild] [Repos...]` | Resolve task mappings, validate all new PR builds, push unless `nopush`, create PRs with each repo's mapped head and cross-link them. `nobuild` skips validation. |
| `gbm -pri <TaskId>` | List saved pull request information for a task |

## Branch name format

Set the default format with `-bn`:

```cmd
gbm -bn "feature/{TaskId}-{TaskSummary}"
```

`{TaskId}` and `{TaskSummary}` are replaced verbatim. There is no automatic slug conversion: if the result is not a valid Git branch name (for example, it contains spaces), adjust the format or supply `-t -branch`. Names and capitalization are preserved. Changing the format does not rename or replace existing mappings.

When saving a task, `usedefaultbranch` selects the format; `-branch` supplies a custom name for the selected repos. These options cannot be combined. `norepo` saves only metadata and cannot be combined with repositories or branch options. Updating metadata preserves existing mappings.

## Importing and rediscovering task branches

An unknown task is loaded from Jira and searched across all Git repositories directly under `BasePath`, even if the requested operation selects only some repositories. Discovery queries live heads from `origin` without fetching. Names are matched by task-ID substring, ignoring case: `PROJ-12` also matches a branch containing `PROJ-123`.

One match is selected automatically. Multiple matches prompt for a numbered choice per repo; `0` or closed input cancels the entire discovery without saving partial selections. A successful search with no matches saves the task metadata with an empty mapping list. Commands requiring a branch then report that none is available; use `-n` to create one or `-rb` to rediscover. Saved tasks, including empty ones, are not automatically rediscovered.

```cmd
gbm -s PROJ-123
gbm -ti PROJ-123
gbm -rb PROJ-123
gbm -u PROJ-123 --origin PROJ-100 Api Web
gbm -s --branch PROJ-123 Api
```

In `-s` and `--origin`, saved tasks take precedence. An unknown name shaped like a Jira key (`PROJ-123`) is imported; other names are literal branches. Use `--branch` or `--origin-branch` to force a literal name. The legacy `-s main`/`-s master` shortcut selects each repo's principal branch; `-s --branch main` requests exactly `main`.

`-rb` replaces all mappings and accepts no repository filter. It removes mappings absent from the new results, including missing directories and repos without `origin`. A completed empty discovery removes all mappings. An inaccessible base path, failed remote query or cancelled selection preserves the previous file. Checkout, working files and Git refs remain unchanged.

Branch creation saves each confirmed mapping separately. If a later repository fails, completed creations/mappings remain; retrying recognizes existing branches. Checkout failures prevent subsequent pull, push or PR creation in that repository. Operations across repositories are not rolled back as a group.

Existing PRs remain identified by task/repository. Rediscovering a different branch does not change an open PR's head, and `-pr` does not create a replacement for a locally recorded PR. Existing PRs still receive related links; reconcile their heads separately when required.

## Data and configuration

User settings are stored in `%APPDATA%\Gbm\config.json`. Task information is stored in `tasksinfo.json` and pull request information in `pullrequestsinfo.json`, both under the configured base path.

The new task data format is:

```json
[
  {
    "Id": "PROJ-123",
    "Summary": "Fix login",
    "Description": "Correct login handling",
    "Url": "https://example.atlassian.net/browse/PROJ-123",
    "Repositories": [
      { "Repository": "Api", "BranchName": "feature/PROJ-123-api" },
      { "Repository": "Web", "BranchName": "fix/PROJ-123-ui" }
    ]
  }
]
```

**The previous format with a task-level `BranchName` is incompatible and is not automatically migrated.** Preserve a backup of the old `tasksinfo.json` before removing it from the configured location and recreating tasks using `-rb`, `-t` or automatic import. Legacy or invalid data is rejected without overwriting it. To return to an older gbm version, restore its corresponding data backup.

Writes use a temporary file and replace the destination after successful serialization. Updates are serialized within one process; concurrent gbm processes editing the same data file are not supported.

## Project structure

```text
Gbm/                 CLI application, commands, integrations, and persistence
Gbm.Unit.Tests/      xUnit unit tests
gbm.cmd              Windows launcher
GitBranchManager.sln Solution
```

## License

[MIT](LICENSE)
