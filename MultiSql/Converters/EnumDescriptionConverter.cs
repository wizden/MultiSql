using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace MultiSql.Converters
{
    public class EnumDescriptionConverter : IValueConverter
    {

        private String GetEnumDescription(Enum enumObj)
        {
            var fieldInfo = enumObj.GetType().GetField(enumObj.ToString());

            var attribArray = fieldInfo.GetCustomAttributes(false);

            if (attribArray.Length == 0)
            {
                return enumObj.ToString();
            }

            foreach (var att in attribArray)
            {
                if (att is DescriptionAttribute)
                {
                    return ((DescriptionAttribute) att).Description;
                }
            }

            return enumObj.ToString();
        }

        Object IValueConverter.Convert(Object value, Type targetType, Object parameter, CultureInfo culture) => GetEnumDescription((Enum) value);

        Object IValueConverter.ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture) => String.Empty;

    }
}
