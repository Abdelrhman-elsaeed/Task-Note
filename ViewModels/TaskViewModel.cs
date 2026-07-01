using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using TaskNote.Models;

namespace TaskNote.ViewModels
{
    public partial class TaskViewModel : ObservableObject
    {
        private readonly BoardViewModel _boardViewModel;
        
        public TaskItem Model { get; }

        public int Id => Model.Id;
        
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _estimatedTime;

        [ObservableProperty]
        private string _notes;

        [ObservableProperty]
        private bool _isNotesExpanded;

        [ObservableProperty]
        private bool _isFocused;

        public TaskViewModel(TaskItem model, BoardViewModel boardViewModel)
        {
            Model = model;
            _boardViewModel = boardViewModel;
            _name = model.Name;
            _estimatedTime = model.EstimatedTime;
            _notes = model.Notes ?? string.Empty;
        }

        [RelayCommand]
        private void ToggleNotes()
        {
            IsNotesExpanded = !IsNotesExpanded;
        }

        [RelayCommand]
        private async Task EditNameAsync()
        {
            await _boardViewModel.RenameTaskAsync(this);
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            await _boardViewModel.DeleteTaskAsync(this);
        }

        partial void OnNameChanged(string value)
        {
            if (Model.Name != value)
            {
                Model.Name = value;
                _boardViewModel.QueueTaskSave(Model);
            }
        }

        partial void OnEstimatedTimeChanged(string value)
        {
            if (Model.EstimatedTime != value)
            {
                Model.EstimatedTime = value;
                _boardViewModel.QueueTaskSave(Model);
            }
        }

        partial void OnNotesChanged(string value)
        {
            if (Model.Notes != value)
            {
                Model.Notes = value;
                _boardViewModel.QueueTaskSave(Model);
            }
        }
    }
}
