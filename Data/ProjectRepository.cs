using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskNote.Models;

namespace TaskNote.Data
{
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(IDbContextFactory<AppDbContext> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<Project?> GetProjectWithDetailsAsync(int projectId)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            var project = await context.Projects
                .Include(p => p.Columns)
                .ThenInclude(c => c.Tasks)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project != null)
            {
                project.Columns = project.Columns
                    .OrderBy(c => c.OrderIndex)
                    .Select(c => {
                        c.Tasks = c.Tasks.OrderBy(t => t.OrderIndex).ToList();
                        return c;
                    })
                    .ToList();
            }

            return project;
        }

        public async Task<List<Project>> GetProjectsWithDetailsAsync()
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            var projects = await context.Projects
                .Include(p => p.Columns)
                .ThenInclude(c => c.Tasks)
                .AsNoTracking()
                .ToListAsync();

            foreach (var project in projects)
            {
                project.Columns = project.Columns
                    .OrderBy(c => c.OrderIndex)
                    .Select(c => {
                        c.Tasks = c.Tasks.OrderBy(t => t.OrderIndex).ToList();
                        return c;
                    })
                    .ToList();
            }

            return projects;
        }

        public async Task SaveProjectStructureAsync(Project project)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            context.Projects.Update(project);
            await context.SaveChangesAsync();
        }

        public async Task UpdateTaskPositionsAsync(List<TaskItem> tasks)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            foreach (var task in tasks)
            {
                var existing = await context.Tasks.FindAsync(task.Id);
                if (existing != null)
                {
                    existing.ColumnId = task.ColumnId;
                    existing.OrderIndex = task.OrderIndex;
                }
            }
            await context.SaveChangesAsync();
        }

        public async Task UpdateColumnPositionsAsync(List<Column> columns)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            foreach (var col in columns)
            {
                var existing = await context.Columns.FindAsync(col.Id);
                if (existing != null)
                {
                    existing.OrderIndex = col.OrderIndex;
                }
            }
            await context.SaveChangesAsync();
        }

        public async Task<List<Folder>> GetFoldersWithDetailsAsync()
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            var folders = await context.Folders
                .Include(f => f.Projects)
                .ThenInclude(p => p.Columns)
                .ThenInclude(c => c.Tasks)
                .AsNoTracking()
                .ToListAsync();

            foreach (var folder in folders)
            {
                folder.Projects = folder.Projects
                    .OrderBy(p => p.OrderIndex)
                    .Select(p => {
                        p.Columns = p.Columns
                            .OrderBy(c => c.OrderIndex)
                            .Select(c => {
                                c.Tasks = c.Tasks.OrderBy(t => t.OrderIndex).ToList();
                                return c;
                            })
                            .ToList();
                        return p;
                    })
                    .ToList();
            }

            return folders.OrderBy(f => f.OrderIndex).ToList();
        }

        public async Task<List<Project>> GetRootProjectsWithDetailsAsync()
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            var projects = await context.Projects
                .Include(p => p.Columns)
                .ThenInclude(c => c.Tasks)
                .Where(p => p.FolderId == null)
                .AsNoTracking()
                .ToListAsync();

            foreach (var p in projects)
            {
                p.Columns = p.Columns
                    .OrderBy(c => c.OrderIndex)
                    .Select(c => {
                        c.Tasks = c.Tasks.OrderBy(t => t.OrderIndex).ToList();
                        return c;
                    })
                    .ToList();
            }

            return projects.OrderBy(p => p.OrderIndex).ToList();
        }

        public async Task UpdateFolderPositionsAsync(List<Folder> folders)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            foreach (var f in folders)
            {
                var existing = await context.Folders.FindAsync(f.Id);
                if (existing != null)
                {
                    existing.OrderIndex = f.OrderIndex;
                }
            }
            await context.SaveChangesAsync();
        }

        public async Task RenameProjectAsync(int projectId, string newName)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            var existing = await context.Projects.FindAsync(projectId);
            if (existing == null) return;
            existing.Name = newName;
            await context.SaveChangesAsync();
        }
    }
}
