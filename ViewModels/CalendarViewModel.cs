using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TaskNote.Data;
using TaskNote.Models;

namespace TaskNote.ViewModels
{
    public class CalendarViewModel : ObservableObject
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IRepository<TimerHistoryItem> _timerHistoryRepository;

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    _ = LoadDateDetailsAsync(value);
                    UpdateSelectedDayHighlight();
                }
            }
        }

        private DateTime _currentMonthStart;
        public DateTime CurrentMonthStart
        {
            get => _currentMonthStart;
            set
            {
                if (SetProperty(ref _currentMonthStart, value))
                {
                    OnPropertyChanged(nameof(CurrentMonthYearName));
                    _ = RebuildCalendarAsync();
                }
            }
        }

        public string CurrentMonthYearName => CurrentMonthStart.ToString("MMMM yyyy");

        private ObservableCollection<CalendarDayViewModel> _calendarDays = new();
        public ObservableCollection<CalendarDayViewModel> CalendarDays
        {
            get => _calendarDays;
            set => SetProperty(ref _calendarDays, value);
        }

        private ObservableCollection<TaskItem> _completedTasks = new();
        public ObservableCollection<TaskItem> CompletedTasks
        {
            get => _completedTasks;
            set => SetProperty(ref _completedTasks, value);
        }

        private int _minutesStudied;
        public int MinutesStudied
        {
            get => _minutesStudied;
            set => SetProperty(ref _minutesStudied, value);
        }

        public IRelayCommand PreviousMonthCommand { get; }
        public IRelayCommand NextMonthCommand { get; }
        public IRelayCommand<CalendarDayViewModel> SelectDayCommand { get; }

        public CalendarViewModel(
            IProjectRepository projectRepository,
            IRepository<TimerHistoryItem> timerHistoryRepository)
        {
            _projectRepository = projectRepository;
            _timerHistoryRepository = timerHistoryRepository;

            PreviousMonthCommand = new RelayCommand(GoToPreviousMonth);
            NextMonthCommand = new RelayCommand(GoToNextMonth);
            SelectDayCommand = new RelayCommand<CalendarDayViewModel>(SelectDay);

            // Initialize to current month
            _currentMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            // Initial load
            _ = RebuildCalendarAsync();
            _ = LoadDateDetailsAsync(DateTime.Today);
        }

        /// <summary>
        /// Reloads both the day-cell grid (badges for study time and completed tasks)
        /// and the currently selected day-details panel. Safe to call from any thread.
        /// </summary>
        public async Task RefreshAsync()
        {
            await RebuildCalendarAsync();
            await LoadDateDetailsAsync(SelectedDate);
        }

        private void GoToPreviousMonth()
        {
            CurrentMonthStart = CurrentMonthStart.AddMonths(-1);
        }

        private void GoToNextMonth()
        {
            CurrentMonthStart = CurrentMonthStart.AddMonths(1);
        }

        private void SelectDay(CalendarDayViewModel? day)
        {
            if (day != null)
            {
                SelectedDate = day.Date;
            }
        }

        private void UpdateSelectedDayHighlight()
        {
            foreach (var day in CalendarDays)
            {
                day.IsSelected = day.Date.Date == SelectedDate.Date;
            }
        }

        public async Task RebuildCalendarAsync()
        {
            try
            {
                // 1. Calculate 42 days (6 weeks)
                var firstDayOfWeek = CurrentMonthStart.DayOfWeek;
                int offset = (int)firstDayOfWeek; // 0 for Sunday, 1 for Monday, etc.
                var startDate = CurrentMonthStart.AddDays(-offset);

                var datesList = new List<DateTime>();
                for (int i = 0; i < 42; i++)
                {
                    datesList.Add(startDate.AddDays(i));
                }

                // 2. Fetch stats in batch
                var histories = await _timerHistoryRepository.GetAllAsync();
                var studyTimeByDate = histories
                    .GroupBy(h => h.Date.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(h => h.DurationSeconds));

                var projects = await _projectRepository.GetProjectsWithDetailsAsync();
                var completedTasksByDate = projects
                    .SelectMany(p =>
                    {
                        var lastCol = p.Columns.OrderBy(c => c.OrderIndex).LastOrDefault();
                        if (lastCol == null) return Array.Empty<TaskItem>();
                        return lastCol.Tasks;
                    })
                    .GroupBy(t => t.TaskDate.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 3. Build day ViewModels
                var daysList = new List<CalendarDayViewModel>();
                foreach (var date in datesList)
                {
                    studyTimeByDate.TryGetValue(date.Date, out int seconds);
                    completedTasksByDate.TryGetValue(date.Date, out int tasksCount);

                    daysList.Add(new CalendarDayViewModel
                    {
                        Date = date,
                        IsCurrentMonth = date.Month == CurrentMonthStart.Month,
                        IsSelected = date.Date == SelectedDate.Date,
                        IsToday = date.Date == DateTime.Today,
                        MinutesStudied = (int)Math.Round((double)seconds / 60),
                        CompletedTasksCount = tasksCount
                    });
                }

                // Update on UI thread
                CalendarDays.Clear();
                foreach (var d in daysList)
                {
                    CalendarDays.Add(d);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error rebuilding calendar");
            }
        }

        public async Task LoadDateDetailsAsync(DateTime date)
        {
            try
            {
                // Fetch minutes studied
                var histories = await _timerHistoryRepository.FindAsync(h => h.Date.Date == date.Date);
                var totalSeconds = histories.Sum(h => h.DurationSeconds);
                MinutesStudied = (int)Math.Round((double)totalSeconds / 60);

                // Fetch completed tasks
                var projects = await _projectRepository.GetProjectsWithDetailsAsync();

                CompletedTasks.Clear();
                foreach (var p in projects)
                {
                    var lastCol = p.Columns.OrderBy(c => c.OrderIndex).LastOrDefault();
                    if (lastCol == null) continue;
                    foreach (var t in lastCol.Tasks)
                    {
                        if (t.TaskDate.Date == date.Date)
                            CompletedTasks.Add(t);
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error loading calendar details for date {Date}", date);
            }
        }
    }
}
