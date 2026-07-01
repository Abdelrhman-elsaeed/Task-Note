using System.Threading.Tasks;
using TaskNote.Data;
using TaskNote.Models;

namespace TaskNote.Services
{
    public class ProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IRepository<Column> _columnRepository;

        public ProjectService(
            IProjectRepository projectRepository,
            IRepository<Column> columnRepository)
        {
            _projectRepository = projectRepository;
            _columnRepository = columnRepository;
        }

        public async Task<int> CreateProjectShellAsync(int? folderId, int nextOrderIndex)
        {
            var newProject = new Project
            {
                Name = "New Project",
                FolderId = folderId,
                OrderIndex = nextOrderIndex,
                IsFocused = true,
                TargetDate = System.DateTime.Today
            };

            await _projectRepository.AddAsync(newProject);

            var c1 = new Column { Name = "To Do", OrderIndex = 0, ProjectId = newProject.Id, ColorHex = "#F0EFEA" };
            var c2 = new Column { Name = "In Progress", OrderIndex = 1, ProjectId = newProject.Id, ColorHex = "#FAF2EB" };
            var c3 = new Column { Name = "Done", OrderIndex = 2, ProjectId = newProject.Id, ColorHex = "#EBF6F0" };

            await _columnRepository.AddAsync(c1);
            await _columnRepository.AddAsync(c2);
            await _columnRepository.AddAsync(c3);

            return newProject.Id;
        }
    }
}
