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

            // Build known category folders
            string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
            var mapper = new FileCategoryMapper(dataDirectory);

            var allFiles = Directory.EnumerateFiles(directoryToSort, "*", SearchOption.AllDirectories).ToList();

            // Filter out files already inside a category folder
            var categoryFolders = mapper.GetAllCategoryNames()
                .Select(c => Path.Combine(directoryToSort, c))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var filesToProcess = allFiles
                .Where(file =>
                    !categoryFolders.Any(folder =>
                        file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            reporter.SetTotalFiles(filesToProcess.Count);

            var sorter = new FileSorter(directoryToSort, mapper, reporter);
            sorter.Sort();
        }
    }

}
