using System.Threading.Tasks;
using FileTidy.CLI.Reporting;
using FileTidy.Core.Services;
using FileTidy.Data.Sqlite;

namespace FileTidy.CLI.Handlers;

public static class SortingHandler
{
    public static async Task SortDirectories(List<string> directories)
    {
        foreach (var directoryToSort in directories)
        {
            var reporter = new ConsoleSortReporter();
            var store = new SqliteOperationStore();
            var service = new FileTidyingService(store, reporter);
            Guid sortSessionId = Guid.NewGuid();
            await service.SortDirectory(directoryToSort, sortSessionId);
        }
    }

}
