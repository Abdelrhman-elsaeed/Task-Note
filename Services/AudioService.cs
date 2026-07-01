using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using TaskNote.Services;

namespace TaskNote.Services
{
    public class AudioService : IAudioService
    {
        private readonly ISettingsService _settingsService;
        private MediaPlayer? _startPlayer;
        private MediaPlayer? _finishPlayer;

        public AudioService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public Task PlayStartSoundAsync()
        {
            if (App.Current != null)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        StopStartSoundInternal();
                        var customPath = _settingsService.CurrentSettings.TimerStartSoundPath;
                        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
                        {
                            _startPlayer = new MediaPlayer();
                            _startPlayer.Open(new Uri(customPath));
                            _startPlayer.Play();
                        }
                        else
                        {
                            System.Media.SystemSounds.Asterisk.Play();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AudioService play failed: {ex.Message}");
                        try
                        {
                            System.Media.SystemSounds.Asterisk.Play();
                        }
                        catch { }
                    }
                });
            }
            return Task.CompletedTask;
        }

        public Task PlayFinishSoundAsync()
        {
            if (App.Current != null)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        StopStartSoundInternal();
                        StopFinishSoundInternal();
                        var customPath = _settingsService.CurrentSettings.TimerFinishSoundPath;
                        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
                        {
                            _finishPlayer = new MediaPlayer();
                            _finishPlayer.Open(new Uri(customPath));
                            _finishPlayer.Play();
                        }
                        else
                        {
                            System.Media.SystemSounds.Exclamation.Play();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AudioService play failed: {ex.Message}");
                        try
                        {
                            System.Media.SystemSounds.Exclamation.Play();
                        }
                        catch { }
                    }
                });
            }
            return Task.CompletedTask;
        }

        public Task StopSoundsAsync()
        {
            if (App.Current != null)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    StopStartSoundInternal();
                    StopFinishSoundInternal();
                });
            }
            return Task.CompletedTask;
        }

        private void StopStartSoundInternal()
        {
            if (_startPlayer != null)
            {
                try
                {
                    _startPlayer.Stop();
                    _startPlayer.Close();
                }
                catch { }
                _startPlayer = null;
            }
        }

        private void StopFinishSoundInternal()
        {
            if (_finishPlayer != null)
            {
                try
                {
                    _finishPlayer.Stop();
                    _finishPlayer.Close();
                }
                catch { }
                _finishPlayer = null;
            }
        }
    }
}
