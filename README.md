# TaskNote

> A focused, offline-first desktop task manager that turns your day into a calm, draggable board — with a built-in focus timer and a calendar that remembers what you actually did.

<p align="left">
  <img src="Resources/logo.png" alt="TaskNote logo" width="120" />
</p>

[![Platform](https://img.shields.io/badge/platform-Windows-0078D4)](https://dotnet.microsoft.com/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-1A7BC1)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/license-Proprietary-lightgrey)](#license)

---

## What is TaskNote?

TaskNote is a single-user, locally-installed Windows app for organizing work into **Projects → Boards → Columns → Tasks**, with a **focus timer** (Pomodoro-style) and a **calendar view** that tracks what you finished and how long you focused.

It runs entirely on your machine. Your data lives in a local SQLite file under `%AppData%\TaskNote`. No account. No cloud. No telemetry.

### Highlights

- **Kanban boards** — unlimited projects, each with a 3-column default (To Do / In Progress / Done) that you can extend, rename, recolor, and reorder.
- **Folder sidebar** — group projects into collapsible folders. Drag-and-drop to move projects between folders or to the root.
- **Focus timer** — built-in pomodoro with start/finish sounds, pause/resume, and a "time focused today" stat. Each completed session is logged so the calendar can show your minutes.
- **Calendar** — month grid with study-time and completed-task badges per day, plus a day-details panel listing what you finished.
- **Carry-over** — on launch, past-dated projects offer to move their unfinished tasks into a fresh project dated today.
- **Inline editing** — single-click selects, double-click renames anywhere a label appears.
- **Light / Dark theme** — Wpf.Ui + custom palette, persisted per user.
- **Search** — live filter across folders and projects in the sidebar.
- **Backup-friendly** — settings dialog lets you point the database at a new path; the old file is copied, not moved.

---

## Screenshots

> Add screenshots under `docs/screenshots/` and reference them here. The board, the calendar, and the timer are the three views worth showing.

---

## Tech Stack

| Layer | Choice | Why |
|---|---|---|
| UI | **WPF** on .NET 8 (`net8.0-windows`) | Native Windows, XAML databinding, deep control over the look. |
| MVVM | **CommunityToolkit.Mvvm 8.2** | Source-generated `[ObservableProperty]` and `[RelayCommand]`. |
| UI library | **WPF-UI 3.0** | Modern Fluent-style controls + theme manager. |
| Drag-and-drop | **gong-wpf-dragdrop 3.1** | Tree + collection reordering. |
| Persistence | **EF Core 8 + SQLite** | Local file DB, zero-config, plays nicely with `IDbContextFactory`. |
| Behaviors | **Microsoft.Xaml.Behaviors.Wpf 1.1** | `FocusBehavior` for auto-focus on inline edit. |
| Hosting | **Microsoft.Extensions.Hosting 8** | DI composition root, options, configuration. |
| Logging | **Serilog** (file sink, rolling daily) | One-line file logger, easy to inspect. |
| Installer | **Inno Setup** (`installer.iss`) | Single-EXE installer, no admin required. |

---

## Architecture

TaskNote is a **layered MVVM** desktop app. The dependency direction is one-way: `Views → ViewModels → Services → (Repositories | External)`. The data layer is the only code that knows about EF Core / SQLite; services hold the business workflows free of I/O detail; view-models shape the UI.

```
┌─────────────────────────────────────────────────────────┐
│  Views  (XAML)  —  BoardView, CalendarView, TimerView,  │
│                   SettingsView, MainWindow              │
└──────────────────────────┬──────────────────────────────┘
                           │ data binding / commands
┌──────────────────────────▼──────────────────────────────┐
│  ViewModels  —  MainViewModel, BoardViewModel,          │
│                 CalendarViewModel, TimerViewModel,       │
│                 SettingsViewModel, Column/Task VMs,     │
│                 ISidebarProjectLocator                  │
└──────────────────────────┬──────────────────────────────┘
                           │ depends on interfaces
┌──────────────────────────▼──────────────────────────────┐
│  Services  —  CarryOverService, ProjectService,         │
│               TimerService, AudioService,               │
│               DialogService, SettingsService,           │
│               ThemeHelper (UI-bound helper)             │
└──────────────────────────┬──────────────────────────────┘
                           │ depends on interfaces
┌──────────────────────────▼──────────────────────────────┐
│  Data  —  AppDbContext, IRepository<T>, IProject-      │
│           Repository, Repository<T>, ProjectRepository  │
└──────────────────────────┬──────────────────────────────┘
                           │
                          EF Core ──► SQLite (local file)
                           │
┌──────────────────────────▼──────────────────────────────┐
│  Models  —  Project, Folder, Column, TaskItem,          │
│             TimerHistoryItem, AppSettings,              │
│             CarryOverTaskItem  (DTOs + entities)        │
└─────────────────────────────────────────────────────────┘
```

**The composition root** is `App.xaml.cs`. It builds the `IHost`, wires DI, and resolves `MainWindow` with its `MainViewModel`.

### Why this layout

- **Layering is the diagnostic.** Most bugs in this app historically have been a workflow sneaking into a view-model. Services own workflows; view-models own UI state.
- **No service-locator.** Every cross-VM reference goes through a small role interface (e.g. `ISidebarProjectLocator`) or through a service that raises an event the VM subscribes to.
- **EF stays in `Data/`.** Services depend on `IProjectRepository` and `IRepository<T>`, never on `DbContext` types — so unit tests can fake them.
- **One source of truth for data shapes.** `Models/` is the only place `Project`, `Column`, `TaskItem`, etc. are defined. DTOs (`CarryOverTaskItem`, `AppSettings`) live next to the entities because they're shapes, not behavior.

---

## Project Structure

```
TaskNote/
├── App.xaml / App.xaml.cs         # composition root, DI wiring, startup migrations
├── MainWindow.xaml / .cs          # shell window, drag-to-move, sidebar event handlers
├── TaskNote.csproj                # SDK-style project, all packages pinned
├── installer.iss                  # Inno Setup script (single-EXE installer)
│
├── Models/                        # entities + DTOs (one source of truth)
│   ├── AppSettings.cs
│   ├── CarryOverTaskItem.cs
│   ├── Column.cs
│   ├── Folder.cs
│   ├── Project.cs
│   ├── TaskItem.cs
│   └── TimerHistoryItem.cs
│
├── Data/                          # the only EF / SQLite code
│   ├── AppDbContext.cs            # + SqliteBusyTimeoutInterceptor
│   ├── IRepository.cs / Repository.cs
│   └── IProjectRepository.cs / ProjectRepository.cs
│
├── Services/                      # business workflows (verbs)
│   ├── AudioService.cs / IAudioService.cs
│   ├── CarryOverService.cs        # past-project → today carry-over
│   ├── DialogService.cs / IDialogService.cs
│   ├── ProjectService.cs          # project-shell creation (3 default columns)
│   ├── SettingsService.cs / ISettingsService.cs
│   └── TimerService.cs / ITimerService.cs
│
├── ViewModels/                    # presentation + UI state
│   ├── BoardViewModel.cs
│   ├── CalendarDayViewModel.cs
│   ├── CalendarViewModel.cs
│   ├── ColumnViewModel.cs
│   ├── ISidebarProjectLocator.cs  # breaks the MainVM ↔ BoardVM cycle
│   ├── MainViewModel.cs           # shell, sidebar tree, drag-drop, settings
│   ├── SettingsViewModel.cs
│   ├── TaskViewModel.cs
│   ├── ThemeHelper.cs             # static WPF theme switcher
│   └── TimerViewModel.cs
│
├── Views/                         # XAML user controls
│   ├── BoardView.xaml / .cs
│   ├── CalendarView.xaml / .cs
│   ├── SettingsView.xaml / .cs
│   └── TimerView.xaml / .cs
│
└── Resources/
    ├── LightTheme.xaml / DarkTheme.xaml
    ├── Styles.xaml
    ├── Converters.cs
    ├── FocusBehavior.cs           # auto-focus attached behavior
    ├── app_icon.ico / app_icon.png
    └── logo.png
```

---

## Getting Started

### Prerequisites

- **Windows 10/11** (WPF doesn't run on macOS/Linux)
- **.NET 8 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/8.0)
- *(Optional)* **Inno Setup 6** — only if you want to rebuild the installer

### Run from source

```bash
git clone https://github.com/Abdelrhman-elsaeed/TaskNote.git
cd TaskNote
dotnet restore
dotnet build -c Debug
dotnet run --project TaskNote.csproj
```

The first launch creates `%AppData%\TaskNote\` with:
- `appsettings.json` — user settings (theme, sounds, DB path, timer duration)
- `tasknote.db` — SQLite database
- `logs/log-YYYY-MM-DD.txt` — rolling Serilog file logs

### Publish a self-contained build

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -o publish
```

The output lands in `publish/TaskNote.exe`.

### Build the installer

1. Publish first (above) so `publish/` is populated.
2. Open `installer.iss` in **Inno Setup Compiler** and click **Build**.
3. The installer is written to `installer/TaskNote_Setup_v13.exe`.

---

## Configuration

All user settings live in `%AppData%\TaskNote\appsettings.json`:

```json
{
  "AppSettings": {
    "DatabasePath": "C:\\Users\\You\\AppData\\Roaming\\TaskNote\\tasknote.db",
    "TimerStartSoundPath": "",
    "TimerFinishSoundPath": "",
    "TimerDurationMinutes": 25,
    "Theme": "Light"
  }
}
```

- `DatabasePath` — change it to relocate your DB. When the path changes, the previous file is copied to the new location.
- `TimerStartSoundPath` / `TimerFinishSoundPath` — `.mp3` or `.wav` files. Leave empty to use the system sounds.
- `TimerDurationMinutes` — default pomodoro length, also picked up by the quick-set buttons (15/25/45).
- `Theme` — `"Light"` or `"Dark"`.

The settings file is hot-reloaded (`reloadOnChange: true`) and changes are persisted via `ISettingsService`.

---

## Data Model

```
Folder 1 ── * Project 1 ── * Column 1 ── * TaskItem
                                              │
                                              └── TaskDate (date the work targets)

TimerHistoryItem      (Id, Date, DurationSeconds)  ← per-session log
AppSettings           (DBSettings + Theme + Sounds + Timer defaults)
CarryOverTaskItem     (Id, Name, ProjectName, IsSelected)  ← DTO for the carry-over dialog
```

Cascade deletes are configured in `AppDbContext.OnModelCreating`:
- Deleting a **Folder** removes its Projects.
- Deleting a **Project** removes its Columns.
- Deleting a **Column** removes its Tasks.

---

## Keyboard & Mouse

| Action | Result |
|---|---|
| **Click** a project / folder | Selects it |
| **Double-click** a project / folder | Enters inline rename |
| **Drag** a task | Moves it within or across columns; final position persists on drop |
| **Drag** a column | Reorders columns in the current project |
| **Drag** a project | Moves it between folders / to root, with persisted ordering |
| **Drag** the window header | Moves the borderless window |

---

## Logging

Logs are written to `%AppData%\TaskNote\logs\log-YYYY-MM-DD.txt` via Serilog, rolling daily, at `Debug` level. Includes:
- DI host startup / shutdown
- Schema migration events
- Focus-timer start/pause/finish events
- Repository errors (never fatal — surfaced to the user via dialogs)

---

## Build & Test

There is no automated test suite yet. The main verification is the build:

```bash
dotnet build TaskNote.csproj -c Debug
```

Should produce `0 Warning(s), 0 Error(s)`. Run the app, exercise: create a project → add columns/tasks → drag tasks → start the timer → switch to the calendar.

---

## Roadmap

- [ ] Encapsulate `IsFocused` / `IsExpanded` view flags off EF models (move into wrapper view-models).
- [ ] Break `MainViewModel` into `SidebarViewModel` (tree + filter + drag-drop) + `ShellViewModel` (settings/theme/init).
- [ ] Break `BoardViewModel` into `BoardViewModel` + `BoardDragController`.
- [ ] Replace `ALTER TABLE` startup migrations with a proper `DbContext` initializer.
- [ ] Replace `AppDbContext → ISettingsService` upward dependency with `DbContextOptions<T>` wired at startup.
- [ ] Unit tests for `CarryOverService` and `ProjectService` (pure logic, no UI).

---

## License

Proprietary. © Antigravity. All rights reserved.

---

## Credits

Built with WPF, .NET 8, EF Core, CommunityToolkit.Mvvm, WPF-UI, and gong-wpf-dragdrop.
