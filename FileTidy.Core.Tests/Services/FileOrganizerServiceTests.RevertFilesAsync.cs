using FileTidy.Core.Models;
using Moq;

namespace FileTidy.Core.Tests.Services;

public partial class FileOrganizerServiceTests
{
    [Fact]
    public async Task RevertFilesAsync_RevertsMultipleFilesCorrectly()
    {
        // Arrange
        var files = new[] { "C:\\Sorted\\file1.txt", "C:\\Sorted\\file2.txt" };

        var op1 = new FileOperation
        {
            Id = Guid.NewGuid(),
            FileName = "file1.txt",
            OriginalPath = "C:\\Original\\file1.txt",
            NewPath = files[0],
            Status = FileOperationStatus.Moved
        };

        var op2 = new FileOperation
        {
            Id = Guid.NewGuid(),
            FileName = "file2.txt",
            OriginalPath = "C:\\Original\\file2.txt",
            NewPath = files[1],
            Status = FileOperationStatus.Moved
        };

        _storeMock.Setup(m => m.GetLatestNonRevertedOperationByNewPathAsync(files[0], FileOperationStatus.Moved)).ReturnsAsync(op1);
        _storeMock.Setup(m => m.GetLatestNonRevertedOperationByNewPathAsync(files[1], FileOperationStatus.Moved)).ReturnsAsync(op2);

        _opMock.Setup(m => m.RevertFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storeMock.Setup(m => m.UpdateOperationStatusAsync(It.IsAny<Guid>(), FileOperationStatus.Reverted))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RevertFilesAsync(files);

        // Assert
        _opMock.Verify(m => m.RevertFileAsync(op1.NewPath, op1.OriginalPath, It.IsAny<CancellationToken>()), Times.Once);
        _opMock.Verify(m => m.RevertFileAsync(op2.NewPath, op2.OriginalPath, It.IsAny<CancellationToken>()), Times.Once);

        _reporterMock.Verify(m => m.OnFileReverted(op1.NewPath), Times.Once);
        _reporterMock.Verify(m => m.OnFileReverted(op2.NewPath), Times.Once);

        _reporterMock.Verify(m => m.OnBulkRevertSummary(2, 2, 0), Times.Once);
    }
    
    [Fact]
    public async Task RevertFilesAsync_SkipsMissingOperations()
    {
        // Arrange
        var existingFile = "C:\\Sorted\\file1.txt";
        var missingFile = "C:\\Sorted\\ghost.txt";

        var op = new FileOperation
        {
            Id = Guid.NewGuid(),
            FileName = "file1.txt",
            OriginalPath = "C:\\Original\\file1.txt",
            NewPath = existingFile,
            Status = FileOperationStatus.Moved
        };

        _storeMock.Setup(m => m.GetLatestNonRevertedOperationByNewPathAsync(existingFile, FileOperationStatus.Moved)).ReturnsAsync(op);
        _storeMock.Setup(m => m.GetLatestNonRevertedOperationByNewPathAsync(missingFile, FileOperationStatus.Moved)).ReturnsAsync((FileOperation?)null);

        _opMock.Setup(m => m.RevertFileAsync(op.NewPath, op.OriginalPath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storeMock.Setup(m => m.UpdateOperationStatusAsync(op.Id, FileOperationStatus.Reverted))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RevertFilesAsync(new[] { existingFile, missingFile });

        // Assert
        _reporterMock.Verify(m => m.OnFileReverted(op.NewPath), Times.Once);
        _reporterMock.Verify(m => m.OnError(missingFile, It.Is<Exception>(e => e.Message.Contains("No active operation"))), Times.Once);
        _reporterMock.Verify(m => m.OnBulkRevertSummary(2, 1, 1), Times.Once);
    }

 
}