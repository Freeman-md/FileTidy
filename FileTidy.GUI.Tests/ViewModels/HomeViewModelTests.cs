using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using FileTidy.Core.Interfaces;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;
using FileTidy.GUI.Reporting;
using FileTidy.GUI.ViewModels;
using Moq;
using Xunit;

namespace FileTidy.Gui.Tests.ViewModels;

public class HomeViewModelTests
{
    private readonly Mock<IFileOperationStore> _operationStoreMock = new();
    private readonly Mock<ISortReporter> _sortReporterMock = new();
    private readonly FolderTreeViewModel _folderTreeViewModel;
    private readonly FileListViewModel _fileListViewModel;
    private readonly SortOperationViewModel _sortOperationViewModel;
    private readonly NotificationViewModel _notificationViewModel;
    private readonly HomeViewModel _viewModel;

    public HomeViewModelTests()
    {
        _folderTreeViewModel = new FolderTreeViewModel(Mock.Of<IFolderService>(), _ => { });
        _fileListViewModel = new FileListViewModel(
            _folderTreeViewModel,
            Mock.Of<IFolderService>(),
            Mock.Of<IFileStatusService>(),
            Mock.Of<IFileOrganizerService>(),
            _ => { }
        );

        _sortOperationViewModel = new SortOperationViewModel(
            _folderTreeViewModel,
            _fileListViewModel,
            Mock.Of<IFileOrganizerService>(),
            _operationStoreMock.Object
        );

        _notificationViewModel = new NotificationViewModel();

        _viewModel = new HomeViewModel(
            _operationStoreMock.Object,
            _folderTreeViewModel,
            _fileListViewModel,
            _sortOperationViewModel,
            _notificationViewModel,
            _sortReporterMock.Object
        );
    }

    [Fact]
    public void SelectedFolderPath_ReturnsEmpty_WhenNoFolderSelected()
    {
        _folderTreeViewModel.SelectedFolder = null;
        Assert.Equal(string.Empty, _viewModel.SelectedFolderPath);
    }

    [Fact]
    public void SelectedFolderPath_ReturnsPath_WhenFolderSelected()
    {
        var folder = new FolderItem { FullPath = "/User/Desktop", Name = "Desktop" };
        _folderTreeViewModel.SelectedFolder = folder;
        Assert.Equal("/User/Desktop", _viewModel.SelectedFolderPath);
    }

    [Fact]
    public void ShouldShowEmptyState_True_WhenNoFolderAndNotLoading()
    {
        _fileListViewModel.IsLoadingFiles = false;
        _folderTreeViewModel.SelectedFolder = null;
        Assert.True(_viewModel.ShouldShowEmptyState);
    }

    [Fact]
    public void ShouldShowFileTable_True_WhenFolderSelectedAndNotLoading()
    {
        _fileListViewModel.IsLoadingFiles = false;
        _folderTreeViewModel.SelectedFolder = new FolderItem { FullPath = "/test", Name = "test" };
        Assert.True(_viewModel.ShouldShowFileTable);
    }

    [Fact]
    public async Task InitializeAsync_RestoresLastSortSessionId()
    {
        var sessionId = Guid.NewGuid();
        _operationStoreMock.Setup(x => x.GetConfigValueAsync(nameof(SortOperationViewModel.LastSortSessionId)))
            .ReturnsAsync(sessionId.ToString());

        var viewModel = new HomeViewModel(
            _operationStoreMock.Object,
            _folderTreeViewModel,
            _fileListViewModel,
            _sortOperationViewModel,
            _notificationViewModel,
            _sortReporterMock.Object
        );

        await Task.Delay(200); // Give async tasks a moment to run

        Assert.Equal(sessionId, _sortOperationViewModel.LastSortSessionId);
    }

    [Fact]
    public void PropertyChangeEvents_UpdateMainViewModelProperties()
    {
        bool emptyStateChanged = false;
        bool fileTableChanged = false;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(HomeViewModel.ShouldShowEmptyState))
                emptyStateChanged = true;
            if (e.PropertyName == nameof(HomeViewModel.ShouldShowFileTable))
                fileTableChanged = true;
        };

        _fileListViewModel.IsLoadingFiles = false;
        _folderTreeViewModel.SelectedFolder = new FolderItem { FullPath = "/test", Name = "test" };

        Assert.True(emptyStateChanged);
        Assert.True(fileTableChanged);
    }

    [Fact]
    public void AppVersion_ContainsVersionAndAuthor()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var expected = $"FileTidy v{version} | Built by Freemancodz";

        Assert.Equal(expected, _viewModel.AppVersion);
    }

    [Fact]
    public void NotificationRequested_ShowsNotification()
    {
        _sortReporterMock.Raise(
            x => x.NotificationRequested += null,
            "Test Title",
            "Test Message"
        );

        Assert.Equal("Test Title", _notificationViewModel.NotificationTitle);
        Assert.Equal("Test Message", _notificationViewModel.NotificationMessage);
        Assert.True(_notificationViewModel.IsNotificationVisible);
    }

    [Fact]
    public void ProgressChanged_UpdatesSortOperationProgress()
    {
        _sortReporterMock.Raise(x => x.ProgressChanged += null, 67);
        Assert.Equal(67, _sortOperationViewModel.OperationProgress);
    }

}
