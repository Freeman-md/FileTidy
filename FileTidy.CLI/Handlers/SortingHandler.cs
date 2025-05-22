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

            string baseDirectory = AppContext.BaseDirectory;
            string projectRoot = Directory.GetParent(baseDirectory)!.Parent!.Parent!.FullName;
            string dataDirectory = Path.Combine(projectRoot, "FileTidy.CLI");

            var mapper = new FileCategoryMapper(dataDirectory);
            FileSorter sorter = new FileSorter(directoryToSort, mapper, reporter);
            sorter.Sort();
        }
    }
}
