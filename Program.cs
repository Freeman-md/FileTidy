List<string> directoriesToSort = new List<string>();

while (true)
{
    Console.Write("Enter folder path(s) (comma-separated) or type 'exit' to quit: ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input)) continue;

    if (input.Trim().ToLower() == "exit") break;

    var paths = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .ToList();

    foreach (var path in paths)
    {
        string fullPath = GetFullPath(path);

        if (CheckIfDirectoryExists(fullPath))
        {
            directoriesToSort.Add(fullPath);
        }
    }
}

static string GetFullPath(string path)
{
    if (path.Equals("downloads", StringComparison.OrdinalIgnoreCase))
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }
    else if (path.Equals("documents", StringComparison.OrdinalIgnoreCase))
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
    }
    else if (path.Equals("desktop", StringComparison.OrdinalIgnoreCase))
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
    }

    return Path.GetFullPath(path);
}

static bool CheckIfDirectoryExists(string path)
{
    if (Directory.Exists(path))
    {
        Console.WriteLine($"\n✅ Directory found: {path}");
        IEnumerable<string> allFiles = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        Console.WriteLine($"📂 Total files in '{path}': {allFiles.Count()}");

        return true;
    }
    else
    {
        Console.WriteLine($"❌ Directory does not exist: {path}");
        return false;
    }
}