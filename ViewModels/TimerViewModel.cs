using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using TaskNote.Services;

namespace TaskNote.ViewModels
{
    public partial class TimerViewModel : ObservableObject
    {
        private readonly ITimerService _timerService;
        private readonly IAudioService _audioService;
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        private string _timeString = "25:00";

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private bool _isPaused;

        [ObservableProperty]
        private string _todayFocusedText = "Time focused today: 0m";

        [ObservableProperty]
        private int _inputMinutes = 25;

        public TimerViewModel(
            ITimerService timerService,
            IAudioService audioService,
            ISettingsService settingsService)
        {
            _timerService = timerService;
            _audioService = audioService;
            _settingsService = settingsService;

            _timerService.Tick += Timer_Tick;
            _timerService.TimerStarted += Timer_Started;
            _timerService.TimerFinished += Timer_Finished;
            _timerService.TimerStopped += Timer_Stopped;

            _inputMinutes = _settingsService.CurrentSettings.TimerDurationMinutes;
            if (_inputMinutes <= 0) _inputMinutes = 25;
            SetTimeStringFromMinutes(_inputMinutes);
        }

        /// <summary>
        /// Fires whenever a study session is logged (timer finishes or stops) so that
        /// other views (Calendar, statistics) can refresh without a manual reload.
        /// </summary>
        public event EventHandler? DataChanged;

        public void RaiseDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);

        public async Task LoadHistoryAsync()
        {
            var focusedToday = await _timerService.GetTotalFocusedTimeTodayAsync();
            
            string timeString;
            if (focusedToday.TotalHours >= 1)
            {
                timeString = $"{(int)focusedToday.TotalHours}h {focusedToday.Minutes}m";
            }
            else
            {
                timeString = $"{focusedToday.Minutes}m";
            }

            TodayFocusedText = $"Time focused today: {timeString}";
        }

        private void SetTimeStringFromMinutes(int mins)
        {
            TimeString = $"{mins:D2}:00";
            Progress = 100;
        }

        private void Timer_Tick(object? sender, TimeSpan remaining)
        {
            TimeString = $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
            
            var total = _timerService.TotalDuration;
            if (total > TimeSpan.Zero)
            {
                Progress = (remaining.TotalSeconds / total.TotalSeconds) * 100;
            }
            else
            {
                Progress = 0;
            }
        }

        private async void Timer_Started(object? sender, EventArgs e)
        {
            IsRunning = true;
            IsPaused = false;
            await _audioService.PlayStartSoundAsync();
        }

        private async void Timer_Finished(object? sender, EventArgs e)
        {
            IsRunning = false;
            IsPaused = false;
            Progress = 0;
            TimeString = "00:00";

            await _audioService.PlayFinishSoundAsync();
            await LoadHistoryAsync();
            RaiseDataChanged();
        }

        private async void Timer_Stopped(object? sender, EventArgs e)
        {
            IsRunning = false;
            IsPaused = false;
            SetTimeStringFromMinutes(InputMinutes);
            await _audioService.StopSoundsAsync();
            RaiseDataChanged();
        }

        [RelayCommand]
        private void StartTimer()
        {
            if (IsRunning) return;

            if (InputMinutes > 0)
            {
                var settings = _settingsService.CurrentSettings;
                if (settings.TimerDurationMinutes != InputMinutes)
                {
                    settings.TimerDurationMinutes = InputMinutes;
                    _settingsService.SaveSettingsAsync(settings).ConfigureAwait(false);
                }
            }

            _timerService.Start(TimeSpan.FromMinutes(InputMinutes));
        }

        [RelayCommand]
        private void StopTimer()
        {
            _timerService.Stop();
        }

        [RelayCommand]
        private void TogglePause()
        {
            if (!IsRunning) return;

            if (IsPaused)
            {
                _timerService.Resume();
                IsPaused = false;
            }
            else
            {
                _timerService.Pause();
                IsPaused = true;
            }
        }

        [RelayCommand]
        private void QuickSetDuration(int minutes)
        {
            if (IsRunning) return;
            InputMinutes = minutes;
            SetTimeStringFromMinutes(minutes);
        }
    }
}
