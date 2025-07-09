using System.Collections.ObjectModel;
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

    
}