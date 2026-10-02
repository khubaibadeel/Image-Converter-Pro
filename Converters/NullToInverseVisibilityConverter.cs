using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ImageConverterPro.Converters
{
    /// <summary>Shows an element only while its bound value is null.</summary>
    public sealed class NullToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }
}
