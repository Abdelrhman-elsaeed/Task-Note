using System.Collections.Generic;
using System.Threading.Tasks;
using TaskNote.Models;

namespace TaskNote.Data
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<Project?> GetProjectWithDetailsAsync(int projectId);
        Task<List<Project>> GetProjectsWithDetailsAsync();
        Task SaveProjectStructureAsync(Project project);
        Task UpdateTaskPositionsAsync(List<TaskItem> tasks);
        Task UpdateColumnPositionsAsync(List<Column> columns);
        Task<List<Folder>> GetFoldersWithDetailsAsync();
        Task<List<Project>> GetRootProjectsWithDetailsAsync();
        Task UpdateFolderPositionsAsync(List<Folder> folders);
        Task RenameProjectAsync(int projectId, string newName);
    }
}
