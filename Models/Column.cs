using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace TaskNote.Models
{
    public class Column : ObservableObject
    {
        public int Id { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _colorHex = "#F0EFEA";
        public string ColorHex
        {
            get => _colorHex;
            set => SetProperty(ref _colorHex, value);
        }

        private int _orderIndex;
        public int OrderIndex
        {
            get => _orderIndex;
            set => SetProperty(ref _orderIndex, value);
        }
        
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; } = null!;
        
        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
