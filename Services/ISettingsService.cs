using System;
using System.Threading.Tasks;
using TaskNote.Models;

namespace TaskNote.Services
{
    public interface ISettingsService
    {
        AppSettings CurrentSettings { get; }
        event EventHandler<AppSettings>? SettingsChanged;
        Task SaveSettingsAsync(AppSettings settings);
    }
}
