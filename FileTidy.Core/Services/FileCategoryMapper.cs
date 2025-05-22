using System.Text.Json;
using FileTidy.Core.Models;

namespace FileTidy.Core.Services;

public class FileCategoryMapper
{
    private readonly Dictionary<string, string> _fileCategories = new();
    private readonly string _dataDirectory;

    public FileCategoryMapper(string dataDirectory)
    {
        _dataDirectory = dataDirectory;

        LoadCategories();
    }

    private void LoadCategories()
    {
        string extensionsFilePath = Path.Combine(_dataDirectory, "extensions.json");
        string categoriesFilePath = Path.Combine(_dataDirectory, "categories.json");

        if (!File.Exists(extensionsFilePath)) throw new FileNotFoundException($"extensions.json file not found at {extensionsFilePath}");
        if (!File.Exists(categoriesFilePath)) throw new FileNotFoundException($"categories.json file not found at {categoriesFilePath}");

        string extensionsJson = File.ReadAllText(extensionsFilePath);
        string categoriesJson = File.ReadAllText(categoriesFilePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        List<Extension>? extensions = JsonSerializer.Deserialize<List<Extension>>(extensionsJson, options)
            ?? throw new InvalidOperationException("Deserialization of extensions.json returned null");

        List<Category>? categories = JsonSerializer.Deserialize<List<Category>>(categoriesJson, options)
            ?? throw new InvalidOperationException("Deserialization of categories.json returned null");

        Dictionary<int, string> categoryLookUp = categories.ToDictionary(category => category.Id, category => category.Name);

        foreach (var extension in extensions)
        {
            if (categoryLookUp.TryGetValue(extension.CategoryId, out string? categoryName))
            {
                if (!string.IsNullOrEmpty(categoryName))
                {
                    _fileCategories[extension.Name.ToLower()] = categoryName;
                }
            }
        }
    }

    public string GetCategory(string extension)
    {
        return _fileCategories.TryGetValue(extension.ToLower(), out string? category) ? category : "Others";
    }

    public IEnumerable<string> GetAllCategoryNames() => _fileCategories.Values.Distinct();
}
