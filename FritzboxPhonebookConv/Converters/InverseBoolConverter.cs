using System;
using System.Globalization;
using System.Windows.Data;

namespace FritzboxPhonebookConv.Converters
{
    /// <summary>Inverts a <see cref="bool"/> value for use in bindings.</summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? (object)!b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? (object)!b : value;
    }
}
