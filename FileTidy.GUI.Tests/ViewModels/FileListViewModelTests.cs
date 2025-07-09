using System.Collections.ObjectModel;
using System.ComponentModel;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;
using FileTidy.GUI.ViewModels;
using Moq;

namespace FileTidy.Gui.Tests.ViewModels;

public class FileListViewModelTests
{
    private readonly Mock<IFolderService> _folderServiceMock = new();
    private readonly Mock<IFileStatusService> _fileStatusServiceMock = new();
    private readonly Mock<IFileOrganizerService> _fileOrganizerServiceMock = new();
    private readonly FolderTreeViewModel _folderTreeViewModel;
    private readonly FileListViewModel _viewModel;

    public FileListViewModelTests()
    {
        var dummyFolderService = new Mock<IFolderService>().Object;
        _folderTreeViewModel = new FolderTreeViewModel(dummyFolderService, _ => { });

        _viewModel = new FileListViewModel(
            _folderTreeViewModel,
            _folderServiceMock.Object,
            _fileStatusServiceMock.Object,
            _fileOrganizerServiceMock.Object,
            _ => { }
        );
    }

    [Fact]
    public void SelectingFile_UpdatesSelectionStateAndTriggersComputedProperties()
    {
        // Arrange
        var file1 = new FileItem
        {
            Name = "test.txt",
            FullPath = "/User/test.txt",
            Type = "txt",
            Size = 100,
            Modified = "Now",
            IsSelected = false,
            FileOperationStatus = FileOperationStatus.Unprocessed
        };

        var file2 = new FileItem
        {
            Name = "doc.pdf",
            FullPath = "/User/doc.pdf",
            Type = "pdf",
            Size = 200,
            Modified = "Now",
            IsSelected = false,
            FileOperationStatus = FileOperationStatus.Unprocessed
        };

        _viewModel.CurrentFiles = new ObservableCollection<FileItem>(new List<FileItem> { file1, file2 });

        // Act
        file1.IsSelected = true;

        // Assert
        Assert.True(file1.IsSelected);
        Assert.Equal(1, _viewModel.SelectedFileCount);
        Assert.True(_viewModel.CanDeleteSelected);
        Assert.False(_viewModel.CanRevertSelected); // none is Moved
    }

    [Fact]
    public void SelectAll_TogglesAllFilesCorrectly()
    {
        // Arrange
        var file1 = new FileItem { Name = "a.txt", Type = "txt", Size = 100, Modified = "Now", IsSelected = false };
        var file2 = new FileItem { Name = "b.txt", Type = "txt", Size = 100, Modified = "Now", IsSelected = false };

        HookPropertyChangedHandlers(file1);
        HookPropertyChangedHandlers(file2);

        _viewModel.CurrentFiles = new ObservableCollection<FileItem> { file1, file2 };

        // Act
        _viewModel.IsAllSelected = true;

        // Assert
        Assert.True(file1.IsSelected);
        Assert.True(file2.IsSelected);
        Assert.True(_viewModel.IsAllSelected);

        // Act again (deselect)
        _viewModel.IsAllSelected = false;

        // Assert
        Assert.False(file1.IsSelected);
        Assert.False(file2.IsSelected);
        Assert.False(_viewModel.IsAllSelected);
    }


    [Fact]
    public void DeselectSingleFile_UpdatesIsAllSelected()
    {
        // Arrange
        var file1 = new FileItem { Name = "a.txt", Type = "txt", Size = 100, Modified = "Now", IsSelected = true };
        var file2 = new FileItem { Name = "b.txt", Type = "txt", Size = 100, Modified = "Now", IsSelected = true };

        HookPropertyChangedHandlers(file1);
        HookPropertyChangedHandlers(file2);

        _viewModel.CurrentFiles = new ObservableCollection<FileItem> { file1, file2 };

        _viewModel.IsAllSelected = true;
        Assert.True(_viewModel.IsAllSelected);

        // Act
        file1.IsSelected = false;

        // Assert
        Assert.False(_viewModel.IsAllSelected);
    }

    [Fact]
    public async Task RevertSelected_CallsOrganizerServiceAndRemovesFiles()
    {
        // Arrange
        var file1 = new FileItem
        {
            Name = "moved.txt",
            FullPath = "/User/moved.txt",
            Type = "txt",
            Size = 300,
            Modified = "Now",
            IsSelected = true,
            FileOperationStatus = FileOperationStatus.Moved
        };

        var file2 = new FileItem
        {
            Name = "unmoved.txt",
            FullPath = "/User/unmoved.txt",
            Type = "txt",
            Size = 200,
            Modified = "Now",
            IsSelected = true,
            FileOperationStatus = FileOperationStatus.Unprocessed
        };

        HookPropertyChangedHandlers(file1);
        HookPropertyChangedHandlers(file2);

        _viewModel.CurrentFiles = new ObservableCollection<FileItem> { file1, file2 };
        _viewModel.FolderSize = 500;

        _fileOrganizerServiceMock.Setup(x => x.RevertFileAsync("/User/moved.txt", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _viewModel.RevertSelectedCommand.ExecuteAsync(null);

        // Assert
        _fileOrganizerServiceMock.Verify(x => x.RevertFileAsync("/User/moved.txt", default), Times.Once);
        Assert.DoesNotContain(file1, _viewModel.CurrentFiles);
        Assert.Contains(file2, _viewModel.CurrentFiles);
        Assert.Equal(200, _viewModel.FolderSize);
    }


    private void HookPropertyChangedHandlers(FileItem file)
    {
        file.PropertyChanged += (sender, e) =>
        {
            var method = typeof(FileListViewModel)
                .GetMethod("OnFileItemPropertyChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            method?.Invoke(_viewModel, new object[] { sender, e });
        };
    }


}