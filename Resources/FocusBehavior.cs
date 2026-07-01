using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TaskNote.Resources
{
    public static class FocusBehavior
    {
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsFocused",
                typeof(bool),
                typeof(FocusBehavior),
                new FrameworkPropertyMetadata(false, 
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, 
                    OnIsFocusedChanged));

        public static bool GetIsFocused(DependencyObject obj) => (bool)obj.GetValue(IsFocusedProperty);
        public static void SetIsFocused(DependencyObject obj, bool value) => obj.SetValue(IsFocusedProperty, value);

        private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                {
                    if (element.IsLoaded)
                    {
                        FocusAndSelect(element);
                    }
                    else
                    {
                        RoutedEventHandler? loadedHandler = null;
                        loadedHandler = (s, args) =>
                        {
                            element.Loaded -= loadedHandler;
                            if (GetIsFocused(element))
                            {
                                FocusAndSelect(element);
                            }
                        };
                        element.Loaded += loadedHandler;
                    }
                }
            }
        }

        private static void FocusAndSelect(FrameworkElement element)
        {
            element.Dispatcher.BeginInvoke(new Action(() =>
            {
                element.Focus();
                if (element is TextBox textBox)
                {
                    textBox.SelectAll();
                    
                    // Register a key down handler to clear focus on Enter
                    textBox.KeyDown -= OnTextBoxKeyDown;
                    textBox.KeyDown += OnTextBoxKeyDown;

                    // Also set IsFocused to false when focus is lost
                    textBox.LostFocus -= OnTextBoxLostFocus;
                    textBox.LostFocus += OnTextBoxLostFocus;
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private static void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                e.Handled = true;
                Keyboard.ClearFocus();
            }
        }

        private static void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.LostFocus -= OnTextBoxLostFocus;
                textBox.KeyDown -= OnTextBoxKeyDown;
                SetIsFocused(textBox, false);
            }
        }
    }
}
