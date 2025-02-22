using FileTidy.Services;

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
        string fullPath = DirectoryHelper.GetFullPath(path);

        if (DirectoryHelper.CheckIfDirectoryExists(fullPath))
        {
            directoriesToSort.Add(fullPath);
        }
    }

    foreach (var directoryToSort in directoriesToSort)
    {
        FileSorter sorter = new FileSorter(directoryToSort);
        sorter.Sort();
    }

    directoriesToSort.Clear();
}