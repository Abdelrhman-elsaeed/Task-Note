using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TaskNote.ViewModels;

namespace TaskNote.Views
{
    public partial class BoardView : UserControl
    {
        public BoardView()
        {
            InitializeComponent();
        }

        private void ProjectNameDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2 && DataContext is BoardViewModel vm && vm.HasProject)
            {
                vm.StartRenamingProjectCommand.Execute(null);
                e.Handled = true;
            }
        }

        private async void ProjectNameEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (DataContext is not BoardViewModel vm) return;

            if (e.Key == Key.Enter)
            {
                await vm.CommitProjectNameAsync(tb.Text);
                Keyboard.ClearFocus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                vm.IsProjectNameEditing = false;
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        private async void ProjectNameEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && DataContext is BoardViewModel vm)
            {
                if (vm.IsProjectNameEditing)
                {
                    await vm.CommitProjectNameAsync(tb.Text);
                }
            }
        }

        private void ProjectNameEdit_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.Visibility == Visibility.Visible)
            {
                tb.Focus();
                tb.SelectAll();
            }
        }
    }
}
