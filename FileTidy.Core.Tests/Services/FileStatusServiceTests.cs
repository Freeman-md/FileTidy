using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.Core.Services;
using Moq;
using Xunit;

namespace FileTidy.Core.Tests.Services;

public class FileStatusServiceTests
{
    private readonly Mock<IFileOperationStore> _storeMock = new();
    private readonly FileStatusService _service;

    public FileStatusServiceTests()
    {
        _service = new FileStatusService(_storeMock.Object);
    }

    [Fact]
    public async Task GetFileStatusesForDirectoryAsync_ReturnsLatestStatusPerFile()
    {
        // Arrange
        var folderPath = "C:\\Sorted";

        var operations = new List<FileOperation>
        {
            new() {
                FileName = "file1.txt",
                OriginalPath = "C:\\Unsorted\\file1.txt",
                NewPath = "C:\\Sorted\\file1.txt",
                Status = FileOperationStatus.Moved,
                Timestamp = DateTime.UtcNow.AddMinutes(-10)
            },
            new() {
                FileName = "file1.txt",
                OriginalPath = "C:\\Unsorted\\file1.txt",
                NewPath = "C:\\Sorted\\file1.txt",
                Status = FileOperationStatus.Reverted,
                Timestamp = DateTime.UtcNow
            },
            new() {
                FileName = "file2.txt",
                OriginalPath = "C:\\Unsorted\\file2.txt",
                NewPath = "C:\\Sorted\\file2.txt",
                Status = FileOperationStatus.Moved,
                Timestamp = DateTime.UtcNow
            }
        };

        _storeMock.Setup(m => m.GetFileOperationsInDirectoryAsync(folderPath)).ReturnsAsync(operations);

        // Act
        var result = await _service.GetFileStatusesForDirectoryAsync(folderPath);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(FileOperationStatus.Reverted, result["C:\\Sorted\\file1.txt"]);
        Assert.Equal(FileOperationStatus.Moved, result["C:\\Sorted\\file2.txt"]);
    }

    [Fact]
    public async Task GetFileStatusesForDirectoryAsync_ReturnsEmptyDictionary_WhenNoOperationsExist()
    {
        // Arrange
        var folderPath = "C:\\Empty";

        _storeMock.Setup(m => m.GetFileOperationsInDirectoryAsync(folderPath)).ReturnsAsync(new List<FileOperation>());

        // Act
        var result = await _service.GetFileStatusesForDirectoryAsync(folderPath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
