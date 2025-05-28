using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace FileTidy.GUI.Components;

public partial class StatusBadge : UserControl, INotifyPropertyChanged
{

    public static readonly StyledProperty<string> StatusProperty =
        AvaloniaProperty.Register<StatusBadge, string>(nameof(Status), defaultValue: "Moved");

    public IBrush BadgeColor => Status switch
    {
        "Moved" => Brushes.MediumSeaGreen,
        "Skipped" => Brushes.Goldenrod,
        "Pending" => Brushes.Gray,
        _ => Brushes.LightGray,
    };

    public string Status
    {
        get => GetValue(StatusProperty);
        set
        {
            SetValue(StatusProperty, value);
            OnPropertyChanged(nameof(BadgeColor));
        }
    }

    public StatusBadge()
    {
        InitializeComponent();
    }
    
    public new event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}