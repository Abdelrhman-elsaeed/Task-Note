using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TaskNote.Services;

namespace TaskNote.Resources
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                var invert = parameter != null && string.Equals(parameter.ToString(), "Inverted", StringComparison.OrdinalIgnoreCase);
                if (invert)
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                var invert = parameter != null && string.Equals(parameter.ToString(), "Inverted", StringComparison.OrdinalIgnoreCase);
                if (invert) return visibility != Visibility.Visible;
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    public class HexToBrushConverter : IValueConverter
    {
        private static readonly BrushConverter BrushConverter = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);

                    // Check if Dark Theme is active
                    if (App.Host != null)
                    {
                        var settingsService = (ISettingsService?)App.Host.Services.GetService(typeof(ISettingsService));
                        if (settingsService?.CurrentSettings.Theme == "Dark")
                        {
                            color = AdjustColorForDarkMode(color);
                        }
                    }

                    return new SolidColorBrush(color);
                }
                catch
                {
                    return GetFallbackBrush();
                }
            }
            return GetFallbackBrush();
        }

        private SolidColorBrush GetFallbackBrush()
        {
            if (App.Host != null)
            {
                var settingsService = (ISettingsService?)App.Host.Services.GetService(typeof(ISettingsService));
                if (settingsService?.CurrentSettings.Theme == "Dark")
                {
                    return new SolidColorBrush(Color.FromRgb(30, 30, 28)); // Dark Mode Column background fallback
                }
            }
            return new SolidColorBrush(Color.FromRgb(240, 239, 234)); // Light Mode Column background fallback
        }

        private static Color AdjustColorForDarkMode(Color color)
        {
            // Convert to HSL
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            double h = 0;
            double s = 0;
            double l = (max + min) / 2.0;

            if (max != min)
            {
                double d = max - min;
                s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

                if (max == r)
                    h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g)
                    h = (b - r) / d + 2;
                else if (max == b)
                    h = (r - g) / d + 4;

                h /= 6.0;
            }

            // Dark Mode target: elegant deep low lightness, slightly saturated colors
            l = 0.12; 
            s = Math.Min(1.0, s * 1.2); 

            // Convert back to RGB
            double q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
            double p = 2.0 * l - q;

            Func<double, double> hueToRgb = (t) =>
            {
                if (t < 0) t += 1.0;
                if (t > 1.0) t -= 1.0;
                if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
                if (t < 1.0 / 2.0) return q;
                if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
                return p;
            };

            byte newR = (byte)Math.Clamp(hueToRgb(h + 1.0 / 3.0) * 255, 0, 255);
            byte newG = (byte)Math.Clamp(hueToRgb(h) * 255, 0, 255);
            byte newB = (byte)Math.Clamp(hueToRgb(h - 1.0 / 3.0) * 255, 0, 255);

            return Color.FromRgb(newR, newG, newB);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush) return brush.Color.ToString();
            return "#F0EFEA";
        }
    }

    /// <summary>Collapses when string is null/empty; shows when non-empty.</summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var invert = parameter != null && string.Equals(parameter.ToString(), "Inverted", StringComparison.OrdinalIgnoreCase);
            bool hasValue = !string.IsNullOrEmpty(value as string);
            if (invert) return hasValue ? Visibility.Collapsed : Visibility.Visible;
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>Returns Visible when count == 0, Collapsed otherwise (for empty-state placeholders).</summary>
    public class ZeroCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count) return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>Rotates chevron 0° when collapsed, 90° when expanded.</summary>
    public class BoolToRotationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? 90.0 : 0.0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>Inverts a boolean value.</summary>
    public class BooleanInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;
    }
}
