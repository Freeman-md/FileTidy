namespace FileTidy.Core.Interfaces;

using FileTidy.Core.Models;
using System.Threading;
using System.Threading.Tasks;
using System;

public interface IFileOperationService
{
    Task<FileMoveResult> MoveFileAsync(string file, string category, string directoryPath, CancellationToken cancellationToken = default);
    Task RevertFileAsync(string fromPath, string toPath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    Task RemoveEmptyDirectoriesAsync(string directory);
} 