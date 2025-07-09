using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.Core.Interfaces;

namespace FileTidy.GUI.ViewModels;

public partial class SortOperationViewModel : ViewModelBase
{
    private readonly FolderTreeViewModel _folderTreeViewModel;
    private readonly FileListViewModel _fileListViewModel;
    private readonly IFileOrganizerService _fileOrganizerService;
    private readonly IFileOperationStore _fileOperationStore;
    
    private CancellationTokenSource? _sortingCancellationTokenSource;
    
    [ObservableProperty] private int _operationProgress = 0;
    [ObservableProperty] private int _filesProcessed = 0;
    [ObservableProperty] private string _elapsedTime = "0m 00s";
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopSortingCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertLastSortCommand))]
    [NotifyPropertyChangedFor(nameof(CurrentOperationLabel))]
    private bool _isSorting;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTidyingCommand))]
    [NotifyPropertyChangedFor(nameof(CurrentOperationLabel))]
    private bool _isReverting;
    
    [ObservableProperty] private bool _wasCancelled;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RevertLastSortCommand))]
    private Guid _lastSortSessionId = Guid.Empty;
    
    private bool CanStartTidying => _folderTreeViewModel.SelectedFolder is not null && IsSorting is false && IsReverting is false;
    private bool CanRevertLastSort() => LastSortSessionId != Guid.Empty && IsSorting == false && IsReverting == false;
    
    public string CurrentOperationLabel
    {
        get
        {
            if (IsReverting)
                return "files reverted";
            if (IsSorting)
                return "files sorted";
            return "files processed";
        }
    }

    public SortOperationViewModel(
        FolderTreeViewModel folderTreeViewModel,
        FileListViewModel fileListViewModel,
        IFileOrganizerService fileOrganizerService,
        IFileOperationStore fileOperationStore
        )
    {
        _folderTreeViewModel = folderTreeViewModel;
        _fileListViewModel = fileListViewModel;
        _fileOrganizerService = fileOrganizerService;
        _fileOperationStore = fileOperationStore;
    }
    
    [RelayCommand(CanExecute = nameof(CanStartTidying))]
    private async Task StartTidying()
    {
        if (_folderTreeViewModel.SelectedFolder is null)
            return;

        try
        {
            IsSorting = true;
            OperationProgress = 0;
            FilesProcessed = 0;
            ElapsedTime = "0m 00s";
            WasCancelled = false;
            
            _sortingCancellationTokenSource = new CancellationTokenSource();
            var token = _sortingCancellationTokenSource.Token;
            
            LastSortSessionId = Guid.NewGuid();
            await _fileOperationStore.SaveConfigValueAsync(nameof(LastSortSessionId), LastSortSessionId.ToString());

            
            var result = await _fileOrganizerService.SortDirectoryAsync(_folderTreeViewModel.SelectedFolder.FullPath, LastSortSessionId, token);

            Console.WriteLine($"Tidying complete. Moved: {result.TotalMoved}, Errors: {result.TotalErrors}");

            _ = _fileListViewModel.LoadFilesForSelectedFolder();
            // Instead of doing this, let's just update the current folder with the new structure directly
            // _ = InitializeFolderTreeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine($"Tidying failed: {ex.Message}");
        }
        finally
        {
            IsSorting = false;
            
            _sortingCancellationTokenSource?.Dispose();
            _sortingCancellationTokenSource = null;
        }
    }

    [RelayCommand] private void PauseSorting() => Console.WriteLine("Pause sorting triggered");
    
    [RelayCommand(CanExecute = nameof(IsSorting))] 
    private void StopSorting() {
        if (_sortingCancellationTokenSource is not null)
        {
            _sortingCancellationTokenSource?.Cancel();
            _sortingCancellationTokenSource = null;
            IsSorting = false;
            WasCancelled = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRevertLastSort))]
    private async Task RevertLastSort()
    {
        if (LastSortSessionId == Guid.Empty)
            return;
        
        IsReverting = true;
        OperationProgress = 0;
        FilesProcessed = 0;
        ElapsedTime = "0m 00s";

        await _fileOrganizerService.RevertSessionAsync(LastSortSessionId);
        
        LastSortSessionId = Guid.Empty;
        await _fileOperationStore.DeleteConfigValueAsync(nameof(LastSortSessionId));


        _ = _fileListViewModel.LoadFilesForSelectedFolder();

        IsReverting = false;
    }
}