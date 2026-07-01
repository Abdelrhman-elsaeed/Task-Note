using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Threading.Tasks;
using TaskNote.Models;
using TaskNote.Services;

namespace TaskNote.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _databasePath = string.Empty;

        [ObservableProperty]
        private string _startSoundPath = string.Empty;

        [ObservableProperty]
        private string _finishSoundPath = string.Empty;

        [ObservableProperty]
        private bool _isDarkMode;

        public SettingsViewModel(ISettingsService settingsService, IDialogService dialogService)
        {
            _settingsService = settingsService;
            _dialogService = dialogService;
        }

        public Task InitializeAsync()
        {
            var settings = _settingsService.CurrentSettings;
            DatabasePath = settings.DatabasePath;
            StartSoundPath = settings.TimerStartSoundPath;
            FinishSoundPath = settings.TimerFinishSoundPath;
            IsDarkMode = settings.Theme == "Dark";
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task BrowseDatabasePathAsync()
        {
            var result = await _dialogService.ShowSaveFileDialogAsync("SQLite Database (*.db)|*.db", DatabasePath);
            if (result != null)
            {
                var oldPath = DatabasePath;
                DatabasePath = result;

                if (!string.IsNullOrEmpty(oldPath) && oldPath != result && File.Exists(oldPath))
                {
                    try
                    {
                        File.Copy(oldPath, result, true);
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to copy database: {ex.Message}");
                    }
                }

                await SaveSettingsAsync();
            }
        }

        [RelayCommand]
        private async Task BrowseStartSoundAsync()
        {
            var result = await _dialogService.ShowOpenFileDialogAsync("Audio Files (*.mp3;*.wav)|*.mp3;*.wav", StartSoundPath);
            if (result != null)
            {
                StartSoundPath = result;
                await SaveSettingsAsync();
            }
        }

        [RelayCommand]
        private async Task BrowseFinishSoundAsync()
        {
            var result = await _dialogService.ShowOpenFileDialogAsync("Audio Files (*.mp3;*.wav)|*.mp3;*.wav", FinishSoundPath);
            if (result != null)
            {
                FinishSoundPath = result;
                await SaveSettingsAsync();
            }
        }

        [RelayCommand]
        private async Task ClearStartSoundAsync()
        {
            StartSoundPath = string.Empty;
            await SaveSettingsAsync();
        }

        [RelayCommand]
        private async Task ClearFinishSoundAsync()
        {
            FinishSoundPath = string.Empty;
            await SaveSettingsAsync();
        }

        partial void OnIsDarkModeChanged(bool value)
        {
            var currentTheme = _settingsService.CurrentSettings.Theme;
            var expectedTheme = value ? "Dark" : "Light";
            if (currentTheme != expectedTheme)
            {
                _ = SaveSettingsAsync();
                ThemeHelper.ApplyTheme(expectedTheme);
            }
        }

        private async Task SaveSettingsAsync()
        {
            var settings = new AppSettings
            {
                DatabasePath = DatabasePath,
                TimerStartSoundPath = StartSoundPath,
                TimerFinishSoundPath = FinishSoundPath,
                TimerDurationMinutes = _settingsService.CurrentSettings.TimerDurationMinutes,
                Theme = IsDarkMode ? "Dark" : "Light"
            };

            await _settingsService.SaveSettingsAsync(settings);
        }
    }
}
