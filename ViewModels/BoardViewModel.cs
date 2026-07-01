using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TaskNote.Data;
using TaskNote.Models;
using TaskNote.Services;

namespace TaskNote.ViewModels
{
    public partial class BoardViewModel : ObservableObject, IDropTarget, IDragSource
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IRepository<Column> _columnRepository;
        private readonly IRepository<TaskItem> _taskRepository;
        private readonly IDialogService _dialogService;
        private readonly ISidebarProjectLocator _sidebarProjectLocator;
        private readonly ILogger<BoardViewModel> _logger;

        private int _currentProjectId;

        [ObservableProperty]
        private string _projectName = string.Empty;

        [ObservableProperty]
        private DateTime _projectTargetDate = DateTime.Today;

        [ObservableProperty]
        private bool _hasProject;

        [ObservableProperty]
        private bool _isProjectNameEditing;

        [ObservableProperty]
        private ObservableCollection<ColumnViewModel> _columns = new();

        public BoardViewModel(
            IProjectRepository projectRepository,
            IRepository<Column> columnRepository,
            IRepository<TaskItem> taskRepository,
            IDialogService dialogService,
            ISidebarProjectLocator sidebarProjectLocator,
            ILogger<BoardViewModel> logger)
        {
            _projectRepository = projectRepository;
            _columnRepository = columnRepository;
            _taskRepository = taskRepository;
            _dialogService = dialogService;
            _sidebarProjectLocator = sidebarProjectLocator;
            _logger = logger;
        }

        /// <summary>
        /// Fires whenever tasks or columns are added, moved, edited, or deleted so that
        /// other views (Calendar, statistics) can refresh without a manual reload.
        /// </summary>
        public event EventHandler? DataChanged;

        public void RaiseDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);

        public async void LoadProject(int projectId)
        {
            try
            {
                _logger.LogInformation("Loading board project with ID {ProjectId}", projectId);
                _currentProjectId = projectId;
                var project = await _projectRepository.GetProjectWithDetailsAsync(projectId);

                Columns.Clear();
                if (project != null)
                {
                    ProjectName = project.Name;
                    ProjectTargetDate = project.TargetDate;
                    HasProject = true;
                    foreach (var col in project.Columns)
                    {
                        var colVm = new ColumnViewModel(col, this);
                        foreach (var task in col.Tasks)
                        {
                            colVm.Tasks.Add(new TaskViewModel(task, this));
                        }
                        Columns.Add(colVm);
                    }
                }
                else
                {
                    ClearBoard();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project {ProjectId}", projectId);
            }
        }

        public void ClearBoard()
        {
            _logger.LogInformation("Clearing board (no project selected)");
            _currentProjectId = 0;
            Columns.Clear();
            ProjectName = string.Empty;
            ProjectTargetDate = DateTime.Today;
            HasProject = false;
            IsProjectNameEditing = false;
        }

        [RelayCommand]
        private void StartRenamingProject()
        {
            if (!HasProject) return;
            IsProjectNameEditing = true;
        }

        public async Task CommitProjectNameAsync(string? newName)
        {
            if (!HasProject || _currentProjectId == 0) return;
            var trimmed = newName?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) trimmed = "Untitled Project";
            if (trimmed == ProjectName)
            {
                IsProjectNameEditing = false;
                return;
            }
            try
            {
                // 1) Persist the new name to the database using a focused query.
                await _projectRepository.RenameProjectAsync(_currentProjectId, trimmed);

                // 2) Update the live Project instance that the sidebar is bound to so
                //    the TextBlock refreshes immediately via PropertyChanged.
                var liveProject = _sidebarProjectLocator.FindById(_currentProjectId);
                if (liveProject != null)
                {
                    liveProject.Name = trimmed;
                }

                // 3) Update the header's own property.
                ProjectName = trimmed;
                RaiseDataChanged();
                _logger.LogInformation("Renamed project {ProjectId} to '{Name}'", _currentProjectId, trimmed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rename project {ProjectId}", _currentProjectId);
            }
            finally
            {
                IsProjectNameEditing = false;
            }
        }

        [RelayCommand]
        private async Task AddColumnAsync()
        {
            if (_currentProjectId == 0) return;

            var maxOrder = Columns.Any() ? Columns.Max(c => c.Model.OrderIndex) : -1;
            var newCol = new Column
            {
                Name = "", // Empty name for immediate inline editing
                ColorHex = "#F0EFEA",
                ProjectId = _currentProjectId,
                OrderIndex = maxOrder + 1
            };

            await _columnRepository.AddAsync(newCol);
            var columnVm = new ColumnViewModel(newCol, this);
            Columns.Add(columnVm);
            columnVm.IsFocused = true;
            RaiseDataChanged();
            _logger.LogInformation("Added new column directly to project {ProjectId} for inline editing", _currentProjectId);
        }

        public async Task RenameColumnAsync(ColumnViewModel column)
        {
            var name = await _dialogService.ShowInputDialogAsync("Rename Column", "Enter new name:", column.Name);
            if (string.IsNullOrWhiteSpace(name) || name.Trim() == column.Name) return;

            _logger.LogInformation("Renaming column '{OldName}' to '{NewName}'", column.Name, name);
            column.Name = name.Trim();
        }

        public async Task ChangeColumnColorAsync(ColumnViewModel column)
        {
            var hex = await _dialogService.ShowColorPickerDialogAsync("Column Color", column.ColorHex);
            if (string.IsNullOrWhiteSpace(hex)) return;

            _logger.LogInformation("Changing color of column '{ColumnName}' to {Color}", column.Name, hex);
            column.ColorHex = hex;
        }

        public async Task DeleteColumnAsync(ColumnViewModel column)
        {
            bool confirm = await _dialogService.ShowConfirmDialogAsync(
                "Delete Column", 
                $"Are you sure you want to delete column '{column.Name}' and all its tasks?");
            if (!confirm) return;

            _logger.LogInformation("Deleting column '{ColumnName}' (ID {ColumnId})", column.Name, column.Id);
            await _columnRepository.DeleteAsync(column.Model);
            Columns.Remove(column);

            await UpdateColumnsOrderingAsync();
            RaiseDataChanged();
        }

        private async Task UpdateColumnsOrderingAsync()
        {
            var columnsToUpdate = new List<Column>();
            for (int i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                col.Model.OrderIndex = i;
                columnsToUpdate.Add(col.Model);
            }
            await _projectRepository.UpdateColumnPositionsAsync(columnsToUpdate);
        }

        public async Task AddTaskAsync(ColumnViewModel column)
        {
            var maxOrder = column.Tasks.Any() ? column.Tasks.Max(t => t.Model.OrderIndex) : -1;
            var newTask = new TaskItem
            {
                Name = "", // Empty initially for inline editing
                EstimatedTime = "1h 00m",
                ColumnId = column.Id,
                OrderIndex = maxOrder + 1,
                TaskDate = ProjectTargetDate
            };

            await _taskRepository.AddAsync(newTask);
            var taskVm = new TaskViewModel(newTask, this);
            column.Tasks.Add(taskVm);

            // Put it in focus/edit mode immediately
            taskVm.IsFocused = true;
            RaiseDataChanged();
            _logger.LogInformation("Added blank task directly inside column '{ColumnName}' for inline editing", column.Name);
        }

        public async Task RenameTaskAsync(TaskViewModel task)
        {
            var name = await _dialogService.ShowInputDialogAsync("Rename Task", "Enter new name:", task.Name);
            if (string.IsNullOrWhiteSpace(name) || name.Trim() == task.Name) return;

            _logger.LogInformation("Renaming task '{OldName}' to '{NewName}'", task.Name, name);
            task.Name = name.Trim();
        }

        public async Task DeleteTaskAsync(TaskViewModel task)
        {
            bool confirm = await _dialogService.ShowConfirmDialogAsync(
                "Delete Task", 
                $"Are you sure you want to delete task '{task.Name}'?");
            if (!confirm) return;

            var colVm = Columns.FirstOrDefault(c => c.Id == task.Model.ColumnId);
            if (colVm != null)
            {
                _logger.LogInformation("Deleting task '{TaskName}' (ID {TaskId}) from column '{ColumnName}'", task.Name, task.Id, colVm.Name);
                await _taskRepository.DeleteAsync(task.Model);
                colVm.Tasks.Remove(task);

                await UpdateTasksOrderingAsync(colVm);
                RaiseDataChanged();
            }
        }

        public void QueueTaskSave(TaskItem task)
        {
            Task.Run(async () =>
            {
                try
                {
                    await _taskRepository.UpdateAsync(task);
                    RaiseDataChanged();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to asynchronously save task (ID {TaskId})", task.Id);
                }
            });
        }

        public void QueueColumnSave(Column col)
        {
            Task.Run(async () =>
            {
                try
                {
                    await _columnRepository.UpdateAsync(col);
                    RaiseDataChanged();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to asynchronously save column (ID {ColumnId})", col.Id);
                }
            });
        }

        // Drag and Drop (Gong) IDropTarget implementation
        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is TaskViewModel draggedTask && dropInfo.TargetCollection is ObservableCollection<TaskViewModel> targetList)
            {
                var currentColumnOfTask = Columns.FirstOrDefault(c => c.Tasks.Contains(draggedTask));
                if (currentColumnOfTask != null)
                {
                    var currentList = currentColumnOfTask.Tasks;
                    int oldIndex = currentList.IndexOf(draggedTask);
                    int newIndex = dropInfo.InsertIndex;
                    
                    if (currentList == targetList)
                    {
                        if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
                        {
                            int targetIndex = newIndex;
                            if (targetIndex > oldIndex) targetIndex--;
                            if (oldIndex != targetIndex)
                            {
                                targetList.Move(oldIndex, targetIndex);
                            }
                        }
                    }
                    else
                    {
                        // Moving to a different column in real-time safely
                        currentList.Remove(draggedTask);
                        if (newIndex > targetList.Count) newIndex = targetList.Count;
                        targetList.Insert(newIndex, draggedTask);
                        
                        var targetColumnViewModel = Columns.FirstOrDefault(c => c.Tasks == targetList);
                        if (targetColumnViewModel != null)
                        {
                            draggedTask.Model.ColumnId = targetColumnViewModel.Id;
                        }
                    }
                }
                
                dropInfo.Effects = DragDropEffects.Move;
                dropInfo.DropTargetAdorner = null; // Hide horizontal insert line
            }
            else if (dropInfo.Data is ColumnViewModel draggedColumn && dropInfo.TargetCollection is ObservableCollection<ColumnViewModel> columnsCollection)
            {
                int oldIndex = columnsCollection.IndexOf(draggedColumn);
                int newIndex = dropInfo.InsertIndex;
                if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
                {
                    int targetIndex = newIndex;
                    if (targetIndex > oldIndex) targetIndex--;
                    
                    if (oldIndex != targetIndex)
                    {
                        columnsCollection.Move(oldIndex, targetIndex);
                    }
                }
                dropInfo.Effects = DragDropEffects.Move;
                dropInfo.DropTargetAdorner = null; // Hide horizontal insert line
            }
        }

        public async void Drop(IDropInfo dropInfo)
        {
            try
            {
                if (dropInfo.Data is TaskViewModel)
                {
                    // Persist final task order in all columns to database
                    foreach (var col in Columns)
                    {
                        await UpdateTasksOrderingAsync(col);
                    }
                    RaiseDataChanged();
                }
                else if (dropInfo.Data is ColumnViewModel)
                {
                    // Persist final column order to database
                    await UpdateColumnsOrderingAsync();
                    RaiseDataChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting order after drop operation.");
            }
        }

        private async Task UpdateTasksOrderingAsync(ColumnViewModel columnVm)
        {
            var tasksToUpdate = new List<TaskItem>();
            for (int i = 0; i < columnVm.Tasks.Count; i++)
            {
                var taskVm = columnVm.Tasks[i];
                taskVm.Model.OrderIndex = i;
                taskVm.Model.ColumnId = columnVm.Id;
                tasksToUpdate.Add(taskVm.Model);
            }
            await _projectRepository.UpdateTaskPositionsAsync(tasksToUpdate);
        }

        // ─── IDragSource ──────────────────────────────────────────────────
        public void StartDrag(IDragInfo dragInfo)
        {
            // Let gong handle the default logic — it sets dragInfo.Data
            GongSolutions.Wpf.DragDrop.DragDrop.DefaultDragHandler.StartDrag(dragInfo);
        }

        public bool CanStartDrag(IDragInfo dragInfo)
        {
            // Allow dragging ColumnViewModel from anywhere in the column header area
            // but NOT from inside a TextBox (so inline editing still works)
            if (dragInfo.SourceItem is ColumnViewModel || dragInfo.SourceItem is TaskViewModel)
                return true;
            return GongSolutions.Wpf.DragDrop.DragDrop.DefaultDragHandler.CanStartDrag(dragInfo);
        }

        public void Dropped(IDropInfo dropInfo)
        {
            // Nothing extra needed — Drop() handles persistence
        }

        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
        {
            // No-op
        }

        public void DragCancelled()
        {
            // No-op
        }

        public bool TryCatchOccurredException(Exception exception) => false;
    }
}
