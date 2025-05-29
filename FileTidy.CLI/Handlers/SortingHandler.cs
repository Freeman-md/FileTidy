using System.Threading.Tasks;
using FileTidy.CLI.Reporting;
using FileTidy.Core.Services;

namespace FileTidy.CLI.Handlers;

public static class SortingHandler
{
    public static async Task SortDirectories(List<string> directories)
    {
        foreach (var directoryToSort in directories)
        {
            var reporter = new ConsoleSortReporter();
            var service = new FileTidyingService(reporter);
            await service.SortDirectory(directoryToSort);
        }
    }

}
