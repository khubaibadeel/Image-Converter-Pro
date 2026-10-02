using System;
using System.Globalization;
using System.Windows.Data;

namespace ImageConverterPro.Converters
{
    /// <summary>
    /// Formats a numeric byte count for display.
    /// The class name is intentionally different from the XAML resource key
    /// so it cannot conflict with an existing project converter type.
    /// </summary>
    public sealed class ByteSizeToStringConverter : IValueConverter
    {
        private static readonly string[] Units =
        {
            "B",
            "KB",
            "MB",
            "GB",
            "TB",
            "PB"
        };

        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is null)
            {
                return "0 B";
            }

            double bytes;

            try
            {
                bytes = System.Convert.ToDouble(
                    value,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                // If the property is already a formatted string, preserve it.
                return value.ToString() ?? "0 B";
            }

            if (double.IsNaN(bytes) ||
                double.IsInfinity(bytes) ||
                bytes < 0)
            {
                return "0 B";
            }

            var unitIndex = 0;

            while (bytes >= 1024d &&
                   unitIndex < Units.Length - 1)
            {
                bytes /= 1024d;
                unitIndex++;
            }

            if (unitIndex == 0)
            {
                return $"{bytes:0} {Units[unitIndex]}";
            }

            return $"{bytes:0.##} {Units[unitIndex]}";
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
