using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ImageConverterPro.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var status = value.ToString();
                switch (status)
                {
                    case "Success":
                    case "Completed":
                        return Application.Current?.TryFindResource("SuccessColor") as SolidColorBrush ?? new SolidColorBrush(Colors.Green);
                    case "Failed":
                        return Application.Current?.TryFindResource("ErrorColor") as SolidColorBrush ?? new SolidColorBrush(Colors.Red);
                    case "Pending":
                        return Application.Current?.TryFindResource("WarningColor") as SolidColorBrush ?? new SolidColorBrush(Colors.Orange);
                    case "Converting":
                        return Application.Current?.TryFindResource("AccentColor") as SolidColorBrush ?? new SolidColorBrush(Colors.Blue);
                }
            }
            return Application.Current?.TryFindResource("SecondaryText") as SolidColorBrush ?? new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
