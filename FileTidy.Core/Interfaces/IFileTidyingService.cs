using FileTidy.Core.Models;

namespace FileTidy.Core.Interfaces;

public interface IFileTidyingService
{
    Task<TidyingResult> SortDirectory(string directoryPath, CancellationToken cancellationToken = default);
    Task RevertFileAsync(string newPath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
}