using FileTidy.Core.Models;
using FileTidy.Core.Services;
using Xunit;

namespace FileTidy.Core.Tests.Services;

public class FileOperationServiceTests : IDisposable
{
    private readonly string _testRoot;
    private readonly FileOperationService _service;

    public FileOperationServiceTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "FileOpTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_testRoot);

        _service = new FileOperationService();
    }

    private string CreateTestFile(string relativePath, string content = "test")
    {
        var fullPath = Path.Combine(_testRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            try { Directory.Delete(_testRoot, recursive: true); }
            catch { /* ignore cleanup errors */ }
        }
    }

    [Fact]
    public async Task MoveFileAsync_FileMovesSuccessfully_ReturnsMoved()
    {
        // Arrange
        var sourcePath = CreateTestFile("Original/file.txt");
        var category = "Documents";
        var destinationRoot = _testRoot;

        // Act
        var result = await _service.MoveFileAsync(sourcePath, category, destinationRoot);

        // Assert
        Assert.Equal(FileOperationStatus.Moved, result.Status);
        Assert.False(File.Exists(sourcePath));
        Assert.True(File.Exists(result.NewPath));
        Assert.Contains(category, result.NewPath);
    }
}