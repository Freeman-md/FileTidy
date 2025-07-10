using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FileTidy.GUI.ViewModels
{
    public partial class MainLayoutViewModel : ViewModelBase
    {
        [ObservableProperty]
        private object? content;
        
        public string AppVersion => $"FileTidy v{Assembly.GetExecutingAssembly().GetName().Version} | Built by Freemancodz";

        public MainLayoutViewModel(object? content = null)
        {
            Content = content;
        }
    }
} 