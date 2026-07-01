using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TaskNote.Data;
using TaskNote.Models;
using TaskNote.Services;
using GongSolutions.Wpf.DragDrop;
using System.Windows;

namespace TaskNote.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDropTarget, ISidebarProjectLocator
    {
        // Strip legacy date suffix " (yyyy-MM-dd)" from project names so the name and date are stored separately.
        private static readonly System.Text.RegularExpressions.Regex LegacyDateSuffixRegex =
            new(@"\s*\(\d{4}-\d{2}-\d{2}\)");

        private static string StripLegacyDateSuffix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return LegacyDateSuffixRegex.Replace(name, "").Trim();
        }
        private readonly IProjectRepository _projectRepository;
        private readonly IRepository<Column> _columnRepository;
        private readonly IRepository<Folder> _folderRepository;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;
        private readonly IRepository<TaskItem> _taskRepository;
        private readonly CarryOverService _carryOverService;

        [ObservableProperty]
        private CalendarViewModel _calendarViewModel;

        [ObservableProperty]
        private bool _isCalendarActive;

        [ObservableProperty]
        private ObservableCollection<object> _sidebarItems = new();

        // Filtered view — rebuilt whenever SearchQuery changes
        [ObservableProperty]
        private ObservableCollection<object> _filteredSidebarItems = new();

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                    RebuildFilteredItems();
            }
        }

        [ObservableProperty]
        private Project? _selectedProject;

        [ObservableProperty]
        private BoardViewModel _boardViewModel;

        [ObservableProperty]
        private TimerViewModel _timerViewModel;

        [ObservableProperty]
        private SettingsViewModel _settingsViewModel;

        [ObservableProperty]
        private bool _isSettingsVisible;

        [ObservableProperty]
        private bool _isSidebarCollapsed;

        [ObservableProperty]
        private bool _isDarkMode;

        private object? _selectedSidebarItem;
        public object? SelectedSidebarItem
        {
            get => _selectedSidebarItem;
            set
            {
                if (SetProperty(ref _selectedSidebarItem, value))
                {
                    if (value is Project project)
                    {
                        SelectedProject = project;
                        IsCalendarActive = false;
                    }
                }
            }
        }

        public Project? FindById(int projectId)
        {
            return SidebarItems.OfType<Project>()
                       .FirstOrDefault(p => p.Id == projectId)
                   ?? SidebarItems.OfType<Folder>()
                       .SelectMany(f => f.Projects)
                       .FirstOrDefault(p => p.Id == projectId);
        }

        private void RebuildFilteredItems()
        {
            FilteredSidebarItems.Clear();
            var q = SearchQuery?.Trim();
            foreach (var item in SidebarItems)
            {
                if (string.IsNullOrEmpty(q))
                {
                    FilteredSidebarItems.Add(item);
                }
                else if (item is Folder folder)
                {
                    // Show folder if it or any child project matches
                    bool folderMatch = folder.Name.Contains(q, StringComparison.OrdinalIgnoreCase);
                    bool hasMatchingProject = folder.Projects.Any(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
                    if (folderMatch || hasMatchingProject)
                        FilteredSidebarItems.Add(folder);
                }
                else if (item is Project project)
                {
                    if (project.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                        FilteredSidebarItems.Add(project);
                }
            }
        }

        public MainViewModel(
            IProjectRepository projectRepository,
            IRepository<Column> columnRepository,
            IRepository<Folder> folderRepository,
            IDialogService dialogService,
            ISettingsService settingsService,
            IRepository<TaskItem> taskRepository,
            CarryOverService carryOverService,
            CalendarViewModel calendarViewModel,
            BoardViewModel boardViewModel,
            TimerViewModel timerViewModel,
            SettingsViewModel settingsViewModel)
        {
            _projectRepository = projectRepository;
            _columnRepository = columnRepository;
            _folderRepository = folderRepository;
            _dialogService = dialogService;
            _settingsService = settingsService;
            _taskRepository = taskRepository;
            _carryOverService = carryOverService;
            _calendarViewModel = calendarViewModel;
            
            _boardViewModel = boardViewModel;
            _timerViewModel = timerViewModel;
            _settingsViewModel = settingsViewModel;

            _settingsService.SettingsChanged += OnSettingsChanged;
            _carryOverService.ProjectsChanged += OnCarryOverProjectsChanged;

            // Wire live-refresh: when the board or timer changes data, the calendar
            // (currently selected day + day grid badges) must reload automatically.
            _boardViewModel.DataChanged += OnBoardOrTimerDataChanged;
            _timerViewModel.DataChanged += OnBoardOrTimerDataChanged;
        }

        private async void OnBoardOrTimerDataChanged(object? sender, EventArgs e)
        {
            try
            {
                if (CalendarViewModel == null) return;
                // DataChanged can fire from a background thread (e.g. QueueTaskSave),
                // so marshal the calendar refresh onto the UI dispatcher.
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    await dispatcher.InvokeAsync(() => CalendarViewModel.RefreshAsync());
                }
                else
                {
                    await CalendarViewModel.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to refresh calendar after data change");
            }
        }

        private async void OnSettingsChanged(object? sender, AppSettings e)
        {
            try
            {
                if (System.Windows.Application.Current != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await InitializeAsync();
                    });
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error handling settings change in MainViewModel");
            }
        }

        private async void OnCarryOverProjectsChanged(object? sender, EventArgs e)
        {
            try
            {
                await LoadSidebarItemsAsync();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error refreshing sidebar after carry-over");
            }
        }

        public async Task InitializeAsync()
        {
            await LoadSidebarItemsAsync();
            await TimerViewModel.LoadHistoryAsync();
            await SettingsViewModel.InitializeAsync();
            IsDarkMode = _settingsService.CurrentSettings.Theme == "Dark";
            ThemeHelper.ApplyTheme(_settingsService.CurrentSettings.Theme);
            await _carryOverService.CheckAndCarryOverAsync();
        }

        [RelayCommand]
        private void ToggleFolderExpand(Folder? folder)
        {
            if (folder != null)
                folder.IsExpanded = !folder.IsExpanded;
        }

        [RelayCommand]
        private async Task LoadSidebarItemsAsync()
        {
            int? selectedProjectId = SelectedProject?.Id;

            var foldersList = await _projectRepository.GetFoldersWithDetailsAsync();
            var rootProjectsList = await _projectRepository.GetRootProjectsWithDetailsAsync();

            SidebarItems.Clear();

            // One-time migration: strip legacy "(yyyy-MM-dd)" suffix from project names
            // that were generated before date-as-metadata was introduced.
            await MigrateLegacyProjectNamesAsync(foldersList, rootProjectsList);

            // Add root projects first (so the default project sits at the top of the sidebar)
            foreach (var project in rootProjectsList)
            {
                project.PropertyChanged += OnProjectPropertyChanged;
                SidebarItems.Add(project);
            }

            // Then folders and their children
            foreach (var folder in foldersList)
            {
                folder.PropertyChanged += OnFolderPropertyChanged;
                foreach (var project in folder.Projects)
                {
                    project.PropertyChanged += OnProjectPropertyChanged;
                }
                SidebarItems.Add(folder);
            }

            // Restore selection or select default
            Project? toSelect = null;
            if (selectedProjectId.HasValue)
            {
                toSelect = rootProjectsList.FirstOrDefault(p => p.Id == selectedProjectId.Value)
                           ?? foldersList.SelectMany(f => f.Projects).FirstOrDefault(p => p.Id == selectedProjectId.Value);
            }

            // Rebuild filter after loading
            RebuildFilteredItems();

            if (toSelect != null)
            {
                SelectedProject = toSelect;
            }
            else
            {
                var firstProject = rootProjectsList.FirstOrDefault()
                                   ?? foldersList.SelectMany(f => f.Projects).FirstOrDefault();

                if (firstProject != null)
                {
                    SelectedProject = firstProject;
                }
                // No projects yet — start with an empty sidebar; the user can create
                // a project or folder using the toolbar buttons.
            }
        }

        private async Task MigrateLegacyProjectNamesAsync(
            List<Folder> folders,
            List<Project> rootProjects)
        {
            foreach (var project in folders.SelectMany(f => f.Projects).Concat(rootProjects))
            {
                if (string.IsNullOrEmpty(project.Name)) continue;
                var cleaned = StripLegacyDateSuffix(project.Name);
                if (!string.Equals(cleaned, project.Name, StringComparison.Ordinal))
                {
                    project.Name = cleaned;
                    try
                    {
                        await _projectRepository.UpdateAsync(project);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Failed to migrate legacy project name for project {ProjectId}", project.Id);
                    }
                }
            }
        }

        partial void OnSelectedProjectChanged(Project? value)
        {
            if (value != null)
            {
                BoardViewModel.LoadProject(value.Id);
            }
            else
            {
                BoardViewModel.ClearBoard();
            }
        }

        private async void OnFolderPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Folder.Name) && sender is Folder folder)
            {
                if (!string.IsNullOrWhiteSpace(folder.Name))
                {
                    try
                    {
                        await _folderRepository.UpdateAsync(folder);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Failed to update folder name");
                    }
                }
            }
        }

        private async void OnProjectPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Project.Name) && sender is Project project)
            {
                if (!string.IsNullOrWhiteSpace(project.Name))
                {
                    try
                    {
                        await _projectRepository.UpdateAsync(project);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Failed to update project name");
                    }
                }
            }
        }

        [RelayCommand]
        private async Task AddFolderAsync()
        {
            var maxOrder = SidebarItems.OfType<Folder>().Any() ? SidebarItems.OfType<Folder>().Max(f => f.OrderIndex) : -1;
            var newFolder = new Folder
            {
                Name = "New Folder",
                OrderIndex = maxOrder + 1,
                IsFocused = true
            };

            await _folderRepository.AddAsync(newFolder);
            await LoadSidebarItemsAsync();

            var match = SidebarItems.OfType<Folder>().FirstOrDefault(f => f.Id == newFolder.Id);
            if (match != null)
            {
                match.IsFocused = true;
            }
        }

        [RelayCommand]
        private async Task DeleteFolderAsync(Folder? folder)
        {
            if (folder == null) return;

            bool confirm = await _dialogService.ShowConfirmDialogAsync(
                "Delete Folder", 
                $"Are you sure you want to delete folder '{folder.Name}' and all its projects?");
            if (!confirm) return;

            // Delete child projects to handle existing database structures where cascade delete isn't configured in database schema
            foreach (var project in folder.Projects.ToList())
            {
                await _projectRepository.DeleteAsync(project);
            }

            await _folderRepository.DeleteAsync(folder);
            await LoadSidebarItemsAsync();
        }

        [RelayCommand]
        private async Task AddProjectAsync()
        {
            var maxOrder = SidebarItems.OfType<Project>().Any() ? SidebarItems.OfType<Project>().Max(p => p.OrderIndex) : -1;
            var newProject = new Project
            {
                Name = "New Project",
                OrderIndex = maxOrder + 1,
                IsFocused = true,
                TargetDate = DateTime.Today
            };

            await _projectRepository.AddAsync(newProject);

            var c1 = new Column { Name = "To Do", OrderIndex = 0, ProjectId = newProject.Id, ColorHex = "#F0EFEA" };
            var c2 = new Column { Name = "In Progress", OrderIndex = 1, ProjectId = newProject.Id, ColorHex = "#FAF2EB" };
            var c3 = new Column { Name = "Done", OrderIndex = 2, ProjectId = newProject.Id, ColorHex = "#EBF6F0" };
            
            await _columnRepository.AddAsync(c1);
            await _columnRepository.AddAsync(c2);
            await _columnRepository.AddAsync(c3);

            await LoadSidebarItemsAsync();

            var loaded = SidebarItems.OfType<Project>().FirstOrDefault(p => p.Id == newProject.Id) 
                         ?? SidebarItems.OfType<Folder>().SelectMany(f => f.Projects).FirstOrDefault(p => p.Id == newProject.Id);
            
            if (loaded != null)
            {
                SelectedProject = loaded;
                loaded.IsFocused = true;
            }
        }

        [RelayCommand]
        private async Task AddProjectToFolderAsync(Folder? folder)
        {
            if (folder == null) return;

            var maxOrder = folder.Projects.Any() ? folder.Projects.Max(p => p.OrderIndex) : -1;
            var newProject = new Project
            {
                Name = "New Project",
                FolderId = folder.Id,
                OrderIndex = maxOrder + 1,
                IsFocused = true,
                TargetDate = DateTime.Today
            };

            await _projectRepository.AddAsync(newProject);

            var c1 = new Column { Name = "To Do", OrderIndex = 0, ProjectId = newProject.Id, ColorHex = "#F0EFEA" };
            var c2 = new Column { Name = "In Progress", OrderIndex = 1, ProjectId = newProject.Id, ColorHex = "#FAF2EB" };
            var c3 = new Column { Name = "Done", OrderIndex = 2, ProjectId = newProject.Id, ColorHex = "#EBF6F0" };
            
            await _columnRepository.AddAsync(c1);
            await _columnRepository.AddAsync(c2);
            await _columnRepository.AddAsync(c3);

            await LoadSidebarItemsAsync();

            var loadedFolder = SidebarItems.OfType<Folder>().FirstOrDefault(f => f.Id == folder.Id);
            if (loadedFolder != null)
            {
                var loadedProj = loadedFolder.Projects.FirstOrDefault(p => p.Id == newProject.Id);
                if (loadedProj != null)
                {
                    SelectedProject = loadedProj;
                    loadedProj.IsFocused = true;
                }
            }
        }

        [RelayCommand]
        private async Task AddProjectSiblingAsync(Project? project)
        {
            if (project == null) return;
            if (project.FolderId.HasValue)
            {
                var folder = SidebarItems.OfType<Folder>().FirstOrDefault(f => f.Id == project.FolderId.Value);
                if (folder != null)
                {
                    await AddProjectToFolderAsync(folder);
                }
            }
            else
            {
                await AddProjectAsync();
            }
        }

        [RelayCommand]
        private async Task ToggleThemeAsync()
        {
            var settings = _settingsService.CurrentSettings;
            settings.Theme = (settings.Theme == "Dark") ? "Light" : "Dark";
            await _settingsService.SaveSettingsAsync(settings);
        }

        [RelayCommand]
        private async Task DeleteProjectAsync(Project? project)
        {
            if (project == null) return;

            bool confirm = await _dialogService.ShowConfirmDialogAsync(
                "Delete Project", 
                $"Delete project '{project.Name}' and all its tasks?");
            if (!confirm) return;

            await _projectRepository.DeleteAsync(project);
            
            if (SelectedProject == project)
            {
                SelectedProject = null;
            }

            await LoadSidebarItemsAsync();
        }

        [RelayCommand]
        private void RenameFolder(Folder? folder)
        {
            if (folder != null)
            {
                folder.IsFocused = true;
            }
        }

        [RelayCommand]
        private void RenameProject(Project? project)
        {
            if (project != null)
            {
                project.IsFocused = true;
            }
        }

        [RelayCommand]
        private void ToggleSettings()
        {
            IsSettingsVisible = !IsSettingsVisible;
        }

        [RelayCommand]
        private void ToggleSidebar()
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        // Drag and Drop (Gong) IDropTarget implementation for tree reordering
        public void DragOver(IDropInfo dropInfo)
        {
            var sourceItem = dropInfo.Data;
            var targetItem = dropInfo.TargetItem;

            if (sourceItem is Folder)
            {
                if (targetItem == null || targetItem is Folder || (targetItem is Project p && p.FolderId == null))
                {
                    dropInfo.Effects = DragDropEffects.Move;
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                }
            }
            else if (sourceItem is Project)
            {
                dropInfo.Effects = DragDropEffects.Move;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            }
        }

        public async void Drop(IDropInfo dropInfo)
        {
            try
            {
                var draggedItem = dropInfo.Data;
                var targetItem = dropInfo.TargetItem;
                
                if (draggedItem is Folder draggedFolder)
                {
                    int insertIndex = dropInfo.InsertIndex;
                    var foldersOnly = SidebarItems.OfType<Folder>().ToList();
                    var oldIndex = foldersOnly.IndexOf(draggedFolder);
                    if (oldIndex >= 0)
                    {
                        foldersOnly.RemoveAt(oldIndex);
                        if (insertIndex > foldersOnly.Count) insertIndex = foldersOnly.Count;
                        foldersOnly.Insert(insertIndex, draggedFolder);
                        
                        for (int i = 0; i < foldersOnly.Count; i++)
                        {
                            foldersOnly[i].OrderIndex = i;
                        }
                        await _projectRepository.UpdateFolderPositionsAsync(foldersOnly);
                    }
                    
                    await LoadSidebarItemsAsync();
                }
                else if (draggedItem is Project draggedProject)
                {
                    if (targetItem is Folder targetFolder)
                    {
                        draggedProject.FolderId = targetFolder.Id;
                        draggedProject.Folder = targetFolder;
                        
                        var folderProjects = targetFolder.Projects.OrderBy(p => p.OrderIndex).ToList();
                        folderProjects.Remove(draggedProject);
                        
                        int insertIndex = dropInfo.InsertIndex;
                        if (insertIndex > folderProjects.Count) insertIndex = folderProjects.Count;
                        folderProjects.Insert(insertIndex, draggedProject);
                        
                        for (int i = 0; i < folderProjects.Count; i++)
                        {
                            folderProjects[i].OrderIndex = i;
                            folderProjects[i].FolderId = targetFolder.Id;
                            await _projectRepository.UpdateAsync(folderProjects[i]);
                        }
                    }
                    else if (targetItem is Project targetProject)
                    {
                        if (targetProject.FolderId != null)
                        {
                            draggedProject.FolderId = targetProject.FolderId;
                            var folder = targetProject.Folder;
                            if (folder != null)
                            {
                                var folderProjects = folder.Projects.OrderBy(p => p.OrderIndex).ToList();
                                folderProjects.Remove(draggedProject);
                                int insertIndex = folderProjects.IndexOf(targetProject);
                                if (insertIndex >= 0)
                                {
                                    folderProjects.Insert(insertIndex, draggedProject);
                                }
                                else
                                {
                                    folderProjects.Add(draggedProject);
                                }
                                
                                for (int i = 0; i < folderProjects.Count; i++)
                                {
                                    folderProjects[i].OrderIndex = i;
                                    folderProjects[i].FolderId = folder.Id;
                                    await _projectRepository.UpdateAsync(folderProjects[i]);
                                }
                            }
                        }
                        else
                        {
                            draggedProject.FolderId = null;
                            draggedProject.Folder = null;
                            
                            var rootProjects = await _projectRepository.GetRootProjectsWithDetailsAsync();
                            rootProjects = rootProjects.Where(p => p.Id != draggedProject.Id).ToList();
                            int insertIndex = rootProjects.FindIndex(p => p.Id == targetProject.Id);
                            if (insertIndex >= 0)
                            {
                                rootProjects.Insert(insertIndex, draggedProject);
                            }
                            else
                            {
                                rootProjects.Add(draggedProject);
                            }
                            
                            for (int i = 0; i < rootProjects.Count; i++)
                            {
                                rootProjects[i].OrderIndex = i;
                                rootProjects[i].FolderId = null;
                                await _projectRepository.UpdateAsync(rootProjects[i]);
                            }
                        }
                    }
                    else
                    {
                        draggedProject.FolderId = null;
                        draggedProject.Folder = null;
                        draggedProject.OrderIndex = SidebarItems.OfType<Project>().Count();
                        await _projectRepository.UpdateAsync(draggedProject);
                    }
                    
                    await LoadSidebarItemsAsync();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error handling sidebar drag-and-drop drop");
            }
        }

        [RelayCommand]
        private void ShowCalendar()
        {
            IsCalendarActive = true;
            SelectedProject = null;
            SelectedSidebarItem = null;
            _ = CalendarViewModel.LoadDateDetailsAsync(CalendarViewModel.SelectedDate);
        }
    }
}
