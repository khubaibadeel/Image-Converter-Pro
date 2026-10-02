using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace ImageConverterPro.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            Type type = value.GetType();
            if (!type.IsEnum) return string.Empty;

            var name = value.ToString();
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            FieldInfo? field = type.GetField(name);
            if (field == null) return string.Empty;

            DescriptionAttribute? attribute = field.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
