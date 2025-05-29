using FileTidy.Core.Models;

namespace FileTidy.Core.Interfaces;

public interface IFileTidyingService
{
    Task<TidyingResult> SortDirectory(string directoryPath, CancellationToken cancellationToken = default);
}