using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileTidy.Core.Services;

public class FileOperationService : IFileOperationService
{
    public Task<FileMoveResult> MoveFileAsync(string file, string category, string directoryPath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task RevertFileAsync(string fromPath, string toPath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task RemoveEmptyDirectoriesAsync(string directory)
        => throw new NotImplementedException();

    public Task RetryAsync(Func<Task> action, int maxRetries = 3, int delayMs = 200)
        => throw new NotImplementedException();
} 