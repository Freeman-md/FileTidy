using System;
using System.Globalization;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;

namespace FileTidy.GUI.Converters;

public class FolderToTemplateConverter : IValueConverter
{
    public IDataTemplate? FileTemplate { get; set; }
    public IDataTemplate? EmptyTemplate { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isFolder = value is true;

        return isFolder ? EmptyTemplate : FileTemplate;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}