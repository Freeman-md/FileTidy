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