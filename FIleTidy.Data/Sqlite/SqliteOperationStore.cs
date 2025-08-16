using System.Diagnostics;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using Microsoft.Data.Sqlite;

namespace FileTidy.Data.Sqlite;

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

        Console.WriteLine($"DB Path: {new SqliteConnectionStringBuilder(_connectionString).DataSource}");
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
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FileTidy");

        Directory.CreateDirectory(baseDir);
        return Path.Combine(baseDir, "filetidy.db");
    }


    private void CreateTablesIfNotExists()
    {
        using var connection = new SqliteConnection(_connectionString);

        connection.Open();

        var command = connection.CreateCommand();

        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS FileOperations (
                Id TEXT PRIMARY KEY NOT NULL,
                FileName TEXT NOT NULL,
                OriginalPath TEXT NOT NULL,
                NewPath TEXT NOT NULL,
                Status TEXT NOT NULL CHECK(Status IN ('Moved', 'Deleted', 'Reverted', 'Skipped', 'Failed', 'Unprocessed')) DEFAULT 'Unprocessed',
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                SortSessionId TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS AppConfig (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );

        ";

        command.ExecuteNonQuery();
    }


    public async Task LogOperationAsync(FileOperation operation)
    {
        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        await using var connection = new SqliteConnection(_connectionString);
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
        command.Parameters.AddWithValue("@OriginalPath", operation.OriginalPath.Replace('\\', '/'));
        command.Parameters.AddWithValue("@NewPath", operation.NewPath.Replace('\\', '/'));
        command.Parameters.AddWithValue("@Status", operation.Status.ToString());
        command.Parameters.AddWithValue("@Timestamp", operation.Timestamp.ToUniversalTime());
        command.Parameters.AddWithValue("@SortSessionId", operation.SortSessionId.ToString());

        await command.ExecuteNonQueryAsync();
    }

    public async Task<FileOperation?> GetOperationByIdAsync(Guid operationId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"SELECT * FROM FileOperations WHERE Id = @Id";

        command.Parameters.AddWithValue("@Id", operationId.ToString());

        await using var reader = await command.ExecuteReaderAsync();

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

    public async Task<FileOperation?> GetLatestNonRevertedOperationByNewPathAsync(string newPath,
        FileOperationStatus status)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT * FROM FileOperations 
        WHERE NewPath = @NewPath AND Status = @Status 
        ORDER BY Timestamp DESC 
        LIMIT 1";

        command.Parameters.AddWithValue("@NewPath", newPath);
        command.Parameters.AddWithValue("@Status", status.ToString());

        await using var reader = await command.ExecuteReaderAsync();

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

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, FileName, OriginalPath, NewPath, Status, Timestamp, SortSessionId
FROM FileOperations
WHERE SortSessionId = @SortSessionId
ORDER BY Timestamp ASC";

        command.Parameters.AddWithValue("@SortSessionId", sessionId.ToString());

        await using var reader = await command.ExecuteReaderAsync();

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

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT Id, FileName, OriginalPath, NewPath, Status, Timestamp, SortSessionId
        FROM FileOperations
        ORDER BY Timestamp DESC
        LIMIT @Limit
    ";

        command.Parameters.AddWithValue("@Limit", limit);

        await using var reader = await command.ExecuteReaderAsync();

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
        await using var connection = new SqliteConnection(_connectionString);
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

    public async Task<IEnumerable<FileOperation>> GetFileOperationsInDirectoryAsync(string folderPath)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText = @"
SELECT Id, FileName, OriginalPath, NewPath, Status, Timestamp, SortSessionId
FROM FileOperations
WHERE NewPath LIKE @PathPrefix";

        // Ensure consistent forward slashes
        var normalizedFolderPath = folderPath.Replace('\\', '/').TrimEnd('/');
        command.Parameters.AddWithValue("@PathPrefix", normalizedFolderPath + "/%");


        var results = new List<FileOperation>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var newPath = reader.GetString(reader.GetOrdinal("NewPath"));
            var directoryOfNewPath = Path.GetDirectoryName(newPath)?.Replace('\\', '/').TrimEnd('/');

            // Only include files directly in the folder, not nested
            if (string.Equals(directoryOfNewPath, normalizedFolderPath, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new FileOperation
                {
                    Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                    FileName = reader.GetString(reader.GetOrdinal("FileName")),
                    OriginalPath = reader.GetString(reader.GetOrdinal("OriginalPath")),
                    NewPath = newPath,
                    Status = Enum.Parse<FileOperationStatus>(reader.GetString(reader.GetOrdinal("Status"))),
                    Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp")),
                    SortSessionId = Guid.Parse(reader.GetString(reader.GetOrdinal("SortSessionId")))
                });
            }
        }

        return results;
    }

    public async Task SaveConfigValueAsync(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO AppConfig(Key, Value) VALUES (@Key, @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = @Value;
            ";

        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@Value", value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteConfigValueAsync(string key)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"DELETE FROM AppConfig WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);

        await command.ExecuteNonQueryAsync();
    }


    public async Task<string?> GetConfigValueAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT Value FROM AppConfig WHERE Key = @Key
                ";
        command.Parameters.AddWithValue("@Key", key);

        var result = await command.ExecuteScalarAsync();
        return result?.ToString();
    }
}