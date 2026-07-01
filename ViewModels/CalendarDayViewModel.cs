using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace TaskNote.ViewModels
{
    public class CalendarDayViewModel : ObservableObject
    {
        public DateTime Date { get; set; }
        
        public int DayNumber => Date.Day;

        private bool _isCurrentMonth;
        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set => SetProperty(ref _isCurrentMonth, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private bool _isToday;
        public bool IsToday
        {
            get => _isToday;
            set => SetProperty(ref _isToday, value);
        }

        private int _minutesStudied;
        public int MinutesStudied
        {
            get => _minutesStudied;
            set
            {
                if (SetProperty(ref _minutesStudied, value))
                {
                    OnPropertyChanged(nameof(HasStudyTime));
                }
            }
        }

        public bool HasStudyTime => MinutesStudied > 0;

        private int _completedTasksCount;
        public int CompletedTasksCount
        {
            get => _completedTasksCount;
            set
            {
                if (SetProperty(ref _completedTasksCount, value))
                {
                    OnPropertyChanged(nameof(HasCompletedTasks));
                }
            }
        }

        public bool HasCompletedTasks => CompletedTasksCount > 0;
    }
}
