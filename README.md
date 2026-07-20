<p align="center">
  <img src="https://github.com/View12138/GitPlus/raw/main/GitPlus/Assets/Logo.png" alt="GitPlus Logo" width="128" />
</p>

<h1 align="center">Git +</h1>

<p align="center">
  <strong>Visual Studio Git Enhancement Extension</strong>
  <br />
  Toolbar buttons, auto-fetch, smart sync and conventional commits on top of VS built-in Git tools.
</p>

<p align="center">
  <a href="https://github.com/View12138/GitPlus/blob/main/README.zh.md">中文文档</a>
</p>

---

## ✨ Features

### 🔄 Pull with Auto-Stash

One-click pull that automatically handles your local changes. A **Pull with Stash** button is injected into the Git Changes window toolbar, right next to the native Pull button. When clicked, it:

1. Checks for local changes via `git status`
2. Automatically stashes them if any exist (`git stash push`)
3. Runs `git pull` (optionally with `--rebase`)
4. Restores the stash (`git stash pop`)

No more "Cannot pull with uncommitted changes" errors — perfect for quickly syncing without interrupting your workflow.

### ⏱️ Auto-Fetch

Periodically runs `git fetch --all --prune` in the background at a configurable interval. Stay up-to-date with remote changes without lifting a finger. The auto-fetch interval badge is displayed on the Fetch button tooltip.

### 📝 Conventional Commits

Insert conventional commit templates directly into the Git commit message box. A **Insert Conventional Commit** button is injected into the Git Changes window, right next to the work item link button. Supports:

- Select from predefined commit types (`feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`, `ci`, `build`, `revert`), each with emoji and description
- Optional **scope** support — reads from a customizable scope JSON file in the solution root
- **BREAKING CHANGE** footer insertion
- Type replacement — selecting a different type replaces the existing one in the message
- Generates option template file via the `GENERATE OPTIONFILE` menu item

### ✅ Conventional Commit Syntax Validation

Real-time syntax validation of commit messages against the [Conventional Commits](https://www.conventionalcommits.org/) specification. When **strict validation** is enabled (via the `ConventionalCommitOption.json` option file), the extension:

- **Parses** the commit message in real time using a custom lexer/parser that understands the full conventional commit grammar (type, scope, description, body, footers)
- **Highlights** syntax errors and warnings directly in the commit text box with wave underlines — red for errors, yellow for warnings
- **Outlines** the commit message area with a red border when the format is invalid
- **Disables** the Commit button until all errors are resolved
- **Displays** diagnostic messages (CC001–CC014) describing exactly what's wrong

The validator checks for:
- Missing or invalid commit type
- Missing colon or space after type/scope
- Malformed scope parentheses
- Missing description text
- Invalid footer format (e.g., `BREAKING CHANGE`, `Reviewed-by`)
- Unexpected extra blank lines or whitespace

GitPlus supports **repository-level commit policies**. The `ConventionalCommitOption.json` file can be committed into source control, allowing all team members to share the same commit message rules. Generated from the **Insert Conventional Commit** menu, it defines custom commit types, allowed scopes, and custom footer keys — ensuring consistent commit conventions across the entire team.

### ⚙️ Configurable Options

All options are accessible via **Tools → Options → Git +**:

| Category   | Option                              | Default                       | Description                               |
|------------|-------------------------------------|-------------------------------|-------------------------------------------|
| General    | Timeout (seconds)                   | 30                            | Timeout for git operations                |
| General    | Git File Path                       | *(system PATH)*               | Custom path to `git.exe`                  |
| Auto Fetch | Enable Auto Fetch                   | ✅ On                          | Toggle background auto-fetch              |
| Auto Fetch | Fetch Interval (minutes)            | 5                             | Minutes between auto-fetch calls          |
| Pull       | Use Rebase on Pull                  | ✅ On                          | Use `--rebase` instead of merge           |
| Pull       | Show Auto Pull Button               | ✅ On                          | Show/hide the auto pull button            |
| Commit     | Show Conventional Commits Button    | ✅ On                          | Show/hide the conventional commits button |
| Commit     | Conventional Commit Scope File Name | ConventionalCommitScopes.json | File name for commit message scopes       |
| Logging    | Log Level                           | Information                   | Output verbosity in the Git + pane        |

### 🌐 Localization

The extension UI is localized in English and Simplified Chinese (zh-Hans).

---

## 📸 Screenshots

<p align="center">
  <img width="776px" src="https://github.com/View12138/GitPlus/raw/main/GitPlus/Assets/Screenshots1.gif" alt="GitPlus" style="vertical-align: top;" />
</p>

*The injected **Pull with Stash** button, **Conventional Commits** button, and real-time **syntax validation** in the Git Changes window.*

---

## 📦 Installation

### Prerequisites

- **Visual Studio 2022** (version 17.0 or later)
- **.NET Framework 4.7.2** or later

### From VSIX

Download the `.vsix` from [Releases](https://github.com/View12138/GitPlus/releases/latest) and double-click to install. Or build from source:

```powershell
dotnet build GitPlus.slnx -c Release
```

The output `.vsix` will be at:
```
GitPlus/bin/Release/net472/GitPlus.vsix
```

---

## 🛠️ Development

### Tech Stack

- **Target Framework**: `net472` (VS in-process extension)
- **Language**: C# 14 (latest pattern matching, records, file-scoped namespaces)
- **Key Libraries**:
  - `Microsoft.VisualStudio.SDK` — VS extensibility
  - `CommunityToolkit.Mvvm` — `AsyncRelayCommand`
  - `Lombok.NET` — `[RequiredArgsConstructor]` source-gen DI
  - `Microsoft.Extensions.DependencyInjection` — DI container
  - `Microsoft.Extensions.Logging.Abstractions` — structured logging

### Project Structure

```
GitPlus/
├── GitPlusPackage.cs              # AsyncPackage entry point
├── Configurations/
│   └── GitPlusOptionPage.cs       # Tools → Options dialog page
├── Injectors/
│   ├── InjectorBase.cs            # Abstract base for UI injectors
│   ├── GitWindowActionButtonPanelInjector.cs
│   └── GitWorkItemActionStackPanelInjector.cs
├── Services/
│   ├── WindowWatcher.cs           # DTE window lifecycle events
│   ├── GitCommandService.cs       # git.exe process wrapper
│   └── AutoFetchService.cs        # Periodic auto-fetch timer
├── Commons/
│   ├── Extensions.cs              # DI / WPF VisualTree helpers
│   ├── FileBrowserEditor.cs       # File browser UITypeEditor
│   ├── GitResult.cs               # Git operation result model
│   ├── GitWindowLocator.cs        # VisualTree element locator
│   ├── GitWindowViewModelExtensions.cs
│   ├── LocalizedAttributes.cs     # Localized attribute classes
│   └── ConventionalCommitSyntaxs/ # Commit message lexer/parser/diagnostics
├── Assets/
│   ├── conventional-commits-rules.json
│   └── conventional-commits-rules.zh-Hans.json
└── Resources/
    ├── GitButtonStyle.xaml        # ImageButton control template
    └── Icons.xaml                 # DrawingImage vector icons

GitPlus.Tests/                     # xunit + Moq test project
```

### Build

```powershell
dotnet build GitPlus.slnx
```

### Run Tests

```powershell
dotnet test GitPlus.slnx
```

### Debug (F5)

Open the solution in Visual Studio, set `GitPlus` as the startup project, and press **F5** — this launches the VS Experimental Instance with the extension loaded.

---

## 🧪 Testing

The `GitPlus.Tests` project uses **xunit** + **Moq**, targeting `net472` with WPF assembly references for `DependencyObject`-based tests.

---

## 📄 License

This project is licensed under the MIT License — see [LICENSE.txt](https://github.com/View12138/GitPlus/blob/main/LICENSE.txt) for details.

---

<p align="center">
  <sub>Made with ❤️ by <strong>View</strong></sub>
</p>