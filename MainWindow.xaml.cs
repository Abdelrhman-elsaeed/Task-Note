using System.Windows;
using System.Windows.Input;
using TaskNote.Models;
using TaskNote.ViewModels;

namespace TaskNote
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        /// <summary>
        /// Allows dragging the window from any non-interactive area (the sidebar header, etc.).
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only drag if clicking directly on non-interactive background
            if (e.OriginalSource is System.Windows.Controls.Border or Window)
            {
                try { DragMove(); }
                catch { /* ignore if mouse already released */ }
            }
        }

        /// <summary>
        /// Handles single-click on a project row to select it in the ViewModel.
        /// Double-click activates rename mode (sets IsFocused = true).
        /// </summary>
        private void ProjectRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border &&
                border.DataContext is Project project &&
                DataContext is MainViewModel vm)
            {
                if (e.ClickCount == 1)
                {
                    vm.SelectedProject = project;
                    vm.SelectedSidebarItem = project;
                    e.Handled = true;
                }
                else if (e.ClickCount == 2)
                {
                    project.IsFocused = true;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handles double-click on a folder row to activate rename mode.
        /// </summary>
        private void FolderRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && 
                sender is System.Windows.Controls.Border border &&
                border.DataContext is Folder folder)
            {
                folder.IsFocused = true;
                e.Handled = true;
            }
        }
    }
}