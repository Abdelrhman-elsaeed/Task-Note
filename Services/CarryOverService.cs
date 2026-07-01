using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaskNote.Data;
using TaskNote.Models;

namespace TaskNote.Services
{
    public class CarryOverService
    {
        private static readonly Regex LegacyDateSuffixRegex =
            new(@"\s*\(\d{4}-\d{2}-\d{2}\)");

        private static string StripLegacyDateSuffix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return LegacyDateSuffixRegex.Replace(name, "").Trim();
        }

        private readonly IProjectRepository _projectRepository;
        private readonly IRepository<Column> _columnRepository;
        private readonly IRepository<TaskItem> _taskRepository;
        private readonly IDialogService _dialogService;

        public CarryOverService(
            IProjectRepository projectRepository,
            IRepository<Column> columnRepository,
            IRepository<TaskItem> taskRepository,
            IDialogService dialogService)
        {
            _projectRepository = projectRepository;
            _columnRepository = columnRepository;
            _taskRepository = taskRepository;
            _dialogService = dialogService;
        }

        public event EventHandler? ProjectsChanged;

        public async Task CheckAndCarryOverAsync()
        {
            try
            {
                var allProjects = await _projectRepository.GetProjectsWithDetailsAsync();

                var pastProjects = allProjects
                    .Where(p => p.TargetDate.Date < DateTime.Today && !p.IsCarryOverProcessed)
                    .ToList();

                if (!pastProjects.Any()) return;

                var carryOverItems = new List<CarryOverTaskItem>();
                foreach (var p in pastProjects)
                {
                    var lastCol = p.Columns.OrderBy(c => c.OrderIndex).LastOrDefault();
                    var unfinished = p.Columns
                        .Where(c => lastCol == null || c.Id != lastCol.Id)
                        .SelectMany(c => c.Tasks)
                        .ToList();

                    foreach (var t in unfinished)
                    {
                        carryOverItems.Add(new CarryOverTaskItem
                        {
                            Id = t.Id,
                            Name = t.Name,
                            ProjectName = p.Name,
                            IsSelected = true
                        });
                    }
                }

                if (!carryOverItems.Any())
                {
                    foreach (var p in pastProjects)
                    {
                        p.IsCarryOverProcessed = true;
                        await _projectRepository.UpdateAsync(p);
                    }
                    return;
                }

                var selectedIds = await _dialogService.ShowCarryOverDialogAsync("Carry Over Tasks", carryOverItems);

                foreach (var p in pastProjects)
                {
                    p.IsCarryOverProcessed = true;
                    await _projectRepository.UpdateAsync(p);
                }

                if (selectedIds != null && selectedIds.Any())
                {
                    var selectedIdSet = new HashSet<int>(selectedIds);

                    foreach (var oldProject in pastProjects)
                    {
                        var lastCol = oldProject.Columns.OrderBy(c => c.OrderIndex).LastOrDefault();
                        var oldUnfinished = oldProject.Columns
                            .Where(c => lastCol == null || c.Id != lastCol.Id)
                            .SelectMany(c => c.Tasks)
                            .Where(t => selectedIdSet.Contains(t.Id))
                            .ToList();

                        if (!oldUnfinished.Any()) continue;

                        string baseName = StripLegacyDateSuffix(oldProject.Name);
                        if (string.IsNullOrEmpty(baseName)) baseName = "New Project";
                        string newName = baseName;

                        var newProject = new Project
                        {
                            Name = newName,
                            TargetDate = DateTime.Today,
                            FolderId = oldProject.FolderId,
                            OrderIndex = oldProject.OrderIndex + 1
                        };

                        await _projectRepository.AddAsync(newProject);

                        var oldToNewColMap = new Dictionary<int, Column>();
                        foreach (var oldCol in oldProject.Columns.OrderBy(c => c.OrderIndex))
                        {
                            var newCol = new Column
                            {
                                Name = oldCol.Name,
                                ColorHex = oldCol.ColorHex,
                                OrderIndex = oldCol.OrderIndex,
                                ProjectId = newProject.Id
                            };
                            await _columnRepository.AddAsync(newCol);
                            oldToNewColMap[oldCol.Id] = newCol;
                        }

                        var loadedNewProject = await _projectRepository.GetProjectWithDetailsAsync(newProject.Id);
                        if (loadedNewProject != null)
                        {
                            var targetCol = loadedNewProject.Columns.FirstOrDefault(c => string.Equals(c.Name, "To Do", StringComparison.OrdinalIgnoreCase))
                                            ?? loadedNewProject.Columns.OrderBy(c => c.OrderIndex).FirstOrDefault();

                            if (targetCol != null)
                            {
                                int orderIndex = 0;
                                foreach (var oldTask in oldUnfinished)
                                {
                                    oldTask.ColumnId = targetCol.Id;
                                    oldTask.OrderIndex = orderIndex++;
                                    oldTask.TaskDate = DateTime.Today;
                                    await _taskRepository.UpdateAsync(oldTask);
                                }
                            }
                        }
                    }

                    ProjectsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error checking or executing carry-over tasks");
            }
        }
    }
}
