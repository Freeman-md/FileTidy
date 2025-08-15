using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.ViewModels.Home;

namespace FileTidy.GUI.ViewModels.Pages
{
    public partial class HelpViewModel : ViewModelBase
    {
        private readonly IFolderService _folderService;

        public HelpViewModel(IFolderService folderService)
        {
            _folderService = folderService;
        }

        [RelayCommand]
        private async Task OpenOsSettingsAsync()
        {
            await _folderService.OpenSystemFilesAndFoldersSettingsAsync();
        }
    }
} 