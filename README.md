# Git Branch Manager (`gbm`)

A .NET CLI tool that manages git branches across **multiple related repositories** at once, driven by task/ticket IDs (e.g., Jira issues).

Instead of manually creating, switching, or merging branches in each repository, `gbm` lets you run one command and have it applied consistently across all relevant repos.

---

## Features

- 🌱 Create task branches across multiple repositories in a single command
- 🔀 Checkout, pull, push, and remove branches in all repos at once
- 🔄 Update task branches from main or from another task branch
- ⬅️ Merge task branches into `develop` and push
- 🔧 Create GitHub pull requests for all task branches, with automatic cross-linking
- 💾 Save task metadata (summary, description, branch name) locally, with Jira integration
- ⚙️ Per-user configuration (base path, GitHub token, Jira credentials, branch name format)

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git installed and available on `PATH`
- Repositories organized as subdirectories under a common **base path**

---

## Installation

### Option 1 — Run from source

```bash
git clone https://github.com/regismantunes/GitBranchsManager
cd GitBranchsManager
dotnet build Gbm
```

Then use `gbm.cmd` (Windows) to invoke the tool:

```cmd
gbm.cmd <command> [args]
```

### Option 2 — Publish a self-contained executable

```bash
dotnet publish Gbm -c Release
```

The `gbm.cmd` wrapper automatically finds the published executable.

---

## Quick Start

### 1. Set up the base path

```cmd
gbm -b "C:\Projects"
```

All repositories must be direct subdirectories of this path (e.g., `C:\Projects\my-api`, `C:\Projects\my-frontend`).

### 2. Configure GitHub (for PR commands)

```cmd
gbm --github-token <your-token>
gbm --github-owner <org-or-username>
```

### 3. Save a task

```cmd
gbm -t PROJ-123
```

Prompts for summary, description, and branch name. Clipboard content is used if input is empty. Optionally creates branches immediately.

### 4. Create branches in all repos

```cmd
gbm -n PROJ-123 my-api my-frontend my-sdk
```

### 5. Open pull requests

```cmd
gbm -pr PROJ-123
```

Creates GitHub PRs for each repo that has the task branch, and cross-links all PRs in their descriptions.

---

## Command Reference

### Configuration

| Command | Description |
|---|---|
| `gbm -b <path>` | Set base path (root of all repositories) |
| `gbm --github-token <token>` | Set GitHub personal access token |
| `gbm --github-owner <owner>` | Set GitHub repositories owner |
| `gbm --jira-domain <domain>` | Set Jira domain |
| `gbm --jira-mail <mail>` | Set Jira user email |
| `gbm --jira-password <password>` | Set Jira user password |
| `gbm -bn <format>` | Set branch name format (e.g., `feature/{TaskId}_{TaskSummary}`) |
| `gbm --list-repos <true\|false>` | List available repos interactively after task creation |
| `gbm --version` | Print current version |

### Tasks

| Command | Description |
|---|---|
| `gbm -t <TaskId>` | Save task metadata locally (and optionally create branches) |

### Branches

All branch commands accept an optional `[Repos...]` list. When omitted, `gbm` auto-detects repositories that contain the task branch.

| Command | Example | Description |
|---|---|---|
| `-n` | `gbm -n PROJ-123 api frontend` | Create task branch in each repo (from main, after pull) |
| `-l` | `gbm -l PROJ-123` | List repos that have the task branch |
| `-s` | `gbm -s PROJ-123` | Checkout task branch in each repo and pull |
| `-u` | `gbm -u PROJ-123 [--origin PROJ-100]` | Update task branch from main, or from another task branch |
| `-push` | `gbm -push PROJ-123` | Push task branch to remote |
| `-pull` | `gbm -pull PROJ-123` | Pull task branch from remote |
| `-d` | `gbm -d PROJ-123` | Merge task branch into `develop` and push |
| `-r` | `gbm -r PROJ-123` | Delete local task branch in each repo |

### Pull Requests

| Command | Example | Description |
|---|---|---|
| `-pr` | `gbm -pr PROJ-123 [nopush]` | Create GitHub PRs; cross-links all related PRs. Add `nopush` to skip pushing local changes. |
| `-pri` | `gbm -pri PROJ-123` | List saved PR info for a task |

---

## Branch Name Format

You can configure a default branch naming convention:

```cmd
gbm -bn "feature/{TaskId}_{TaskSummary}"
```

Supported placeholders:

| Placeholder | Replaced with |
|---|---|
| `{TaskId}` | The task ID (e.g., `PROJ-123`) |
| `{TaskSummary}` | The task summary (e.g., `fix-login-bug`) |

When running `gbm -t`, the formatted name is shown as the default — press Enter to accept it, or type a custom name.

---

## Project Structure

```
GitBranchsManager/
├── Gbm/                    # CLI application
│   ├── Commands/           # Command implementations
│   ├── Services/           # Git, GitHub, Jira integrations
│   └── Persistence/        # Local configuration and data storage
├── Gbm.Unit.Tests/         # xUnit unit tests
├── gbm.cmd                 # Windows launcher script
└── GitBranchManager.sln
```

---

## License

[MIT](LICENSE)
