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
    
    [Fact]
    public async Task MoveFileAsync_FileIsAlreadyInTarget_ReturnsSkipped()
    {
        // Arrange
        var category = "Images";
        var filePath = CreateTestFile(Path.Combine(category, "photo.jpg"));

        // Act
        var result = await _service.MoveFileAsync(filePath, category, _testRoot);

        // Assert
        Assert.Equal(FileOperationStatus.Skipped, result.Status);
        Assert.True(File.Exists(filePath));
        Assert.Equal(filePath, result.NewPath);
    }
    
    [Fact]
    public async Task MoveFileAsync_WhenFileAlreadyExists_AppendsUniqueSuffix()
    {
        // Arrange
        var original = CreateTestFile("Downloads/report.txt");
        var category = "Docs";

        // Create destination file with same name
        var destinationFolder = Path.Combine(_testRoot, category, "Downloads");
        Directory.CreateDirectory(destinationFolder);
        var existingPath = Path.Combine(destinationFolder, "report.txt");
        File.WriteAllText(existingPath, "existing content");

        // Act
        var result = await _service.MoveFileAsync(original, category, _testRoot);

        // Assert
        Assert.Equal(FileOperationStatus.Moved, result.Status);
        Assert.True(File.Exists(result.NewPath));
        Assert.NotEqual(existingPath, result.NewPath); // Unique path should be different
        Assert.True(result.NewPath.Contains("report"));
    }

    [Fact]
    public async Task MoveFileAsync_WhenFileDoesNotExist_ReturnsFailed()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testRoot, "Missing/file.txt");
        var category = "Invalid";

        // Act
        var result = await _service.MoveFileAsync(nonExistentFile, category, _testRoot);

        // Assert
        Assert.Equal(FileOperationStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("Could not find", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task RevertFileAsync_MovesFileBackToOriginalLocation()
    {
        // Arrange
        var originalPath = CreateTestFile("original/file.txt");
        var newPath = Path.Combine(_testRoot, "moved", "file.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
        File.Move(originalPath, newPath);

        // Act
        await _service.RevertFileAsync(newPath, originalPath);

        // Assert
        Assert.True(File.Exists(originalPath));
        Assert.False(File.Exists(newPath));
    }
    
    [Fact]
    public async Task RevertFileAsync_DeletesConflictingFileBeforeReverting()
    {
        // Arrange
        var originalPath = CreateTestFile("original/file.txt", "original content");
        var newPath = Path.Combine(_testRoot, "moved", "file.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
        File.WriteAllText(newPath, "new version");

        // Create conflicting file at original path
        File.WriteAllText(originalPath, "conflict");

        // Act
        await _service.RevertFileAsync(newPath, originalPath);

        // Assert
        Assert.True(File.Exists(originalPath));
        Assert.False(File.Exists(newPath));
        Assert.Equal("new version", File.ReadAllText(originalPath));
    }

    [Fact]
    public async Task DeleteFileAsync_DeletesFileSuccessfully()
    {
        // Arrange
        var filePath = CreateTestFile("trash/me.txt");
        Assert.True(File.Exists(filePath));

        // Act
        await _service.DeleteFileAsync(filePath);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task RemoveEmptyDirectoriesAsync_RemovesNestedEmptyDirs()
    {
        // Arrange
        var nestedDir = Path.Combine(_testRoot, "A/B/C");
        Directory.CreateDirectory(nestedDir);
        Assert.True(Directory.Exists(nestedDir));

        // Act
        await _service.RemoveEmptyDirectoriesAsync(_testRoot);

        // Assert
        Assert.False(Directory.Exists(nestedDir));
        Assert.False(Directory.Exists(Path.Combine(_testRoot, "A")));
    }

    [Fact]
    public async Task RemoveEmptyDirectoriesAsync_LeavesNonEmptyDirs()
    {
        // Arrange
        var nestedDir = Path.Combine(_testRoot, "X/Y/Z");
        Directory.CreateDirectory(nestedDir);
        File.WriteAllText(Path.Combine(nestedDir, "keep.txt"), "not empty");

        // Act
        await _service.RemoveEmptyDirectoriesAsync(_testRoot);

        // Assert
        Assert.True(Directory.Exists(nestedDir)); // Still exists because it had a file
    }



}