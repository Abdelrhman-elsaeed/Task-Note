using System;
using System.Linq;
using System.Windows;
using Wpf.Ui.Appearance;

namespace TaskNote.ViewModels
{
    public static class ThemeHelper
    {
        public static void ApplyTheme(string themeName)
        {
            if (Application.Current == null) return;
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                var isDark = themeName.Equals("Dark", StringComparison.OrdinalIgnoreCase);

                try
                {
                    // 1. Apply WPF-UI built-in theme
                    ApplicationThemeManager.Apply(isDark ? ApplicationTheme.Dark : ApplicationTheme.Light);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Failed to apply WPF UI application theme");
                }

                try
                {
                    // 2. Swap our custom theme colors dictionary
                    var dicts = Application.Current.Resources.MergedDictionaries;
                    var customThemeDict = dicts.FirstOrDefault(d => 
                        d.Source != null && 
                        (d.Source.OriginalString.Contains("LightTheme.xaml") || 
                         d.Source.OriginalString.Contains("DarkTheme.xaml")));

                    if (customThemeDict != null)
                    {
                        customThemeDict.Source = new Uri(isDark ? "Resources/DarkTheme.xaml" : "Resources/LightTheme.xaml", UriKind.Relative);
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Failed to swap custom theme colors dictionary");
                }
            });
        }
    }
}
