using Moq;

namespace FileTidy.Core.Tests.Services;

public partial class FileOrganizerServiceTests
{
    [Fact]
    public async Task DeleteFilesAsync_DeletesAllFilesSuccessfully_ReportsSummary()
    {
        // Arrange
        var files = new[] { "C:\\Sorted\\file1.txt", "C:\\Sorted\\file2.txt" };

        _opMock.Setup(m => m.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteFilesAsync(files);

        // Assert
        _opMock.Verify(m => m.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _reporterMock.Verify(m => m.OnFileDeleted(It.IsAny<string>()), Times.Exactly(2));
        _reporterMock.Verify(m => m.OnBulkDeleteSummary(2, 2, 0), Times.Once);
    }
    
    [Fact]
    public async Task DeleteFilesAsync_SomeFailures_ReportMixedSummary()
    {
        // Arrange
        var goodFile = "C:\\Sorted\\file1.txt";
        var badFile = "C:\\Sorted\\locked.txt";

        _opMock.Setup(m => m.DeleteFileAsync(goodFile, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _opMock.Setup(m => m.DeleteFileAsync(badFile, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Mock failure"));

        // Act
        await _service.DeleteFilesAsync(new[] { goodFile, badFile });

        // Assert
        _reporterMock.Verify(m => m.OnFileDeleted(goodFile), Times.Once);
        _reporterMock.Verify(m => m.OnError(badFile, It.IsAny<IOException>()), Times.Once);
        _reporterMock.Verify(m => m.OnBulkDeleteSummary(2, 1, 1), Times.Once);
    }


}