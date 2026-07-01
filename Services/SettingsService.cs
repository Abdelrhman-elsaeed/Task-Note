using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TaskNote.Models;

namespace TaskNote.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IOptionsMonitor<AppSettings> _optionsMonitor;
        private readonly string _settingsFilePath;

        public AppSettings CurrentSettings => _optionsMonitor.CurrentValue;

        public event EventHandler<AppSettings>? SettingsChanged;

        public SettingsService(IOptionsMonitor<AppSettings> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
            
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskNote");
            _settingsFilePath = Path.Combine(appDataFolder, "appsettings.json");

            _optionsMonitor.OnChange(settings =>
            {
                SettingsChanged?.Invoke(this, settings);
            });
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                JsonObject root;
                if (File.Exists(_settingsFilePath))
                {
                    var jsonText = await File.ReadAllTextAsync(_settingsFilePath);
                    root = JsonSerializer.Deserialize<JsonObject>(jsonText) ?? new JsonObject();
                }
                else
                {
                    root = new JsonObject();
                }

                var settingsNode = JsonSerializer.SerializeToNode(settings);
                root["AppSettings"] = settingsNode;

                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = JsonSerializer.Serialize(root, options);
                
                await File.WriteAllTextAsync(_settingsFilePath, updatedJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
