using FileTidy.Core.Models;
using FileTidy.Core.Services;
using System.Text.Json;

namespace FileTidy.Core.Tests.Services;

public class FileCategoryServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly List<string> _tempDirs = new();

    public FileCategoryServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "FileTidyTest_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);
    }

    public FileCategoryService CreateService(
        Dictionary<string, int> extensions,
        Dictionary<int, string> categories
    )
    {
        var dataDir = SetupTestData(extensions, categories);
        return new FileCategoryService(dataDir);
    }

    private string SetupTestData(Dictionary<string, int> extensions, Dictionary<int, string> categories)
    {
        var dataDir = Path.Combine(_tempRoot, Guid.NewGuid().ToString(), "Data");
        Directory.CreateDirectory(dataDir);
        _tempDirs.Add(Path.GetDirectoryName(dataDir)!);

        var extensionsList = extensions.Select(e => new Extension { Name = e.Key, CategoryId = e.Value }).ToList();
        var categoriesList = categories.Select(c => new Category { Id = c.Key, Name = c.Value }).ToList();

        File.WriteAllText(Path.Combine(dataDir, "extensions.json"), JsonSerializer.Serialize(extensionsList));
        File.WriteAllText(Path.Combine(dataDir, "categories.json"), JsonSerializer.Serialize(categoriesList));

        return dataDir;
    }
    
    [Fact]
    public void GetCategory_KnownExtension_ReturnsCorrectCategory()
    {
        // Arrange
        var extensions = new Dictionary<string, int> { { ".txt", 1 }, { ".pdf", 1 } };
        var categories = new Dictionary<int, string> { { 1, "Documents" } };

        var service = CreateService(extensions, categories);

        // Act
        var result = service.GetCategory(".pdf");

        // Assert
        Assert.Equal("Documents", result);
    }
    
    [Fact]
    public void GetCategory_UnknownExtension_ReturnsOthers()
    {
        var extensions = new Dictionary<string, int> { { ".mp3", 2 } };
        var categories = new Dictionary<int, string> { { 2, "Audio" } };

        var service = CreateService(extensions, categories);

        var result = service.GetCategory(".zip");

        Assert.Equal("Others", result);
    }

    [Fact]
    public void GetAllCategoryNames_ReturnsDistinctCategoryNames()
    {
        var extensions = new Dictionary<string, int> { { ".mp3", 2 } };
        var categories = new Dictionary<int, string>
        {
            { 1, "Images" },
            { 2, "Audio" },
            { 3, "Images" },
            
        };
        
        var service = CreateService(extensions, categories);
        
        var result = service.GetAllCategoryNames();
        
        Assert.Equal(2, result.Count());
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch { /* Ignore cleanup errors */ }
        }
    }
}