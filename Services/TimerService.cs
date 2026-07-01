using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using TaskNote.Data;
using TaskNote.Models;

namespace TaskNote.Services
{
    public class TimerService : ITimerService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<TimerService> _logger;
        private readonly DispatcherTimer _timer;
        private DateTime _targetEndTime;
        private TimeSpan _timeRemaining;
        private TimeSpan _totalDuration;
        private TimeSpan _pausedTimeRemaining;
        private bool _isRunning;
        private bool _isPaused;

        public bool IsRunning => _isRunning && !_isPaused;
        public TimeSpan TimeRemaining => _timeRemaining;
        public TimeSpan TotalDuration => _totalDuration;

        public event EventHandler<TimeSpan>? Tick;
        public event EventHandler? TimerStarted;
        public event EventHandler? TimerFinished;
        public event EventHandler? TimerStopped;

        public TimerService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<TimerService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += Timer_Tick;
        }

        public void Start(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero) return;

            _logger.LogInformation("Starting focus timer with duration {Duration}", duration);
            _totalDuration = duration;
            _timeRemaining = duration;
            _targetEndTime = DateTime.Now + duration;
            _isRunning = true;
            _isPaused = false;

            _timer.Start();
            TimerStarted?.Invoke(this, EventArgs.Empty);
            Tick?.Invoke(this, _timeRemaining);
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _logger.LogInformation("Stopping focus timer manually");
            _timer.Stop();
            _isRunning = false;
            _isPaused = false;
            _timeRemaining = TimeSpan.Zero;

            TimerStopped?.Invoke(this, EventArgs.Empty);
            Tick?.Invoke(this, TimeSpan.Zero);
        }

        public void Pause()
        {
            if (!_isRunning || _isPaused) return;

            _logger.LogInformation("Pausing focus timer");
            _timer.Stop();
            _isPaused = true;
            _pausedTimeRemaining = _targetEndTime - DateTime.Now;
            if (_pausedTimeRemaining < TimeSpan.Zero) _pausedTimeRemaining = TimeSpan.Zero;
        }

        public void Resume()
        {
            if (!_isRunning || !_isPaused) return;

            _logger.LogInformation("Resuming focus timer");
            _targetEndTime = DateTime.Now + _pausedTimeRemaining;
            _isPaused = false;
            _timer.Start();
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_isRunning || _isPaused) return;

            var remaining = _targetEndTime - DateTime.Now;

            if (remaining <= TimeSpan.Zero)
            {
                _logger.LogInformation("Focus timer completed successfully");
                _timer.Stop();
                _isRunning = false;
                _timeRemaining = TimeSpan.Zero;
                Tick?.Invoke(this, TimeSpan.Zero);
                
                TimerFinished?.Invoke(this, EventArgs.Empty);

                await SaveSessionToHistoryAsync(_totalDuration);
            }
            else
            {
                _timeRemaining = remaining;
                Tick?.Invoke(this, _timeRemaining);
            }
        }

        private async Task SaveSessionToHistoryAsync(TimeSpan duration)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var historyItem = new TimerHistoryItem
                {
                    Date = DateTime.Today,
                    DurationSeconds = (int)duration.TotalSeconds
                };
                await context.TimerHistory.AddAsync(historyItem);
                await context.SaveChangesAsync();
                _logger.LogInformation("Focus session logged in database. Duration: {Duration}", duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save timer history to database");
            }
        }

        public async Task<TimeSpan> GetTotalFocusedTimeTodayAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var today = DateTime.Today;
                var totalSeconds = await context.TimerHistory
                    .Where(h => h.Date == today)
                    .SumAsync(h => h.DurationSeconds);

                return TimeSpan.FromSeconds(totalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load timer history from database");
                return TimeSpan.Zero;
            }
        }
    }
}
