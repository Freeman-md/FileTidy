using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ExCSS;
using FileTidy.Core.Models;

namespace FileTidy.GUI.Converters;

public class RevertVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FileOperationStatus status)
        {
            // NOTE: this is flipped because Avalonia is wild sometimes
            return status != FileOperationStatus.Moved ? Visibility.Visible : Visibility.Hidden;
        }

        return Visibility.Hidden;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}