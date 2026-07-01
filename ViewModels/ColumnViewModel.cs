using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TaskNote.Models;

namespace TaskNote.ViewModels
{
    public partial class ColumnViewModel : ObservableObject
    {
        private readonly BoardViewModel _boardViewModel;
        
        public Column Model { get; }

        public int Id => Model.Id;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _colorHex;

        [ObservableProperty]
        private ObservableCollection<TaskViewModel> _tasks = new();

        [ObservableProperty]
        private bool _isFocused;

        public ColumnViewModel(Column model, BoardViewModel boardViewModel)
        {
            Model = model;
            _boardViewModel = boardViewModel;
            _name = model.Name;
            _colorHex = model.ColorHex;
        }

        [RelayCommand]
        private async Task AddTaskAsync()
        {
            await _boardViewModel.AddTaskAsync(this);
        }

        [RelayCommand]
        private async Task RenameAsync()
        {
            await _boardViewModel.RenameColumnAsync(this);
        }

        [RelayCommand]
        private async Task ChangeColorAsync()
        {
            await _boardViewModel.ChangeColumnColorAsync(this);
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            await _boardViewModel.DeleteColumnAsync(this);
        }

        partial void OnNameChanged(string value)
        {
            if (Model.Name != value)
            {
                Model.Name = value;
                _boardViewModel.QueueColumnSave(Model);
            }
        }

        partial void OnColorHexChanged(string value)
        {
            if (Model.ColorHex != value)
            {
                Model.ColorHex = value;
                _boardViewModel.QueueColumnSave(Model);
            }
        }
    }
}
