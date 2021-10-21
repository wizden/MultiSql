using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MultiSql.Converters
{
    [ValueConversion(typeof(Boolean), typeof(Visibility))]
    internal class BooleanToVisibilityConverter : IValueConverter
    {

        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
            {
                throw new InvalidOperationException("The target must be a boolean");
            }

            return value != null && value.GetType() == typeof(Boolean) && (Boolean) value
                       ? Visibility.Visible
                       : Visibility.Collapsed;
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture) => throw new NotImplementedException();

    }
}
