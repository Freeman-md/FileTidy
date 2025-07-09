using FileTidy.GUI.Contracts;
using FileTidy.GUI.ViewModels;
using Moq;

namespace FileTidy.Gui.Tests.ViewModels;

public class FolderTreeViewModelTests
{
    private readonly Mock<IFolderService> _folderServiceMock = new();
    private readonly FolderTreeViewModel _viewModel;

    public FolderTreeViewModelTests()
    {
        _viewModel = new FolderTreeViewModel(_folderServiceMock.Object);
    }
}