using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Helpers;
using FileTidy.GUI.Models;
using FileTidy.GUI.Views.Onboarding.Steps;

namespace FileTidy.GUI.ViewModels.Layouts;

public partial class OnboardingWindowViewModel : ViewModelBase
{
    private readonly IFileOperationStore _fileOperationStore;
    private readonly IFolderService _folderService;
    private readonly IAppConfigService _appConfig;
    
    public string DesktopPath  => Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public string DocumentsPath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public string DownloadsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    // Current step
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreviousButton))]
    [NotifyPropertyChangedFor(nameof(CurrentStepView))]
    [NotifyPropertyChangedFor(nameof(PrimaryButtonText))]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(StepPreviewSource))]
    [NotifyPropertyChangedFor(nameof(StepPreviewBinding))]
    [NotifyPropertyChangedFor(nameof(PreviewImageHorizontalAlignment))]
    [NotifyPropertyChangedFor(nameof(PreviewContainerMargin))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private int _currentStepIndex;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AtLeastOneGranted))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _desktopGranted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AtLeastOneGranted))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _downloadsGranted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AtLeastOneGranted))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _documentsGranted;

    public ObservableCollection<string> SelectedFolders { get; } = new();

    public bool AtLeastOneGranted => DesktopGranted || DownloadsGranted || DocumentsGranted;

    // public bool ShowPreviousButton => CurrentStepIndex > 0 && CurrentStepIndex < _steps.Count - 1; // we have two steps for now but the 2nd step is not the last step so we comment this for now and use the one below

    public bool ShowPreviousButton => CurrentStepIndex > 0;

    // Steps and preview assets
    private readonly List<UserControl> _steps;

    public int StepsCount => _steps.Count;
    public int StepsCountMinusOne => Math.Max(0, StepsCount - 1);
    
    private string GetAccessImageByOs()
    {
        if (OperatingSystem.IsMacOS())   return "access-macos.png";
        if (OperatingSystem.IsWindows()) return "access-windows.png";
        return "access-linux.png";
    }

    public string StepPreviewSource
    {
        get
        {
            // 0,1,2 share the same image:
            if (CurrentStepIndex is 0 or 1 or 2)
                return "avares://FileTidy.GUI/Assets/Images/onboarding/welcome.png";

            // 3 = Access (OS-specific)
            if (CurrentStepIndex == 3)
                return $"avares://FileTidy.GUI/Assets/Images/onboarding/{GetAccessImageByOs()}";

            // 4 = Completion
            return "avares://FileTidy.GUI/Assets/Images/onboarding/completion.png";
        }
    }

    
    public Bitmap StepPreviewBinding => ImageHelper.LoadFromResource(new Uri(StepPreviewSource));
    
    public HorizontalAlignment PreviewImageHorizontalAlignment =>
        CurrentStepIndex is 0 or 1 or 2
            ? HorizontalAlignment.Right
            : HorizontalAlignment.Center;

    public Thickness PreviewContainerMargin =>
        CurrentStepIndex is 0 or 1 or 2
            ? new Thickness(24, 0, -200, 0)
            : new Thickness(0);


    public string PrimaryButtonText => CurrentStepIndex switch
    {
        0 => "Get Started",
        1 => "Next",
        2 => "Next",
        3 => "Continue",
        4 => "Restart Application",
        _ => "Next"
    };

    public string StepTitle => CurrentStepIndex switch
    {
        0 => "Welcome to FileTidy",
        1 => "Our Pledge to Privacy",
        2 => "Data Security & Encryption",
        3 => "Folder Access Permissions",
        4 => "You're Ready",
        _ => string.Empty
    };

    public UserControl CurrentStepView => _steps[CurrentStepIndex];
    
    public bool CanGoNext => CurrentStepIndex is not 3 || AtLeastOneGranted;

    public OnboardingWindowViewModel(
        IFileOperationStore fileOperationStore,
        IFolderService folderService,
        IAppConfigService appConfig)
    {
        _fileOperationStore = fileOperationStore;
        _folderService = folderService;
        _appConfig = appConfig;

        // For now, only the initial Hello step is enabled to stabilize the shell
        _steps =
        [
            new WelcomeStepView(),
            new PledgeStepView(),
            new SecurityStepView(),
            new AccessStepView(),
        ];

        _ = ProbeAllAsync();
    }

    public OnboardingWindowViewModel()
    {
        _steps =
        [
            new WelcomeStepView()
        ];

        _ = ProbeAllAsync();
    }

    [RelayCommand]
    private async Task ProbeAllAsync()
    {
        DesktopGranted   = await _folderService.CanAccessAsync(DesktopPath);
        DownloadsGranted = await _folderService.CanAccessAsync(DownloadsPath);
        DocumentsGranted = await _folderService.CanAccessAsync(DocumentsPath);
        
        SelectedFolders.Clear();
        if (DesktopGranted)  SelectedFolders.Add(DesktopPath);
        if (DownloadsGranted) SelectedFolders.Add(DownloadsPath);
        if (DocumentsGranted) SelectedFolders.Add(DocumentsPath);
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task PrimaryActionAsync()
    {
        switch (CurrentStepIndex)
        {
            case 0:
            case 1:
                NextStep();
                break;
            default:
                NextStep();
                break;
        }
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStepIndex < _steps.Count - 1)
        {
            CurrentStepIndex++;
            OnStepChanged();
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0)
            CurrentStepIndex--;
    }

    [RelayCommand]
    private async Task OpenOsSettingsAsync()
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = true;

            if (OperatingSystem.IsMacOS())
            {
                process.StartInfo.FileName =
                    "x-apple.systempreferences:com.apple.preference.security?Privacy_FilesAndFolders";
            }
            else if (OperatingSystem.IsWindows())
            {
                process.StartInfo.FileName = "ms-settings:privacy-broadfilesystemaccess";
            }
            else if (OperatingSystem.IsLinux())
            {
                process.StartInfo.FileName = "xdg-open";
                process.StartInfo.Arguments = "https://wiki.gnome.org/Design/OS/Privacy";
                process.StartInfo.UseShellExecute = false;
            }

            process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        await ProbeAllAsync();
    }
    
    private CancellationTokenSource? _accessStepCts;

    private async Task MonitorAccessStepAsync()
    {
        Console.WriteLine("[MonitorAccessStep] Started");
        _accessStepCts?.Cancel();
        _accessStepCts = new CancellationTokenSource();
        var token = _accessStepCts.Token;

        try
        {
            while (!token.IsCancellationRequested && CurrentStepIndex == 3)
            {
                Console.WriteLine(CanGoNext);
                
                await ProbeAllAsync();
                Console.WriteLine($"[MonitorAccessStep] Granted: {AtLeastOneGranted}");

                await Task.Delay(1500, token);
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("[MonitorAccessStep] Cancelled");
        }
    }

    
    private void OnStepChanged()
    {
        if (CurrentStepIndex == 3)
        {
            _ = ProbeAllAsync();
            _ = MonitorAccessStepAsync();
        }
        else
        {
            _accessStepCts?.Cancel();
        }
    }


}
