using System.Text.Json;
using FileTidy.Models;

namespace FileTidy.Services;

public class FileCategoryMapper
{
    private readonly Dictionary<string, string> _fileCategories = new();

    public FileCategoryMapper()
    {
        LoadCategories();
    }

    private void LoadCategories()
    {
        string rootDirectory = Directory.GetCurrentDirectory();

        string extensionsFilePath = Path.Combine(rootDirectory, "Data", "extensions.json");
        string categoriesFilePath = Path.Combine(rootDirectory, "Data", "categories.json");

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
}
