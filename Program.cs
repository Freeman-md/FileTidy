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

    foreach (var directoryToSort in directoriesToSort)
    {
        SortDirectory(directoryToSort);
    }
}

static string GetFullPath(string path)
{
    if (path.Equals("downloads", StringComparison.OrdinalIgnoreCase))
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }
    else if (path.Equals("testing", StringComparison.OrdinalIgnoreCase))
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", path);
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

static void SortDirectory(string directoryToSort)
{

    IEnumerable<string> allFiles = Directory.EnumerateFiles(directoryToSort, "*", SearchOption.AllDirectories);

    Dictionary<string, List<string>> fileCategories = new Dictionary<string, List<string>>
    {
        { "Images", new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" } },
        { "Videos", new List<string> { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv" } },
        { "Documents", new List<string> { ".pdf", ".doc", ".docx", ".txt", ".csv", ".xlsx", ".pptx" } },
        { "Archives", new List<string> { ".zip", ".rar", ".tar", ".7z", ".gz" } },
        { "Code", new List<string> { ".cs", ".js", ".html", ".css", ".cpp", ".py", ".java", ".ts" } }
    };

    Dictionary<string, string> fileDestinations = new Dictionary<string, string>();

    foreach (var file in allFiles)
    {
        string extension = Path.GetExtension(file).ToLower();
        string category = "Others";

        foreach (var entry in fileCategories)
        {
            if (entry.Value.Contains(extension))
            {
                category = entry.Key;

                break;
            }
        }

        string destinationFolder = Path.Combine(directoryToSort, category);

        fileDestinations[file] = destinationFolder;
    }

    HashSet<string> createdDirectories = new HashSet<string>();

    Dictionary<string, int> sortedSummary = new Dictionary<string, int>();
    int totalFilesMoved = 0;
    int totalErrors = 0;


    foreach (var fileDestination in fileDestinations)
    {
        string sourceFilePath = fileDestination.Key;
        string destinationFolder = fileDestination.Value;
        string destinationFilePath = Path.Combine(destinationFolder, Path.GetFileName(sourceFilePath));

        if (!createdDirectories.Contains(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
            createdDirectories.Add(destinationFolder);
        }

        try
        {
            if (File.Exists(destinationFilePath))
            {
                string newFilePath = Path.Combine(destinationFolder, $"{Path.GetFileNameWithoutExtension(sourceFilePath)}_copy{Path.GetExtension(sourceFilePath)}");
                File.Move(sourceFilePath, newFilePath);
            }
            else
            {
                File.Move(sourceFilePath, destinationFilePath);
            }

            string category = new DirectoryInfo(destinationFolder).Name;

            if (sortedSummary.ContainsKey(category))
                sortedSummary[category]++;
            else
                sortedSummary[category] = 1;

            totalFilesMoved++;

        }
        catch (Exception ex)
        {
            totalErrors++;

            Console.WriteLine($"❌ Error moving file: {sourceFilePath} → {destinationFolder}");
            Console.WriteLine($"{ex.Message}");
        }
    }

    Console.WriteLine("\n📌 Sorting Summary:");
    Console.WriteLine($"🔹 Total Files Processed: {allFiles.Count()}");
    Console.WriteLine($"✅ Total Files Moved: {totalFilesMoved}");
    Console.WriteLine($"❌ Total Errors: {totalErrors}\n");

    foreach (var entry in sortedSummary)
    {
        Console.WriteLine($"📂 {entry.Key}: {entry.Value} files moved.");
    }
}