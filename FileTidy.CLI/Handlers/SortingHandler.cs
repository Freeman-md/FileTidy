using FileTidy.CLI.Reporting;
using FileTidy.Core.Services;

namespace FileTidy.CLI.Handlers;

public static class SortingHandler
{
    public static void SortDirectories(List<string> directories)
    {
        foreach (var directoryToSort in directories)
        {
            var reporter = new ConsoleSortReporter();

            var allFiles = Directory.EnumerateFiles(directoryToSort, "*", SearchOption.AllDirectories).ToList();

            var mapper = new FileCategoryMapper(Path.Combine(AppContext.BaseDirectory, "Data"));
            var categoryFolders = mapper.GetAllCategoryNames()
                .Select(c => Path.Combine(directoryToSort, c) + Path.DirectorySeparatorChar)
                .ToList();

            var filesToSort = allFiles
                .Where(file => !categoryFolders.Any(folder =>
                    Path.GetFullPath(file).StartsWith(folder, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            reporter.SetTotalFiles(filesToSort.Count);

            var sorter = new FileSorter(directoryToSort, mapper, reporter);
            sorter.Sort();

        }
    }
}
