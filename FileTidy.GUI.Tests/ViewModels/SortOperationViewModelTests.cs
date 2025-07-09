using System;
using System.Threading;
using System.Threading.Tasks;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;
using FileTidy.GUI.ViewModels;
using Moq;
using Xunit;

namespace FileTidy.Gui.Tests.ViewModels;

public class SortOperationViewModelTests
{
    private readonly Mock<IFileOrganizerService> _organizerServiceMock = new();
    private readonly Mock<IFileOperationStore> _operationStoreMock = new();
    private readonly FolderTreeViewModel _folderTreeViewModel;
    private readonly FileListViewModel _fileListViewModel;
    private readonly SortOperationViewModel _viewModel;

    public SortOperationViewModelTests()
    {
        var dummyFolderService = new Mock<IFolderService>().Object;

        _folderTreeViewModel = new FolderTreeViewModel(dummyFolderService, _ => { });
        _fileListViewModel = new FileListViewModel(
            _folderTreeViewModel,
            dummyFolderService,
            new Mock<IFileStatusService>().Object,
            _organizerServiceMock.Object,
            _ => { }
        );

        _viewModel = new SortOperationViewModel(
            _folderTreeViewModel,
            _fileListViewModel,
            _organizerServiceMock.Object,
            _operationStoreMock.Object
        );
    }

    [Fact]
    public async Task StartTidying_NoFolderSelected_DoesNothing()
    {
        // Arrange
        _folderTreeViewModel.SelectedFolder = null;

        // Act
        await _viewModel.StartTidyingCommand.ExecuteAsync(null);

        // Assert
        _organizerServiceMock.Verify(x => x.SortDirectoryAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(_viewModel.IsSorting);
    }
    
    [Fact]
    public async Task StartTidying_WithSelectedFolder_CallsOrganizerServiceAndUpdatesState()
    {
        // Arrange
        var folder = new FolderItem
        {
            Name = "Documents",
            FullPath = "/User/Documents"
        };
        _folderTreeViewModel.SelectedFolder = folder;

        var result = new TidyingResult
        {
            TotalMoved = 5,
            TotalErrors = 0
        };

        _organizerServiceMock
            .Setup(x => x.SortDirectoryAsync("/User/Documents", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        _operationStoreMock
            .Setup(x => x.SaveConfigValueAsync(nameof(_viewModel.LastSortSessionId), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.StartTidyingCommand.ExecuteAsync(null);

        // Assert
        _organizerServiceMock.Verify(x => x.SortDirectoryAsync("/User/Documents", It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _operationStoreMock.Verify(x => x.SaveConfigValueAsync(nameof(_viewModel.LastSortSessionId), It.IsAny<string>()), Times.Once);
        Assert.False(_viewModel.IsSorting);
        Assert.NotEqual(Guid.Empty, _viewModel.LastSortSessionId);
    }
    
    [Fact]
    public async Task StartTidying_WhenExceptionOccurs_SetsIsSortingToFalse()
    {
        // Arrange
        var folder = new FolderItem
        {
            Name = "Documents",
            FullPath = "/User/Documents"
        };
        _folderTreeViewModel.SelectedFolder = folder;

        _organizerServiceMock
            .Setup(x => x.SortDirectoryAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failure"));

        // Act
        await _viewModel.StartTidyingCommand.ExecuteAsync(null);

        // Assert
        Assert.False(_viewModel.IsSorting);
    }

    [Fact]
    public void StopSorting_WhenSortingIsActive_CancelsTokenAndUpdatesState()
    {
        // Arrange
        _viewModel.IsSorting = true;

        var cts = new CancellationTokenSource();
        typeof(SortOperationViewModel)
            .GetField("_sortingCancellationTokenSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(_viewModel, cts);

        // Act
        _viewModel.StopSortingCommand.Execute(null);

        // Assert
        Assert.True(cts.IsCancellationRequested);
        Assert.False(_viewModel.IsSorting);
        Assert.True(_viewModel.WasCancelled);
    }
    
    [Fact]
    public void StopSorting_WhenNoSortingInProgress_DoesNothing()
    {
        // Arrange
        _viewModel.IsSorting = false;

        // Act
        _viewModel.StopSortingCommand.Execute(null);

        // Assert
        Assert.False(_viewModel.IsSorting);
        Assert.Null(typeof(SortOperationViewModel)
            .GetField("_sortingCancellationTokenSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_viewModel));
    }

    [Fact]
    public async Task RevertLastSort_WhenSessionExists_CallsRevertAndClearsState()
    {
        // Arrange
        var testSessionId = Guid.NewGuid();
        _viewModel.LastSortSessionId = testSessionId;

        _organizerServiceMock
            .Setup(s => s.RevertSessionAsync(testSessionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _operationStoreMock
            .Setup(s => s.DeleteConfigValueAsync(nameof(_viewModel.LastSortSessionId)))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.RevertLastSortCommand.ExecuteAsync(null);

        // Assert
        _organizerServiceMock.Verify(s => s.RevertSessionAsync(testSessionId, default), Times.Once);
        _operationStoreMock.Verify(s => s.DeleteConfigValueAsync(nameof(_viewModel.LastSortSessionId)), Times.Once);
        Assert.Equal(Guid.Empty, _viewModel.LastSortSessionId);
        Assert.False(_viewModel.IsReverting);
    }
    
    [Fact]
    public async Task RevertLastSort_WhenSessionIdIsEmpty_DoesNothing()
    {
        // Arrange
        _viewModel.LastSortSessionId = Guid.Empty;

        // Act
        await _viewModel.RevertLastSortCommand.ExecuteAsync(null);

        // Assert
        _organizerServiceMock.Verify(s => s.RevertSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _operationStoreMock.Verify(s => s.DeleteConfigValueAsync(It.IsAny<string>()), Times.Never);
        Assert.Equal(Guid.Empty, _viewModel.LastSortSessionId);
        Assert.False(_viewModel.IsReverting);
    }



}