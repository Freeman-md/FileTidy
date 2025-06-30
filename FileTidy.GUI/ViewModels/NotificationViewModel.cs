using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FileTidy.GUI.ViewModels;

public partial class NotificationViewModel : ViewModelBase
{
    [ObservableProperty] private string? _notificationTitle;
    [ObservableProperty] private string? _notificationMessage;
    [ObservableProperty] private bool _isNotificationVisible;

    public void Show(string title, string message)
    {
        NotificationTitle = title;
        NotificationMessage = message;
        IsNotificationVisible = true;
                
        Task.Delay(4000).ContinueWith(_ =>
        {
            IsNotificationVisible = false;
        });
    }
}