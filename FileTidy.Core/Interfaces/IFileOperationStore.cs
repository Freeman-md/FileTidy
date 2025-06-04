using FileTidy.Core.Models;

namespace FileTidy.Core.Interfaces;

public interface IFileOperationStore
{
    Task LogOperationAsync(FileOperation operation);
    Task<FileOperation?> GetOperationByIdAsync(Guid operationId);
    Task<FileOperation?> GetLatestNonRevertedOperationByNewPathAsync(string newPath, FileOperationStatus status);
    Task<IEnumerable<FileOperation>> GetOperationsBySessionAsync(Guid sessionId);
    Task<IEnumerable<FileOperation>> GetRecentOperationsAsync(int limit);
    Task UpdateOperationStatusAsync(Guid operationId, FileOperationStatus status);
    Task<IEnumerable<FileOperation>> GetFileOperationsInDirectoryAsync(string folderPath);
}