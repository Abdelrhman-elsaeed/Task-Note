using System;
using System.Threading.Tasks;

namespace TaskNote.Services
{
    public interface ITimerService
    {
        bool IsRunning { get; }
        TimeSpan TimeRemaining { get; }
        TimeSpan TotalDuration { get; }
        event EventHandler<TimeSpan>? Tick;
        event EventHandler? TimerStarted;
        event EventHandler? TimerFinished;
        event EventHandler? TimerStopped;
        
        void Start(TimeSpan duration);
        void Stop();
        void Pause();
        void Resume();
        Task<TimeSpan> GetTotalFocusedTimeTodayAsync();
    }
}
