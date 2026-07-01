using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace TaskNote.Models
{
    public class Project : ObservableObject
    {
        public int Id { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int? FolderId { get; set; }
        public virtual Folder? Folder { get; set; }

        private int _orderIndex;
        public int OrderIndex
        {
            get => _orderIndex;
            set => SetProperty(ref _orderIndex, value);
        }

        private DateTime _targetDate = DateTime.Today;
        public DateTime TargetDate
        {
            get => _targetDate;
            set => SetProperty(ref _targetDate, value);
        }

        private bool _isCarryOverProcessed;
        public bool IsCarryOverProcessed
        {
            get => _isCarryOverProcessed;
            set => SetProperty(ref _isCarryOverProcessed, value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        private bool _isFocused;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsFocused
        {
            get => _isFocused;
            set => SetProperty(ref _isFocused, value);
        }

        public virtual ICollection<Column> Columns { get; set; } = new List<Column>();
    }
}
