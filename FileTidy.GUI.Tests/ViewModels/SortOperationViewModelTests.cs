using System;
using System.Threading;
using System.Threading.Tasks;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.GUI.Contracts;
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

    // Individual tests go here
}