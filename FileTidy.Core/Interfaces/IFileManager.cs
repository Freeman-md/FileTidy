using FileTidy.Core.Models;

namespace FileTidy.Core.Interfaces;

public interface IFileManager
{
    FileMoveResult MoveFile(string file, string category, string directoryPath,
        CancellationToken cancellationToken = default);
}