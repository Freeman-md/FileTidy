using FileTidy.Core.Interfaces;
using FileTidy.GUI.Contracts;
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
}