using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FileTidy.GUI.Components;

public partial class StatusBadge : UserControl
{

    public static readonly StyledProperty<string> StatusProperty =
        AvaloniaProperty.Register<StatusBadge, string>(nameof(Status), defaultValue: "Moved");

    public string Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }
    
    public StatusBadge()
    {
        InitializeComponent();
    }
}