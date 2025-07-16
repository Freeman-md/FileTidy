using System.Collections.ObjectModel;
using System.ComponentModel;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;
using FileTidy.GUI.ViewModels;
using FileTidy.GUI.ViewModels.Home;
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
        _folderTreeViewModel = new FolderTreeViewModel(dummyFolderService, action => action());

        _viewModel = new FileListViewModel(
            _folderTreeViewModel,
            _folderServiceMock.Object,
            _fileStatusServiceMock.Object,
            _fileOrganizerServiceMock.Object,
            action => action()
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
    
    [Fact]
    public async Task DeleteSelected_CallsOrganizerServiceAndRemovesFiles()
    {
        // Arrange
        var file1 = new FileItem
        {
            Name = "delete1.txt",
            FullPath = "/User/delete1.txt",
            Type = "txt",
            Size = 100,
            Modified = "Now",
            IsSelected = true
        };

        var file2 = new FileItem
        {
            Name = "delete2.txt",
            FullPath = "/User/delete2.txt",
            Type = "txt",
            Size = 200,
            Modified = "Now",
            IsSelected = true
        };

        HookPropertyChangedHandlers(file1);
        HookPropertyChangedHandlers(file2);

        _viewModel.CurrentFiles = new ObservableCollection<FileItem> { file1, file2 };
        _viewModel.FolderSize = 300;

        _fileOrganizerServiceMock.Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);

        // Assert
        _fileOrganizerServiceMock.Verify(x => x.DeleteFileAsync(file1.FullPath!, default), Times.Once);
        _fileOrganizerServiceMock.Verify(x => x.DeleteFileAsync(file2.FullPath!, default), Times.Once);
        Assert.Empty(_viewModel.CurrentFiles);
        Assert.Equal(0, _viewModel.FolderSize);
    }

    [Fact]
    public async Task DeleteSelected_SkipsFolders()
    {
        // Arrange
        var folderItem = new FileItem
        {
            Name = "Folder1",
            FullPath = "/User/Folder1",
            Type = "FOLDER",
            Size = 0,
            Modified = "Now",
            IsSelected = true
        };

        var fileItem = new FileItem
        {
            Name = "file.txt",
            FullPath = "/User/file.txt",
            Type = "txt",
            Size = 100,
            Modified = "Now",
            IsSelected = true
        };

        HookPropertyChangedHandlers(folderItem);
        HookPropertyChangedHandlers(fileItem);

        _viewModel.CurrentFiles = new ObservableCollection<FileItem> { folderItem, fileItem };
        _viewModel.FolderSize = 100;

        _fileOrganizerServiceMock.Setup(x => x.DeleteFileAsync(fileItem.FullPath!, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);

        // Assert
        _fileOrganizerServiceMock.Verify(x => x.DeleteFileAsync(fileItem.FullPath!, default), Times.Once);
        _fileOrganizerServiceMock.Verify(x => x.DeleteFileAsync(It.Is<string>(s => s.Contains("Folder1")), default), Times.Never);

        Assert.DoesNotContain(fileItem, _viewModel.CurrentFiles);
        Assert.Contains(folderItem, _viewModel.CurrentFiles);
        Assert.Equal(0, _viewModel.FolderSize);
    }

    [Fact]
    public async Task LoadFilesForSelectedFolder_DoesNothing_WhenNoFolderSelected()
    {
        // Arrange
        _folderTreeViewModel.SelectedFolder = null;

        // Act
        await _viewModel.LoadFilesForSelectedFolder();

        // Assert
        Assert.Empty(_viewModel.CurrentFiles);
    }

    [Fact]
    public async Task SetFileItemsAsync_AppliesCorrectFileStatuses()
    {
        // Arrange
        var file1 = new FileItem
        {
            Name = "alpha.txt",
            FullPath = "/User/alpha.txt",
            Type = "txt",
            Size = 123,
            Modified = "Now"
        };

        var file2 = new FileItem
        {
            Name = "beta.txt",
            FullPath = "/User/beta.txt",
            Type = "txt",
            Size = 456,
            Modified = "Now"
        };

        _folderTreeViewModel.SelectedFolder = new FolderItem
        {
            Name = "User",
            FullPath = "/User"
        };

        var fileList = new List<FileItem> { file1, file2 };

        _folderServiceMock.Setup(x => x.LoadFilesAsync("/User"))
            .ReturnsAsync(fileList);

        _fileStatusServiceMock.Setup(x => x.GetFileStatusesForDirectoryAsync("/User"))
            .ReturnsAsync(new Dictionary<string, FileOperationStatus>
            {
                { "/User/alpha.txt", FileOperationStatus.Moved },
                { "/User/beta.txt", FileOperationStatus.Unprocessed }
            });

        // Act
        await _viewModel.LoadFilesForSelectedFolder();

        // Assert
        Assert.Equal(FileOperationStatus.Moved, _viewModel.CurrentFiles[0].FileOperationStatus);
        Assert.Equal(FileOperationStatus.Unprocessed, _viewModel.CurrentFiles[1].FileOperationStatus);
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