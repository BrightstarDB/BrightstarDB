using System;
using System.Globalization;
using System.Windows.Data;

namespace BrightstarDB.Polaris.Converters
{
    public class TypeMatchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var matchTypeName = parameter as string;
            if (matchTypeName != null && value != null)
            {
                var matchType = Type.GetType(matchTypeName);
                return matchType != null && matchType.IsAssignableFrom(value.GetType());
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
