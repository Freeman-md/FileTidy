using FileTidy.Core.Models;

namespace FileTidy.Core.Interfaces;

public interface IFileTidyingService
{
    Task<TidyingResult> SortDirectory(string directoryPath, Guid sortSessionId, CancellationToken cancellationToken = default);
    Task RevertFileAsync(string newPath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    Task RevertSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task RevertFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    Task DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
}