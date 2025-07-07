using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FileTidy.Core.Services;

public class FileOrganizerService : IFileOrganizerService
{
    public Task<TidyingResult> SortDirectory(string directoryPath, Guid sessionId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task RevertFileAsync(string newPath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task RevertSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task RevertFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
} 