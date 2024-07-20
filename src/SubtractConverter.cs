using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1
{
    public class SubtractConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Subtract a value from the input value (e.g., to be used in animations)
            if (value is double inputValue && parameter is double subtractValue)
            {
                return inputValue - subtractValue;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}