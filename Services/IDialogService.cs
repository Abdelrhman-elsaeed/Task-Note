using System.Collections.Generic;
using System.Threading.Tasks;
using TaskNote.Models;

namespace TaskNote.Services
{
    public interface IDialogService
    {
        Task<string?> ShowInputDialogAsync(string title, string message, string defaultText = "");
        Task<bool> ShowConfirmDialogAsync(string title, string message);
        Task<string?> ShowColorPickerDialogAsync(string title, string currentColorHex = "");
        Task<string?> ShowOpenFileDialogAsync(string filter, string initialPath = "");
        Task<string?> ShowSaveFileDialogAsync(string filter, string initialPath = "");
        Task<List<int>?> ShowCarryOverDialogAsync(string title, List<CarryOverTaskItem> tasks);
    }
}
