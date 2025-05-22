using FileTidy.CLI.Reporting;
using FileTidy.CLI.Utils;
using FileTidy.Core.Services;
using FileTidy.Core.Utils;

List<string> directoriesToSort = new();

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

        if (DirectoryDiagnostics.CheckIfDirectoryExists(fullPath))
        {
            directoriesToSort.Add(fullPath);
        }
    }

    foreach (var directoryToSort in directoriesToSort)
    {
        var reporter = new ConsoleSortReporter();
        IEnumerable<string> allFiles = DirectoryHelper.GetAllFiles(directoryToSort);
        reporter.SetTotalFiles(allFiles.Count());

        string baseDirectory = AppContext.BaseDirectory;
        string projectRoot = Directory.GetParent(baseDirectory)!.Parent!.Parent!.FullName;
        string dataDirectory = Path.Combine(projectRoot, "FileTidy.CLI");

        var mapper = new FileCategoryMapper(dataDirectory);
        var sorter = new FileSorter(directoryToSort, mapper, reporter);
        sorter.Sort();
    }

    directoriesToSort.Clear();
}