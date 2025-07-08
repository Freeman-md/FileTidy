using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileTidy.Core.Services;

public class FileCategoryService : IFileCategoryService
{
    private readonly string _dataDirectory;
    private readonly Dictionary<string, string>? _fileCategories;
    private readonly List<Category>? _allCategories;


    public FileCategoryService(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? Path.Combine(AppContext.BaseDirectory, "Data");
        
        _fileCategories = new Dictionary<string, string>();
        _allCategories = new List<Category>();
        
        LoadCategories();
    }

    private void LoadCategories()
    {
        string extensionsFilePath = Path.Combine(_dataDirectory, "extensions.json");
        string categoriesFilePath = Path.Combine(_dataDirectory, "categories.json");

        if (!File.Exists(extensionsFilePath))
            throw new FileNotFoundException($"extensions.json file not found at {extensionsFilePath}");

        if (!File.Exists(categoriesFilePath))
            throw new FileNotFoundException($"categories.json file not found at {categoriesFilePath}");

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
        
        _allCategories!.AddRange(categories);

        Dictionary<int, string> categoryLookUp = categories.ToDictionary(category => category.Id, category => category.Name);

        foreach (var extension in extensions)
        {
            if (categoryLookUp.TryGetValue(extension.CategoryId, out string? categoryName))
            {
                if (!string.IsNullOrEmpty(categoryName))
                {
                    if (_fileCategories != null) _fileCategories[extension.Name.ToLower()] = categoryName;
                }
            }
        }
    }

    public string GetCategory(string extension)
    {
        return _fileCategories!.GetValueOrDefault(extension.ToLower(), "Others");
    }

    public IEnumerable<string> GetAllCategoryNames()
    {
        return _allCategories!.Select(c => c.Name).Distinct();
    }

    public IEnumerable<string> GetMappedCategoryNames()
    {
        return _fileCategories!.Values.Distinct();
    }
} 