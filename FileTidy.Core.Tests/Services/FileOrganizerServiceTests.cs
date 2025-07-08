using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.Core.Services;
using Moq;

namespace FileTidy.Core.Tests.Services;

public partial class FileOrganizerServiceTests
{
    private readonly Mock<IFileOperationStore> _storeMock = new();
    private readonly Mock<IFileCategoryService> _categoryMock = new();
    private readonly Mock<IFileOperationService> _opMock = new();
    private readonly Mock<ISortReporter> _reporterMock = new();

    private readonly FileOrganizerService _service;

    public FileOrganizerServiceTests()
    {
        _service = new FileOrganizerService(
            _storeMock.Object,
            _categoryMock.Object,
            _opMock.Object,
            _reporterMock.Object
        );
    }

}
