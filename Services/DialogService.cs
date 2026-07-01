using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaskNote.Models;

namespace TaskNote.Services
{
    public class DialogService : IDialogService
    {
        public Task<string?> ShowInputDialogAsync(string title, string message, string defaultText = "")
        {
            var tcs = new TaskCompletionSource<string?>();

            App.Current.Dispatcher.Invoke(() =>
            {
                var window = CreateBaseDialogWindow(title, 380, 200);

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                var messageText = new TextBlock
                {
                    Text = message,
                    Foreground = GetBrush("#2D2D2D"),
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 12),
                    TextWrapping = TextWrapping.Wrap
                };
                stackPanel.Children.Add(messageText);

                var textBox = new TextBox
                {
                    Text = defaultText,
                    Padding = new Thickness(8, 6, 8, 6),
                    Background = GetBrush("#FFFFFF"),
                    BorderBrush = GetBrush("#E5E5E0"),
                    BorderThickness = new Thickness(1),
                    Foreground = GetBrush("#2D2D2D"),
                    FontSize = 13,
                    CaretBrush = GetBrush("#D97706"),
                    Margin = new Thickness(0, 0, 0, 16)
                };
                
                textBox.Focus();
                textBox.SelectAll();
                stackPanel.Children.Add(textBox);

                var buttonGrid = new Grid();
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var okButton = CreateStyledButton("OK", true);
                okButton.Margin = new Thickness(0, 0, 8, 0);
                Grid.SetColumn(okButton, 1);
                
                var cancelButton = CreateStyledButton("Cancel", false);
                Grid.SetColumn(cancelButton, 2);

                buttonGrid.Children.Add(okButton);
                buttonGrid.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonGrid);

                SetupWindowContent(window, stackPanel, title);

                okButton.Click += (s, e) =>
                {
                    tcs.TrySetResult(textBox.Text);
                    window.Close();
                };

                cancelButton.Click += (s, e) =>
                {
                    tcs.TrySetResult(null);
                    window.Close();
                };

                window.Closed += (s, e) => tcs.TrySetResult(null);

                window.ShowDialog();
            });

            return tcs.Task;
        }

        public Task<bool> ShowConfirmDialogAsync(string title, string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            App.Current.Dispatcher.Invoke(() =>
            {
                var window = CreateBaseDialogWindow(title, 380, 170);

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                var messageText = new TextBlock
                {
                    Text = message,
                    Foreground = GetBrush("#2D2D2D"),
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 20),
                    TextWrapping = TextWrapping.Wrap
                };
                stackPanel.Children.Add(messageText);

                var buttonGrid = new Grid();
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var okButton = CreateStyledButton("Yes", true);
                okButton.Margin = new Thickness(0, 0, 8, 0);
                Grid.SetColumn(okButton, 1);

                var cancelButton = CreateStyledButton("No", false);
                Grid.SetColumn(cancelButton, 2);

                buttonGrid.Children.Add(okButton);
                buttonGrid.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonGrid);

                SetupWindowContent(window, stackPanel, title);

                okButton.Click += (s, e) =>
                {
                    tcs.TrySetResult(true);
                    window.Close();
                };

                cancelButton.Click += (s, e) =>
                {
                    tcs.TrySetResult(false);
                    window.Close();
                };

                window.Closed += (s, e) => tcs.TrySetResult(false);

                window.ShowDialog();
            });

            return tcs.Task;
        }

        public Task<string?> ShowColorPickerDialogAsync(string title, string currentColorHex = "")
        {
            var tcs = new TaskCompletionSource<string?>();

            App.Current.Dispatcher.Invoke(() =>
            {
                var window = CreateBaseDialogWindow(title, 340, 260);

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                var label = new TextBlock
                {
                    Text = "Choose a Column Background Color:",
                    Foreground = GetBrush("#2D2D2D"),
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 12)
                };
                stackPanel.Children.Add(label);

                var colors = new[]
                {
                    "#F0EFEA", // Stone/Grey
                    "#F5EBE6", // Soft Sand
                    "#EBF1FA", // Pale Blue
                    "#EBF6F0", // Sage Green
                    "#F9EBF1", // Soft Rose
                    "#FAF2EB", // Warm Amber
                    "#F1EBF9", // Pale Purple
                    "#EDF2F4"  // Cool Slate
                };

                var wrapPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };

                foreach (var hex in colors)
                {
                    var border = new Border
                    {
                        Width = 44,
                        Height = 44,
                        Background = GetBrush(hex),
                        BorderBrush = string.Equals(hex, currentColorHex, StringComparison.OrdinalIgnoreCase) 
                            ? GetBrush("#D97706") 
                            : GetBrush("#E5E5E0"),
                        BorderThickness = string.Equals(hex, currentColorHex, StringComparison.OrdinalIgnoreCase) 
                            ? new Thickness(2) 
                            : new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Margin = new Thickness(4),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    border.MouseDown += (s, e) =>
                    {
                        tcs.TrySetResult(hex);
                        window.Close();
                    };

                    wrapPanel.Children.Add(border);
                }

                stackPanel.Children.Add(wrapPanel);

                var cancelButton = CreateStyledButton("Cancel", false);
                cancelButton.HorizontalAlignment = HorizontalAlignment.Right;
                stackPanel.Children.Add(cancelButton);

                SetupWindowContent(window, stackPanel, title);

                cancelButton.Click += (s, e) =>
                {
                    tcs.TrySetResult(null);
                    window.Close();
                };

                window.Closed += (s, e) => tcs.TrySetResult(null);

                window.ShowDialog();
            });

            return tcs.Task;
        }

        public Task<string?> ShowOpenFileDialogAsync(string filter, string initialPath = "")
        {
            var tcs = new TaskCompletionSource<string?>();

            App.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = filter
                };

                if (!string.IsNullOrWhiteSpace(initialPath))
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(initialPath);
                        if (Directory.Exists(dir))
                        {
                            dialog.InitialDirectory = dir;
                        }
                    }
                    catch { /* ignore */ }
                }

                if (dialog.ShowDialog() == true)
                {
                    tcs.TrySetResult(dialog.FileName);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            });

            return tcs.Task;
        }

        public Task<string?> ShowSaveFileDialogAsync(string filter, string initialPath = "")
        {
            var tcs = new TaskCompletionSource<string?>();

            App.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new SaveFileDialog
                {
                    Filter = filter,
                    DefaultExt = ".db",
                    AddExtension = true
                };

                if (!string.IsNullOrWhiteSpace(initialPath))
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(initialPath);
                        if (Directory.Exists(dir))
                        {
                            dialog.InitialDirectory = dir;
                        }
                        var filename = Path.GetFileName(initialPath);
                        if (!string.IsNullOrEmpty(filename))
                        {
                            dialog.FileName = filename;
                        }
                    }
                    catch { /* ignore */ }
                }

                if (dialog.ShowDialog() == true)
                {
                    tcs.TrySetResult(dialog.FileName);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            });

            return tcs.Task;
        }

        private Window CreateBaseDialogWindow(string title, double width, double height)
        {
            var owner = App.Current?.MainWindow;
            var window = new Window
            {
                Style = null, // Bypass global styles (e.g. from Wpf.Ui) to keep window custom and transparent
                Title = title,
                Width = width + 40, // Account for shadow margin
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                Background = Brushes.Transparent,
                AllowsTransparency = true,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                FontFamily = new FontFamily("Segoe UI Variable Text, Inter, Segoe UI")
            };

            if (owner != null && owner.IsVisible)
            {
                window.Owner = owner;
            }

            return window;
        }

        private void SetupWindowContent(Window window, FrameworkElement content, string title)
        {
            var mainBorder = new Border
            {
                Background = GetBrush("#F9F9F6"),
                BorderBrush = GetBrush("#E5E5E0"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 16,
                    Color = Colors.Black,
                    Opacity = 0.08,
                    ShadowDepth = 3,
                    Direction = 270
                }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Content (Changed from Star to Auto to fix padding)

            // Header Bar
            var headerBorder = new Border
            {
                BorderBrush = GetBrush("#E5E5E0"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 12, 16, 12),
                Background = GetBrush("#F0EFEA"), // Sidebar background style for header
                CornerRadius = new CornerRadius(12, 12, 0, 0)
            };

            // Allow dragging the dialog window by clicking the header
            headerBorder.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                {
                    try { window.DragMove(); } catch { }
                }
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition());
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Title
            var titleText = new TextBlock
            {
                Text = title.ToUpper(),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = GetBrush("#7A7A6E"), // TextSecondary
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 0);
            headerGrid.Children.Add(titleText);

            // Close button (✕)
            var closeButton = new Button
            {
                Content = "✕",
                Style = (Style)Application.Current.FindResource("IconButton"),
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 11,
                Foreground = GetBrush("#7A7A6E"),
                VerticalAlignment = VerticalAlignment.Center
            };
            closeButton.Click += (s, e) => window.Close();
            Grid.SetColumn(closeButton, 1);
            headerGrid.Children.Add(closeButton);

            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            grid.Children.Add(headerBorder);

            // Content
            Grid.SetRow(content, 1);
            grid.Children.Add(content);

            mainBorder.Child = grid;

            // Wrap in a margin to allow drop shadow to render without clipping
            var shadowGrid = new Grid { Margin = new Thickness(20) };
            shadowGrid.Children.Add(mainBorder);

            window.Content = shadowGrid;
        }

        private Button CreateStyledButton(string content, bool isPrimary)
        {
            return new Button
            {
                Content = content,
                Height = 32,
                MinWidth = 90,
                Style = (Style)Application.Current.FindResource(isPrimary ? "PrimaryButton" : "SecondaryButton")
            };
        }

        public Task<List<int>?> ShowCarryOverDialogAsync(string title, List<CarryOverTaskItem> tasks)
        {
            var tcs = new TaskCompletionSource<List<int>?>();

            App.Current.Dispatcher.Invoke(() =>
            {
                var window = CreateBaseDialogWindow(title, 420, 320);

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                var messageText = new TextBlock
                {
                    Text = "You have unfinished tasks from a previous day. Choose which ones you want to carry over to today:",
                    Foreground = GetBrush("#2D2D2D"),
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 12),
                    TextWrapping = TextWrapping.Wrap
                };
                stackPanel.Children.Add(messageText);

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    MaxHeight = 180,
                    Margin = new Thickness(0, 0, 0, 16),
                    Background = GetBrush("#FFFFFF"),
                    BorderBrush = GetBrush("#E5E5E0"),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10)
                };

                var tasksContainer = new StackPanel();
                var checkboxList = new List<(CheckBox CheckBox, CarryOverTaskItem Item)>();

                foreach (var item in tasks)
                {
                    var cb = new CheckBox
                    {
                        IsChecked = item.IsSelected,
                        Margin = new Thickness(0, 4, 0, 4),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    var textBlock = new TextBlock { FontSize = 13 };
                    textBlock.Inlines.Add(new System.Windows.Documents.Run($"[{item.ProjectName}] ") { Foreground = GetBrush("#7A7A6E"), FontWeight = FontWeights.Medium });
                    textBlock.Inlines.Add(new System.Windows.Documents.Run(item.Name) { Foreground = GetBrush("#2D2D2D") });

                    cb.Content = textBlock;
                    tasksContainer.Children.Add(cb);
                    checkboxList.Add((cb, item));
                }

                scrollViewer.Content = tasksContainer;
                stackPanel.Children.Add(scrollViewer);

                var buttonGrid = new Grid();
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var okButton = CreateStyledButton("Carry Over", true);
                okButton.Margin = new Thickness(0, 0, 8, 0);
                Grid.SetColumn(okButton, 1);

                var cancelButton = CreateStyledButton("Skip", false);
                Grid.SetColumn(cancelButton, 2);

                buttonGrid.Children.Add(okButton);
                buttonGrid.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonGrid);

                SetupWindowContent(window, stackPanel, title);

                okButton.Click += (s, e) =>
                {
                    var selectedIds = new List<int>();
                    foreach (var cbTuple in checkboxList)
                    {
                        if (cbTuple.CheckBox.IsChecked == true)
                        {
                            selectedIds.Add(cbTuple.Item.Id);
                        }
                    }
                    tcs.TrySetResult(selectedIds);
                    window.Close();
                };

                cancelButton.Click += (s, e) =>
                {
                    tcs.TrySetResult(null);
                    window.Close();
                };

                window.Closed += (s, e) => tcs.TrySetResult(null);

                window.ShowDialog();
            });

            return tcs.Task;
        }

        private SolidColorBrush GetBrush(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        }
    }
}
