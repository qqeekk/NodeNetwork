using System;
using System.Globalization;
using System.Windows.Data;

namespace NodeNetwork.Utilities.WPF
{
    public class ConverterFromFunction<T> : IValueConverter
    {
        public Func<T, string> Func { get; set; } = o => o.ToString();

        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            return Func((T)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
