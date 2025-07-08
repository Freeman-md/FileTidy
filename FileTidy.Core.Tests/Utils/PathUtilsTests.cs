using FileTidy.Core.Utils;
using Xunit;

namespace FileTidy.Core.Tests.Utils;

public class PathUtilsTests : IDisposable
{
    private readonly string _tempDir;

    public PathUtilsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FileTidyUtils_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void GetUniqueFilePath_ReturnsSamePath_WhenFileDoesNotExist()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "unique.txt");

        // Act
        var result = PathUtils.GetUniqueFilePath(filePath);

        // Assert
        Assert.Equal(filePath, result);
    }

    [Fact]
    public void GetUniqueFilePath_AppendsSuffix_WhenFileExists()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "file.txt");
        File.WriteAllText(filePath, "dummy");

        // Act
        var result = PathUtils.GetUniqueFilePath(filePath);

        // Assert
        Assert.EndsWith("_1.txt", result);
        Assert.NotEqual(filePath, result);
    }

    [Fact]
    public void GetUniqueFilePath_AppendsIncrementingSuffix_WhenMultipleFilesExist()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "doc.txt");
        File.WriteAllText(filePath, "v0");
        File.WriteAllText(Path.Combine(_tempDir, "doc_1.txt"), "v1");
        File.WriteAllText(Path.Combine(_tempDir, "doc_2.txt"), "v2");

        // Act
        var result = PathUtils.GetUniqueFilePath(filePath);

        // Assert
        Assert.EndsWith("_3.txt", result);
    }

    [Fact]
    public void NormalizePath_RemovesTrailingSlashesAndDotSegments_CrossPlatform()
    {
        // Arrange
        var rawPath = Path.Combine(_tempDir, ".", "folder", "sub") + Path.DirectorySeparatorChar;

        // Act
        var result = PathUtils.NormalizePath(rawPath);

        // Assert
        Assert.False(result.EndsWith("/"), "Path should not end with a slash");
        Assert.DoesNotContain("/./", result); // Path should not contain redundant './'
        Assert.Contains("folder/sub", result.Replace("\\", "/")); // Normalize for assert
    }

    [Fact]
    public void NormalizePath_LeavesCleanPathUnchanged()
    {
        var cleanPath = Path.Combine(_tempDir, "folder", "file.txt");

        var result = PathUtils.NormalizePath(cleanPath);

        Assert.Equal(Path.GetFullPath(cleanPath).Replace("\\", "/"), result);
    }


}
