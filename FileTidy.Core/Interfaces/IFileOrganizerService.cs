namespace FileTidy.Core.Interfaces;

using FileTidy.Core.Models;
using System.Threading;
using System.Threading.Tasks;
using System;

public interface IFileOrganizerService
{
    Task<TidyingResult> SortDirectory(string directoryPath, Guid sessionId, CancellationToken cancellationToken = default);
    Task RevertFileAsync(string newPath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    Task RevertSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task RevertFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    Task DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
} 