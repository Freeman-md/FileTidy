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

            IEnumerable<string> allFiles = Directory.EnumerateFiles(directoryToSort, "*", SearchOption.AllDirectories);
            reporter.SetTotalFiles(allFiles.Count());

            string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");

            var mapper = new FileCategoryMapper(dataDirectory);
            FileSorter sorter = new FileSorter(directoryToSort, mapper, reporter);
            sorter.Sort();
        }
    }
}
