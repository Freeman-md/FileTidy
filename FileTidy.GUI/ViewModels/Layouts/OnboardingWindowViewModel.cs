using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

    // Current step
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreviousButton))]
    [NotifyPropertyChangedFor(nameof(ShowPrimaryButton))]
    [NotifyPropertyChangedFor(nameof(CurrentStepView))]
    [NotifyPropertyChangedFor(nameof(PrimaryButtonText))]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(StepPreviewSource))]
    [NotifyPropertyChangedFor(nameof(StepPreviewBinding))]
    [NotifyPropertyChangedFor(nameof(PreviewImageHorizontalAlignment))]
    [NotifyPropertyChangedFor(nameof(PreviewContainerMargin))]
    private int _currentStepIndex;

    // Access statuses (kept for later steps)
    [ObservableProperty] private bool _desktopGranted;
    [ObservableProperty] private bool _downloadsGranted;
    [ObservableProperty] private bool _documentsGranted;

    public bool AtLeastOneGranted => DesktopGranted || DownloadsGranted || DocumentsGranted;

    // public bool ShowPreviousButton => CurrentStepIndex > 0 && CurrentStepIndex < _steps.Count - 1; // we have two steps for now but the 2nd step is not the last step so we comment this for now and use the one below

    public bool ShowPreviousButton => CurrentStepIndex > 0;
    public bool ShowPrimaryButton => true;

    // Steps and preview assets
    private readonly List<UserControl> _steps;

    public int StepsCount => _steps.Count;
    public int StepsCountMinusOne => Math.Max(0, StepsCount - 1);
    
    private readonly string[] _previewFiles =
    [
        "welcome.png",
        "welcome.png",
        "welcome.png",
        "access.png",
        "completion.png"
    ];

    public string StepPreviewSource =>
        $"avares://FileTidy.GUI/Assets/Images/onboarding/{_previewFiles[Math.Clamp(CurrentStepIndex, 0, _previewFiles.Length - 1)]}";
    
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
        DesktopGranted   = await _folderService.CanAccessAsync(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        DownloadsGranted = await _folderService.CanAccessAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
        DocumentsGranted = await _folderService.CanAccessAsync(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        OnPropertyChanged(nameof(AtLeastOneGranted));
        OnPropertyChanged(nameof(PrimaryButtonText));
    }

    [RelayCommand]
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
            CurrentStepIndex++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0)
            CurrentStepIndex--;
    }
}
