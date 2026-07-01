using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace TaskNote.Models
{
    public class TaskItem : ObservableObject
    {
        public int Id { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _estimatedTime = string.Empty;
        public string EstimatedTime
        {
            get => _estimatedTime;
            set => SetProperty(ref _estimatedTime, value);
        }

        private int _orderIndex;
        public int OrderIndex
        {
            get => _orderIndex;
            set => SetProperty(ref _orderIndex, value);
        }

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private DateTime _taskDate = DateTime.Today;
        public DateTime TaskDate
        {
            get => _taskDate;
            set => SetProperty(ref _taskDate, value);
        }

        public int ColumnId { get; set; }
        public virtual Column Column { get; set; } = null!;
    }
}
