using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;
using FileTidy.GUI.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Moq;

namespace FileTidy.Gui.Tests.ViewModels;

public class FolderTreeViewModelTests
{
    private readonly Mock<IFolderService> _folderServiceMock = new();
    private readonly FolderTreeViewModel _viewModel;

    public FolderTreeViewModelTests()
    {
        _viewModel = new FolderTreeViewModel(_folderServiceMock.Object, action => action());
    }
    
    [Fact]
    public async Task InitializeAsync_LoadsFolderTreeAndTopLevelFolders()
    {
        // Arrange
        var topFolders = new List<FolderItem>
        {
            new() { Name = "Documents", FullPath = "C:/Users/Freeman/Documents" },
            new() { Name = "Downloads", FullPath = "C:/Users/Freeman/Downloads" }
        };

        var folderTree = new List<FolderItem>
        {
            new()
            {
                Name = "Documents",
                FullPath = "C:/Users/Freeman/Documents",
                SubFolders = new List<FolderItem>
                {
                    new() { Name = "Reports", FullPath = "C:/Users/Freeman/Documents/Reports" }
                }
            },
            new()
            {
                Name = "Downloads",
                FullPath = "C:/Users/Freeman/Downloads"
            }
        };

        _folderServiceMock.Setup(x => x.GetTopLevelFoldersAsync())
            .ReturnsAsync(topFolders);

        _folderServiceMock.Setup(x => x.GetFolderTreeAsync())
            .ReturnsAsync(folderTree);

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Equal(2, _viewModel.TopLevelFolders.Count);
        Assert.Equal(2, _viewModel.FolderTree.Count);

        var docsFolder = _viewModel.FolderTree.First(f => f.FullPath.Contains("Documents"));
        Assert.Single(docsFolder.SubFolders);
        Assert.Equal("C:/Users/Freeman/Documents/Reports", docsFolder.SubFolders[0].FullPath);

        _folderServiceMock.Verify(x => x.GetTopLevelFoldersAsync(), Times.Once);
        _folderServiceMock.Verify(x => x.GetFolderTreeAsync(), Times.Once);
    }


}