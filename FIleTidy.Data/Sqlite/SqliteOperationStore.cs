using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;

namespace FIleTidy.Data.Sqlite;

public class SqliteOperationStore : IFileOperationStore
{
    public Task LogOperationAsync(FileOperation operation)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<FileOperation>> GetOperationsBySessionAsync(Guid sessionId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<FileOperation>> GetRecentOperationsAsync(int limit)
    {
        throw new NotImplementedException();
    }

    public Task UpdateOperationStatusAsync(Guid operationId, FileOperationStatus status)
    {
        throw new NotImplementedException();
    }
}