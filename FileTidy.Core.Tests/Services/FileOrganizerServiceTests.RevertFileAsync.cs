using FileTidy.Core.Models;
using Moq;

namespace FileTidy.Core.Tests.Services;

public partial class FileOrganizerServiceTests
{
    [Fact]
    public async Task RevertFileAsync_WhenOperationExists_RevertsSuccessfully()
    {
        // Arrange
        var operation = new FileOperation
        {
            Id = Guid.NewGuid(),
            OriginalPath = "C:\\Original\\file.txt",
            NewPath = "C:\\Sorted\\Documents\\file.txt",
            Status = FileOperationStatus.Moved,
            FileName = "file.txt"
        };

        _storeMock.Setup(m => m.GetLatestNonRevertedOperationByNewPathAsync(operation.NewPath, FileOperationStatus.Moved))
            .ReturnsAsync(operation);

        _opMock.Setup(m => m.RevertFileAsync(operation.NewPath, operation.OriginalPath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storeMock.Setup(m => m.UpdateOperationStatusAsync(operation.Id, FileOperationStatus.Reverted))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RevertFileAsync(operation.NewPath);

        // Assert
        _opMock.Verify(m => m.RevertFileAsync(operation.NewPath, operation.OriginalPath, It.IsAny<CancellationToken>()), Times.Once);
        _storeMock.Verify(m => m.UpdateOperationStatusAsync(operation.Id, FileOperationStatus.Reverted), Times.Once);
        _reporterMock.Verify(m => m.OnError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
    }
    
    [Fact]
    public async Task RevertFileAsync_WhenOperationMissing_ReportsError()
    {
        // Arrange
        var missingPath = "C:\\Sorted\\Documents\\ghost.txt";

        _storeMock.Setup(m => m.GetLatestNonRevertedOperationByNewPathAsync(missingPath, FileOperationStatus.Moved))
            .ReturnsAsync((FileOperation?)null);

        // Act
        await _service.RevertFileAsync(missingPath);

        // Assert
        _reporterMock.Verify(m => m.OnError(
            missingPath,
            It.Is<InvalidOperationException>(ex =>
                ex.Message.Contains("No active operation found"))), Times.Once);

        _opMock.Verify(m => m.RevertFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }


}