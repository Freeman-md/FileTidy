using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FileTidy.GUI.Converters;

public class MultiplyConverter : IValueConverter
{
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double number && parameter != null)
            {
                if (double.TryParse(parameter.ToString(), out var multiplier))
                {
                    return number * multiplier;
                }
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
}