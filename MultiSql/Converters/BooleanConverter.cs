using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace MultiSql.Converters
{
    public class BooleanConverter<T> : IValueConverter
    {

        public BooleanConverter(T trueValue, T falseValue)
        {
            TrueValue  = trueValue;
            FalseValue = falseValue;
        }

        public T TrueValue { get; set; }

        public T FalseValue { get; set; }

        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture) => value is Boolean && (Boolean) value ? TrueValue : FalseValue;

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture) => value is T && EqualityComparer<T>.Default.Equals((T) value, TrueValue);

    }
}
