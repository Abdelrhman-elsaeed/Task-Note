using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace TaskNote.Models
{
    public class Folder : ObservableObject
    {
        public int Id { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _orderIndex;
        public int OrderIndex
        {
            get => _orderIndex;
            set => SetProperty(ref _orderIndex, value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        private bool _isFocused;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsFocused
        {
            get => _isFocused;
            set => SetProperty(ref _isFocused, value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        private bool _isExpanded = false;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
