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
        
        // Console.WriteLine($"DB Path: {new SqliteConnectionStringBuilder(_connectionString).DataSource}");
    }
    
    public SqliteOperationStore(string? customPath = null)
    {
        var dbPath = customPath ?? GetDefaultDbPath();

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
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
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library",
                "Application Support", "FileTidy");
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
                Status TEXT NOT NULL CHECK(Status IN ('Moved', 'Deleted', 'Reverted', 'Skipped', 'Failed')) DEFAULT 'Moved',
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                SortSessionId TEXT NOT NULL
            )
        ";

            command.ExecuteNonQuery();
        }
    }


    public async Task LogOperationAsync(FileOperation operation)
    {
        if (operation is null)
            throw new ArgumentNullException(nameof(operation));
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
                INSERT INTO FileOperations (
                            Id, FileName, OriginalPath, NewPath, Status, Timestamp, SortSessionId
                )
                VALUES (
                        @Id, @FileName, @OriginalPath, @NewPath, @Status, @Timestamp, @SortSessionId
                )
            ";

        command.Parameters.AddWithValue("@Id", operation.Id.ToString());
        command.Parameters.AddWithValue("@FileName", operation.FileName);
        command.Parameters.AddWithValue("@OriginalPath", operation.OriginalPath);
        command.Parameters.AddWithValue("@NewPath", operation.NewPath);
        command.Parameters.AddWithValue("@Status", operation.Status.ToString());
        command.Parameters.AddWithValue("@Timestamp", operation.Timestamp.ToUniversalTime());
        command.Parameters.AddWithValue("@SortSessionId", operation.SortSessionId.ToString());

        await command.ExecuteNonQueryAsync();
    }

    public async Task<FileOperation?> GetOperationByIdAsync(Guid operationId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"SELECT * FROM FileOperations WHERE Id = @Id";
        
        command.Parameters.AddWithValue("@Id", operationId.ToString());
        
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            return new FileOperation()
            {
                Id = reader.GetGuid(0),
                FileName = reader.GetString(1),
                OriginalPath = reader.GetString(2),
                NewPath = reader.GetString(3),
                Status = Enum.Parse<FileOperationStatus>(reader.GetString(4)),
                Timestamp = reader.GetDateTime(5),
                SortSessionId = reader.GetGuid(6),
            };
        }
        
        return null;
    }

    public async Task<FileOperation?> GetLatestNonRevertedOperationByNewPathAsync(string newPath, FileOperationStatus status)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT * FROM FileOperations 
        WHERE NewPath = @NewPath AND Status = @Status 
        ORDER BY Timestamp DESC 
        LIMIT 1";
        
        command.Parameters.AddWithValue("@NewPath", newPath);
        command.Parameters.AddWithValue("@Status", status.ToString());
        
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            return new FileOperation()
            {
                Id = reader.GetGuid(0),
                FileName = reader.GetString(1),
                OriginalPath = reader.GetString(2),
                NewPath = reader.GetString(3),
                Status = Enum.Parse<FileOperationStatus>(reader.GetString(4)),
                Timestamp = reader.GetDateTime(5),
                SortSessionId = reader.GetGuid(6),
            };
        }

        return null;
    }

    public async Task<IEnumerable<FileOperation>> GetOperationsBySessionAsync(Guid sessionId)
    {
        var operations = new List<FileOperation>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, FileName, OriginalPath, NewPath, Status, Timestamp, SortSessionId
FROM FileOperations
WHERE SortSessionId = @SortSessionId
ORDER BY Timestamp ASC";

        command.Parameters.AddWithValue("@SortSessionId", sessionId.ToString());

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            operations.Add(new FileOperation
            {
                Id = Guid.Parse(reader.GetString(0)),
                FileName = reader.GetString(1),
                OriginalPath = reader.GetString(2),
                NewPath = reader.GetString(3),
                Status = Enum.Parse<FileOperationStatus>(reader.GetString(4)),
                Timestamp = reader.GetDateTime(5),
                SortSessionId = Guid.Parse(reader.GetString(6))
            });
        }

        return operations;
    }

    public async Task<IEnumerable<FileOperation>> GetRecentOperationsAsync(int limit)
    {
        var operations = new List<FileOperation>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT Id, FileName, OriginalPath, NewPath, Status, Timestamp, SortSessionId
        FROM FileOperations
        ORDER BY Timestamp DESC
        LIMIT @Limit
    ";

        command.Parameters.AddWithValue("@Limit", limit);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            operations.Add(new FileOperation
            {
                Id = Guid.Parse(reader.GetString(0)),
                FileName = reader.GetString(1),
                OriginalPath = reader.GetString(2),
                NewPath = reader.GetString(3),
                Status = Enum.Parse<FileOperationStatus>(reader.GetString(4)),
                Timestamp = reader.GetDateTime(5),
                SortSessionId = Guid.Parse(reader.GetString(6))
            });
        }

        return operations;
    }


    public async Task UpdateOperationStatusAsync(Guid operationId, FileOperationStatus status)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE FileOperations
SET Status = @Status
WHERE Id = @Id";
        
        command.Parameters.AddWithValue("@Id", operationId.ToString());
        command.Parameters.AddWithValue("@Status", status.ToString());
        
        await command.ExecuteNonQueryAsync();
    }
}