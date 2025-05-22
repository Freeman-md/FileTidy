using System.Text.Json;
using FileTidy.Core.Models;

namespace FileTidy.Core.Services;

/// <summary>
/// Maps file extensions to user-defined categories loaded from JSON data files.
/// </summary>
public class FileCategoryMapper
{
    private readonly Dictionary<string, string> _fileCategories = new();
    private readonly string _dataDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileCategoryMapper"/> class.
    /// </summary>
    /// <param name="dataDirectory">The path to the directory containing the JSON data files.</param>
    public FileCategoryMapper(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
        LoadCategories();
    }

    /// <summary>
    /// Loads file extension-category mappings from extensions.json and categories.json.
    /// Throws if any file is missing or deserialization fails.
    /// </summary>
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

    /// <summary>
    /// Returns the category name for a given file extension.
    /// </summary>
    /// <param name="extension">The file extension (e.g., ".pdf").</param>
    /// <returns>The associated category, or "Others" if not found.</returns>
    public string GetCategory(string extension)
    {
        return _fileCategories.TryGetValue(extension.ToLower(), out string? category) ? category : "Others";
    }

    /// <summary>
    /// Gets all unique category names currently mapped.
    /// Useful for validating folder paths or skipping already sorted files.
    /// </summary>
    /// <returns>A collection of distinct category names.</returns>
    public IEnumerable<string> GetAllCategoryNames() => _fileCategories.Values.Distinct();
}
