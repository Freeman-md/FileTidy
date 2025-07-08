using FileTidy.Core.Models;
using Moq;

namespace FileTidy.Core.Tests.Services;

public partial class FileOrganizerServiceTests
{
    [Fact]
    public async Task SortDirectoryAsync_ProcessesAndReportsCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "FileTidyTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var testFiles = new[]
        {
            Path.Combine(tempDir, "file1.txt"),
            Path.Combine(tempDir, "file2.pdf"),
            Path.Combine(tempDir, "file3.txt")
        };

        foreach (var path in testFiles)
            File.WriteAllText(path, "dummy");

        _categoryMock.Setup(m => m.GetCategory(".txt")).Returns("Documents");
        _categoryMock.Setup(m => m.GetCategory(".pdf")).Returns("Documents");
        _categoryMock.Setup(m => m.GetAllCategoryNames()).Returns(new[] { "Documents" });

        _opMock.Setup(m => m.MoveFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((string file, string cat, string baseDir, CancellationToken _) => new FileMoveResult
               {
                   OriginalPath = file,
                   NewPath = Path.Combine(baseDir, cat, Path.GetFileName(file)),
                   Status = FileOperationStatus.Moved
               });

        _storeMock.Setup(m => m.LogOperationAsync(It.IsAny<FileOperation>()))
                  .Returns(Task.CompletedTask);

        var sessionId = Guid.NewGuid();

        // Act
        var result = await _service.SortDirectoryAsync(tempDir, sessionId);

        // Assert
        Assert.Equal(3, result.TotalFiles);
        Assert.Equal(3, result.TotalMoved);
        Assert.Equal(0, result.TotalErrors);
        Assert.True(result.PerCategoryCounts.ContainsKey("Documents"));
        Assert.Equal(3, result.PerCategoryCounts["Documents"]);

        _reporterMock.Verify(m => m.OnFileProcessed(It.IsAny<string>(), "Documents"), Times.Exactly(3));
        _reporterMock.Verify(m => m.OnSummary(3, 3, 0, It.IsAny<Dictionary<string, int>>()), Times.Once);

        // Cleanup
        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public async Task SortDirectoryAsync_SkipsAlreadySortedFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "FileTidyTest_" + Guid.NewGuid());
        var category = "Documents";
        var categoryFolder = Path.Combine(tempDir, category);
        Directory.CreateDirectory(categoryFolder);

        var skippedFile = Path.Combine(categoryFolder, "already_sorted.txt");
        File.WriteAllText(skippedFile, "dummy");

        _categoryMock.Setup(m => m.GetAllCategoryNames()).Returns(new[] { category });

        var sessionId = Guid.NewGuid();

        // Act
        var result = await _service.SortDirectoryAsync(tempDir, sessionId);

        // Assert
        Assert.Equal(0, result.TotalFiles);
        Assert.Equal(0, result.TotalMoved);
        Assert.Equal(0, result.TotalErrors);

        _reporterMock.Verify(m => m.OnSummary(0, 0, 0, It.IsAny<Dictionary<string, int>>()), Times.Never);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task SortDirectoryAsync_HandlesMoveFailuresGracefully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "FileTidyTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var file = Path.Combine(tempDir, "fail.docx");
        File.WriteAllText(file, "dummy");

        _categoryMock.Setup(m => m.GetCategory(".docx")).Returns("Documents");
        _categoryMock.Setup(m => m.GetAllCategoryNames()).Returns(new[] { "Documents" });

        _opMock.Setup(m => m.MoveFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new FileMoveResult
               {
                   OriginalPath = file,
                   NewPath = file,
                   Status = FileOperationStatus.Failed,
                   Error = new IOException("Mock failure")
               });

        var sessionId = Guid.NewGuid();

        // Act
        var result = await _service.SortDirectoryAsync(tempDir, sessionId);

        // Assert
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(0, result.TotalMoved);
        Assert.Equal(1, result.TotalErrors);

        _reporterMock.Verify(m => m.OnError(file, It.IsAny<Exception>()), Times.Once);
        _reporterMock.Verify(m => m.OnSummary(1, 0, 1, It.IsAny<Dictionary<string, int>>()), Times.Once);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task SortDirectoryAsync_WhenNoFiles_ReturnsEmptyResult()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "FileTidyTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        _categoryMock.Setup(m => m.GetAllCategoryNames()).Returns(Array.Empty<string>());

        var sessionId = Guid.NewGuid();

        // Act
        var result = await _service.SortDirectoryAsync(tempDir, sessionId);

        // Assert
        Assert.Equal(0, result.TotalFiles);
        Assert.Equal(0, result.TotalMoved);
        Assert.Equal(0, result.TotalErrors);

        _reporterMock.Verify(m => m.OnSummary(0, 0, 0, It.IsAny<Dictionary<string, int>>()), Times.Never);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task SortDirectoryAsync_LogsCorrectCategoryCounts()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "FileTidyTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var files = new[]
        {
        Path.Combine(tempDir, "a.txt"),
        Path.Combine(tempDir, "b.mp3"),
        Path.Combine(tempDir, "c.txt"),
        Path.Combine(tempDir, "d.jpg"),
    };

        foreach (var path in files)
            File.WriteAllText(path, "dummy");

        _categoryMock.Setup(m => m.GetAllCategoryNames()).Returns(new[] { "Documents", "Audio", "Images" });
        _categoryMock.Setup(m => m.GetCategory(".txt")).Returns("Documents");
        _categoryMock.Setup(m => m.GetCategory(".mp3")).Returns("Audio");
        _categoryMock.Setup(m => m.GetCategory(".jpg")).Returns("Images");

        _opMock.Setup(m => m.MoveFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((string file, string category, string dir, CancellationToken _) =>
                   new FileMoveResult
                   {
                       OriginalPath = file,
                       NewPath = Path.Combine(dir, category, Path.GetFileName(file)),
                       Status = FileOperationStatus.Moved
                   });

        _storeMock.Setup(m => m.LogOperationAsync(It.IsAny<FileOperation>()))
                  .Returns(Task.CompletedTask);

        var sessionId = Guid.NewGuid();

        // Act
        var result = await _service.SortDirectoryAsync(tempDir, sessionId);

        // Assert
        Assert.Equal(4, result.TotalFiles);
        Assert.Equal(4, result.TotalMoved);
        Assert.Equal(0, result.TotalErrors);

        Assert.Equal(2, result.PerCategoryCounts["Documents"]);
        Assert.Equal(1, result.PerCategoryCounts["Audio"]);
        Assert.Equal(1, result.PerCategoryCounts["Images"]);

        Directory.Delete(tempDir, true);
    }

}