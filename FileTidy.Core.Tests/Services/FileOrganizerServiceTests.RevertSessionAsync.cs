using FileTidy.Core.Models;
using Moq;

namespace FileTidy.Core.Tests.Services;

public partial class FileOrganizerServiceTests
{
    [Fact]
    public async Task RevertSessionAsync_RevertsAllSuccessfully_ReportsSummary()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var operations = new List<FileOperation>
        {
            new() { Id = Guid.NewGuid(), OriginalPath = "C:\\Original\\a.txt", NewPath = "C:\\Sorted\\a.txt", Status = FileOperationStatus.Moved, FileName = "a.txt"},
            new() { Id = Guid.NewGuid(), OriginalPath = "C:\\Original\\b.txt", NewPath = "C:\\Sorted\\b.txt", Status = FileOperationStatus.Moved, FileName = "b.txt"}
        };

        _storeMock.Setup(m => m.GetOperationsBySessionAsync(sessionId)).ReturnsAsync(operations);

        _opMock.Setup(m => m.RevertFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storeMock.Setup(m => m.UpdateOperationStatusAsync(It.IsAny<Guid>(), FileOperationStatus.Reverted))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RevertSessionAsync(sessionId);

        // Assert
        _opMock.Verify(m => m.RevertFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _reporterMock.Verify(m => m.OnFileReverted(It.IsAny<string>()), Times.Exactly(2));
        _reporterMock.Verify(m => m.OnBulkRevertSummary(2, 2, 0), Times.Once);
        _reporterMock.Verify(m => m.OnSessionReverted(sessionId), Times.Once);
    }
    
    [Fact]
    public async Task RevertSessionAsync_HandlesPartialFailures()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var op1 = new FileOperation { Id = Guid.NewGuid(), OriginalPath = "C:\\O\\a.txt", NewPath = "C:\\S\\a.txt", Status = FileOperationStatus.Moved, FileName = "a.txt"};
        var op2 = new FileOperation { Id = Guid.NewGuid(), OriginalPath = "C:\\O\\b.txt", NewPath = "C:\\S\\b.txt", Status = FileOperationStatus.Moved, FileName = "b.txt"};

        _storeMock.Setup(m => m.GetOperationsBySessionAsync(sessionId)).ReturnsAsync(new[] { op1, op2 });

        _opMock.Setup(m => m.RevertFileAsync(op1.NewPath, op1.OriginalPath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _opMock.Setup(m => m.RevertFileAsync(op2.NewPath, op2.OriginalPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Simulated failure"));

        _storeMock.Setup(m => m.UpdateOperationStatusAsync(op1.Id, FileOperationStatus.Reverted))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RevertSessionAsync(sessionId);

        // Assert
        _reporterMock.Verify(m => m.OnFileReverted(op1.NewPath), Times.Once);
        _reporterMock.Verify(m => m.OnError(op2.NewPath, It.IsAny<IOException>()), Times.Once);
        _reporterMock.Verify(m => m.OnBulkRevertSummary(2, 1, 1), Times.Once);
        _reporterMock.Verify(m => m.OnSessionReverted(sessionId), Times.Once);
    }
    
    [Fact]
    public async Task RevertSessionAsync_WhenNoOperations_ReportsEmpty()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _storeMock.Setup(m => m.GetOperationsBySessionAsync(sessionId)).ReturnsAsync(Array.Empty<FileOperation>());

        // Act
        await _service.RevertSessionAsync(sessionId);

        // Assert
        _reporterMock.Verify(m => m.OnBulkRevertSummary(0, 0, 0), Times.Once);
        _reporterMock.Verify(m => m.OnSessionReverted(It.IsAny<Guid>()), Times.Never);
        _opMock.Verify(m => m.RevertFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }



}