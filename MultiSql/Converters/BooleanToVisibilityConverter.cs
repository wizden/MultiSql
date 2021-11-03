using System.Windows;

namespace MultiSql.Converters
{
    public class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {

        public BooleanToVisibilityConverter()
            : base(Visibility.Visible, Visibility.Collapsed) { }

    }
}
