using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ImageConverterPro.Converters
{
    /// <summary>
    /// Converts true to Collapsed and false to Visible.
    /// The class name is intentionally different from the XAML resource key
    /// so it cannot conflict with an existing project converter type.
    /// </summary>
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            var isTrue = value is bool boolean && boolean;

            return isTrue
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }

            return Binding.DoNothing;
        }
    }
}
