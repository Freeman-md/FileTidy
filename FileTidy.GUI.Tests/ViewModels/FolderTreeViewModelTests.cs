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
    
    [Fact]
    public async Task InitializeAsync_OverwritesTopLevelFoldersWithTreeStructure()
    {
        // Arrange
        var topLevelFolder = new FolderItem
        {
            Name = "Documents",
            FullPath = "/User/Documents"
        };

        var updatedSubfolder = new FolderItem
        {
            Name = "Reports",
            FullPath = "/User/Documents/Reports"
        };

        var folderTreeItem = new FolderItem
        {
            Name = "Documents",
            FullPath = "/User/Documents",
            SubFolders = new List<FolderItem> { updatedSubfolder }
        };

        var topFolders = new List<FolderItem> { topLevelFolder };
        var folderTree = new List<FolderItem> { folderTreeItem };

        _folderServiceMock.Setup(s => s.GetTopLevelFoldersAsync()).ReturnsAsync(topFolders);
        _folderServiceMock.Setup(s => s.GetFolderTreeAsync()).ReturnsAsync(folderTree);

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        var result = _viewModel.FolderTree.First(f => f.FullPath == "/User/Documents");
        
        Assert.Same(topLevelFolder.FullPath, result.FullPath);
        Assert.Single(result.SubFolders);
        Assert.Equal("/User/Documents/Reports", result.SubFolders.First().FullPath);
    }

    
    [Fact]
    public void SettingSelectedFolder_InvokesSelectedFolderChangedEvent()
    {
        // Arrange
        var folder = new FolderItem { Name = "Docs", FullPath = "/User/Docs" };
        FolderItem? invokedValue = null;
        _viewModel.SelectedFolderChanged += f => invokedValue = f;

        // Act
        _viewModel.SelectedFolder = folder;

        // Assert
        Assert.Equal(folder, invokedValue);
    }
    
    [Fact]
    public void SettingSelectedFolder_AlsoSetsSelectedRootFolderCorrectly()
    {
        // Arrange
        var root = new FolderItem { Name = "Root", FullPath = "/User" };
        var sub = new FolderItem { Name = "Docs", FullPath = "/User/Docs", Parent = root };

        _viewModel.TopLevelFolders.Add(root);

        // Act
        _viewModel.SelectedFolder = sub;

        // Assert
        Assert.Equal(root, _viewModel.SelectedRootFolder);
    }
    
    [Fact]
    public void SettingSelectedFolder_ToNull_ClearsSelectedRootFolder()
    {
        // Arrange
        var root = new FolderItem { Name = "Root", FullPath = "/User" };
        _viewModel.TopLevelFolders.Add(root);
        _viewModel.SelectedRootFolder = root;

        // Act
        _viewModel.SelectedFolder = null;

        // Assert
        Assert.Null(_viewModel.SelectedRootFolder);
    }

    [Fact]
    public void SettingSelectedRootFolder_UpdatesSelectedFolderToSameValue()
    {
        // Arrange
        var root = new FolderItem { Name = "Root", FullPath = "/User" };

        // Act
        _viewModel.SelectedRootFolder = root;

        // Assert
        Assert.Equal(root, _viewModel.SelectedFolder);
    }

}