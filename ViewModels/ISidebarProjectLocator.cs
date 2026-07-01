using TaskNote.Models;

namespace TaskNote.ViewModels
{
    public interface ISidebarProjectLocator
    {
        Project? FindById(int projectId);
    }
}
