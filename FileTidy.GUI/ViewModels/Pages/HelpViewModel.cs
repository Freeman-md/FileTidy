// HelpViewModel.cs
using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Helpers;

namespace FileTidy.GUI.ViewModels.Pages
{
    public partial class HelpViewModel : ViewModelBase
    {
        private readonly IFolderService _folderService;

        [ObservableProperty] private bool _isLightboxOpen;
        [ObservableProperty] private Bitmap? _lightboxImage;  // <-- Bitmap, not string

        public HelpViewModel(IFolderService folderService)
        {
            _folderService = folderService;
        }

        [RelayCommand]
        private async Task OpenOsSettingsAsync()
        {
            await _folderService.OpenSystemFilesAndFoldersSettingsAsync();
        }

        // Called from code-behind with the string path in Image.Tag
        public void OpenLightbox(string source)
        {
            try
            {
                // Support avares:// (embedded), http(s), and file paths.
                if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
                {
                    if (uri.Scheme == "avares")
                    {
                        LightboxImage = ImageHelper.LoadFromResource(uri);
                    }
                    else if (uri.Scheme is "http" or "https")
                    {
                        // Fire-and-forget loader for web images; keep UI snappy
                        _ = LoadWebAsync(uri);
                        return;
                    }
                    else if (uri.Scheme == "file")
                    {
                        LightboxImage = new Bitmap(uri.LocalPath);
                    }
                    else
                    {
                        // Fallback: try as local path
                        LightboxImage = new Bitmap(source);
                    }
                }
                else
                {
                    // Fallback: try as local path
                    LightboxImage = new Bitmap(source);
                }

                IsLightboxOpen = LightboxImage is not null;
            }
            catch
            {
                LightboxImage = null;
                IsLightboxOpen = false;
            }
        }

        private async Task LoadWebAsync(Uri uri)
        {
            var bmp = await ImageHelper.LoadFromWeb(uri);
            LightboxImage = bmp;
            IsLightboxOpen = bmp is not null;
        }

        [RelayCommand]
        public void CloseLightbox()
        {
            IsLightboxOpen = false;
            LightboxImage = null;
        }
    }
}
