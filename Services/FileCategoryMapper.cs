namespace FileTidy.Services;

public class FileCategoryMapper
{
    private readonly Dictionary<string, List<string>> _fileCategories = new Dictionary<string, List<string>>
    {
        { "Images", new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" } },
        { "Videos", new List<string> { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv" } },
        { "Documents", new List<string> { ".pdf", ".doc", ".docx", ".txt", ".csv", ".xlsx", ".pptx" } },
        { "Archives", new List<string> { ".zip", ".rar", ".tar", ".7z", ".gz" } },
        { "Code", new List<string> { ".cs", ".js", ".html", ".css", ".cpp", ".py", ".java", ".ts" } }
    };

    public string GetCategory(string extension)
    {
        extension = extension.ToLower();
        foreach (var entry in _fileCategories)
        {
            if (entry.Value.Contains(extension))
            {
                return entry.Key;
            }
        }
        return "Others";
    }
}
