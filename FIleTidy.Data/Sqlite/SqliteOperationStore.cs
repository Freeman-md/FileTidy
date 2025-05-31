using System.Diagnostics;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using Microsoft.Data.Sqlite;

namespace FIleTidy.Data.Sqlite;

public class SqliteOperationStore : IFileOperationStore
{
    private readonly string _connectionString;

    public SqliteOperationStore()
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = GetDefaultDbPath(),
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        CreateTablesIfNotExists();
    }
    
    private static string GetDefaultDbPath()
    {
        string baseDir;
        if (OperatingSystem.IsWindows())
        {
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileTidy");
        }
        else if (OperatingSystem.IsMacOS())
        {
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "FileTidy");
        }
        else
        {
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".filetidy");
        }

        Directory.CreateDirectory(baseDir);
        return Path.Combine(baseDir, "filetidy.db");
    }


    private void CreateTablesIfNotExists()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS FileOperations (
                Id TEXT PRIMARY KEY NOT NULL,
                FileName TEXT NOT NULL,
                OriginalPath TEXT NOT NULL,
                NewPath TEXT NOT NULL,
                Status TEXT NOT NULL CHECK(Status IN ('Moved', 'Deleted', 'Reverted', 'Unchanged')) DEFAULT 'Moved',
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                SortSessionId TEXT NOT NULL
            )
        ";

            command.ExecuteNonQuery();
        }
    }

    
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