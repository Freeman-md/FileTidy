using FileTidy.Core.Models;

namespace FileTidy.Core.Interfaces;

public interface IFileManager
{
    Task<FileMoveResult> MoveFileAsync(string file, string category, string directoryPath,
        CancellationToken cancellationToken = default);

    Task RevertFileAsync(string fromPath, string toPath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);

}