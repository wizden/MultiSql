using System;
using System.Globalization;
using System.Windows.Data;

namespace MultiSql.Converters
{
    [ValueConversion(typeof(Boolean), typeof(Boolean))]
    internal class InverseBooleanConverter : IValueConverter
    {

        #region Public

        #region Method

        public Object Convert(Object      value,
                              Type        targetType,
                              Object      parameter,
                              CultureInfo culture)
        {
            if (targetType != typeof(Boolean))
            {
                throw new InvalidOperationException("The target must be a boolean");
            }

            return !(Boolean) value;
        }

        public Object ConvertBack(Object      value,
                                  Type        targetType,
                                  Object      parameter,
                                  CultureInfo culture) =>
            throw new NotSupportedException();

        #endregion

        #endregion

    }
}