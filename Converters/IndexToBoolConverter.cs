using System;
using System.Globalization;
using System.Windows.Data;

namespace ImageConverterPro.Converters
{
    public class IndexToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && parameter is string strParam && int.TryParse(strParam, out int targetValue))
            {
                return intValue == targetValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string strParam && int.TryParse(strParam, out int targetValue))
            {
                return targetValue;
            }
            return Binding.DoNothing;
        }
    }
}
