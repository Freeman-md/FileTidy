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

    public bool ShowPreviousButton => CurrentStepIndex > 0 && CurrentStepIndex < _steps.Count - 1;
    public bool ShowPrimaryButton => true;

    // Steps and preview assets
    private readonly List<UserControl> _steps;

    public int StepsCount => _steps.Count;
    public int StepsCountMinusOne => Math.Max(0, StepsCount - 1);

    public string StepPreviewSource =>
        "avares://FileTidy.GUI/Assets/Images/onboarding/welcome.png";
    
    public Bitmap StepPreviewBinding => ImageHelper.LoadFromResource(new Uri(StepPreviewSource));
    
    public HorizontalAlignment PreviewImageHorizontalAlignment =>
        CurrentStepIndex is 0 or 1 ? HorizontalAlignment.Right : HorizontalAlignment.Center;

    public Thickness PreviewContainerMargin =>
        CurrentStepIndex is 0 or 1 ? new Thickness(24, 0, -200, 0) : new Thickness(0);

    public string PrimaryButtonText => CurrentStepIndex switch
    {
        0 => "Get Started",
        _ => ""
    };

    public string StepTitle => CurrentStepIndex switch
    {
        0 => "Welcome to FileTidy",
        _ => ""
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
            new WelcomeStepView()
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
                // Next steps will be added gradually; no-op for now
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
