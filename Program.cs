using FileTidy.CLI.Reporting;
using FileTidy.Core.Services;
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
        var reporter = new ConsoleSortReporter();

        IEnumerable<string> allFiles = Directory.EnumerateFiles(directoryToSort, "*", SearchOption.AllDirectories);
        reporter.SetTotalFiles(allFiles.Count());

        FileSorter sorter = new FileSorter(directoryToSort, reporter);
        sorter.Sort();
    }

    directoriesToSort.Clear();
}